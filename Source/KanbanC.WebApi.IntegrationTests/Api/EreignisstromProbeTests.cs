using System.Net;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Earned Trust fuer die Technik, auf der der ganze Rueckweg ruht: Server-Sent Events sind im
// Framework enthalten, im Repository aber ohne Vorbild. Fuenf Annahmen standen zur Widerlegung,
// **eine ist gefallen**: der Endpunkt schickt den Kopf **nicht** von selbst, solange nichts
// gemeldet wird. Ein Leser, der die Antwort abwartet, haengt dann an einem Strom, den es fuer ihn
// noch gar nicht gibt — und das Versprechen „offen und leer" waere gebrochen. Der Ausweg kostet
// eine Zeile: der Melder spuelt den Rumpf, bevor er wartet. Genau so baut es der Ereignisendpunkt.
// Die uebrigen vier Annahmen haben gehalten: jedes Element geht sofort hinaus, der Leser sieht es
// sofort, ResponseHeadersRead ist noetig, und ein endender Strom endet sichtbar.
// Jede Erwartung haengt an einer Zeitschranke, keine an einer festen Pause.
public class EreignisstromProbeTests
{
    private const string Route = "/probe/strom";
    private const string Ereignisart = "probeereignis";
    private static readonly TimeSpan Zeitschranke = TimeSpan.FromSeconds(5);

    // Nicht wie erwartet: **ohne** Spuelung bleibt der Kopf aus, solange niemand meldet. Der Test
    // haelt die widerlegte Annahme fest, damit ein spaeteres Framework-Update auffaellt.
    [Test]
    public void PROBE_Wenn_der_Melder_den_Rumpf_nicht_spuelt_dann_bekommt_der_Leser_keinen_Kopf_solange_nichts_gemeldet_wird()
    {
        var quelle = Channel.CreateUnbounded<string>();
        using var probe = new Stromprobe(quelle.Reader, spueltVorDemWarten: false);
        using var kurzeSchranke = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        Assert.ThrowsAsync<TaskCanceledException>(async () => await probe.OeffneStrom(kurzeSchranke.Token));

        quelle.Writer.Complete();
    }

    // Mit Spuelung ist der Strom das, was die Anforderung verlangt: 200, text/event-stream, offen
    // und leer.
    [Test]
    public async Task PROBE_Wenn_der_Melder_den_Rumpf_spuelt_dann_steht_der_Kopf_bevor_das_erste_Element_kommt()
    {
        var quelle = Channel.CreateUnbounded<string>();
        using var probe = new Stromprobe(quelle.Reader, spueltVorDemWarten: true);
        using var abbruch = new CancellationTokenSource(Zeitschranke);

        using var antwort = await probe.OeffneStrom(abbruch.Token);

        Assert.Multiple(() =>
        {
            Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(antwort.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/event-stream"));
        });
        quelle.Writer.Complete();
    }

    // Annahme 1 und 2: der Endpunkt schreibt jedes Element sofort und puffert nicht bis zum Ende,
    // und der Parser liefert es, sobald es kommt — beides gemessen an einem Strom, der zwischen
    // den Elementen offen bleibt.
    [Test]
    public async Task PROBE_Wenn_der_Strom_offen_bleibt_dann_erreicht_jedes_Element_den_Leser_einzeln()
    {
        var quelle = Channel.CreateUnbounded<string>();
        using var probe = new Stromprobe(quelle.Reader, spueltVorDemWarten: true);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var antwort = await probe.OeffneStrom(abbruch.Token);
        await using var strom = await antwort.Content.ReadAsStreamAsync(abbruch.Token);
        var gelesene = SseParser.Create(strom).EnumerateAsync(abbruch.Token).GetAsyncEnumerator(abbruch.Token);

        // Ein Strom aus Text reist unveraendert; erst die Ueberladung mit eigenem Typ schreibt
        // JSON — der Ereignisendpunkt nimmt deshalb die zweite.
        await quelle.Writer.WriteAsync("erstes", abbruch.Token);
        var dasErsteElementKam = await gelesene.MoveNextAsync();
        var erstes = gelesene.Current;
        await quelle.Writer.WriteAsync("zweites", abbruch.Token);
        var dasZweiteElementKam = await gelesene.MoveNextAsync();
        var zweites = gelesene.Current;

        Assert.Multiple(() =>
        {
            Assert.That(dasErsteElementKam, Is.True, "Der Strom hat das erste Element nicht geliefert, solange er offen war.");
            Assert.That(erstes.Data, Is.EqualTo("erstes"));
            Assert.That(erstes.EventType, Is.EqualTo(Ereignisart));
            Assert.That(dasZweiteElementKam, Is.True, "Das zweite Element kam erst nach dem Ende des Stroms.");
            Assert.That(zweites.Data, Is.EqualTo("zweites"));
        });

        quelle.Writer.Complete();
        await gelesene.DisposeAsync();
    }

    // Fault Injection auf der Klientenseite (Annahme 3): dieselbe Anfrage ohne
    // ResponseHeadersRead kehrt nicht zurueck, solange der Strom offen ist. Genau deshalb liest
    // die Ereignisleitung der Oberflaeche mit ResponseHeadersRead.
    [Test]
    public void PROBE_Wenn_ohne_ResponseHeadersRead_gelesen_wird_dann_kehrt_der_Aufruf_vor_dem_Strom_Ende_nicht_zurueck()
    {
        var quelle = Channel.CreateUnbounded<string>();
        using var probe = new Stromprobe(quelle.Reader, spueltVorDemWarten: true);
        using var kurzeSchranke = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            using var antwort = await probe.Klient.GetAsync(Route, HttpCompletionOption.ResponseContentRead, kurzeSchranke.Token);
        });

        quelle.Writer.Complete();
    }

    // Annahme 4: endet der Strom auf der Melderseite, sieht der Leser ein Ende und laeuft nicht
    // still weiter. Darauf ruht die Wiederaufnahme der Ereignisleitung.
    [Test]
    public async Task PROBE_Wenn_der_Melder_schliesst_dann_endet_der_Strom_beim_Leser()
    {
        var quelle = Channel.CreateUnbounded<string>();
        using var probe = new Stromprobe(quelle.Reader, spueltVorDemWarten: true);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var antwort = await probe.OeffneStrom(abbruch.Token);
        await using var strom = await antwort.Content.ReadAsStreamAsync(abbruch.Token);
        var gelesene = SseParser.Create(strom).EnumerateAsync(abbruch.Token).GetAsyncEnumerator(abbruch.Token);
        await quelle.Writer.WriteAsync("letztes", abbruch.Token);
        await gelesene.MoveNextAsync();

        quelle.Writer.Complete();

        var esKamNochEtwas = await gelesene.MoveNextAsync();
        Assert.That(esKamNochEtwas, Is.False, "Der Strom lief nach dem Schliessen des Melders still weiter.");
        await gelesene.DisposeAsync();
    }

    // Die Frage, die den Umfang bewegen konnte: bedient ein Testserver einen offenen Strom, ohne
    // den Lauf zu haengen? Ja — und er raeumt den Melder ab, sobald der Leser geht. Damit bleibt
    // die Pruefung des Endpunkts hier und wandert nicht in die Playwright-Strecke.
    [Test]
    public async Task PROBE_Wenn_der_Leser_abbricht_dann_endet_die_Aufzaehlung_des_Melders()
    {
        var quelle = Channel.CreateUnbounded<string>();
        using var probe = new Stromprobe(quelle.Reader, spueltVorDemWarten: true);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var antwort = await probe.OeffneStrom(abbruch.Token);
        await using (var strom = await antwort.Content.ReadAsStreamAsync(abbruch.Token))
        {
            var gelesene = SseParser.Create(strom).EnumerateAsync(abbruch.Token).GetAsyncEnumerator(abbruch.Token);
            await quelle.Writer.WriteAsync("erstes", abbruch.Token);
            await gelesene.MoveNextAsync();
            await gelesene.DisposeAsync();
        }

        var derMelderWurdeVerlassen = await probe.WarteAufEndeDerAufzaehlung(abbruch.Token);

        Assert.That(derMelderWurdeVerlassen, Is.True, "Der Melder lief weiter, obwohl der Leser die Verbindung abgebrochen hat.");
        quelle.Writer.Complete();
    }

    // Die Frage aus B0433, die den Umfang bewegen konnte: traegt TypedResults.ServerSentEvents
    // **zwei** Ereignisarten auf einer Leitung? Die Ueberladung mit einem festen Artnamen tut es
    // nicht — sie hat genau einen Typparameter und genau einen Namen. Die Ueberladung mit
    // SseItem<T> tut es: jedes Element traegt seinen eigenen EventType.
    // **Gemessen, nicht vermutet** — und mit ihr faellt die Antwort auf die zweite Frage: mit T =
    // object serialisiert System.Text.Json den **Laufzeittyp**, der Rumpf des Kartenereignisses
    // bleibt also unveraendert. Ein gemeinsamer Umschlag mit Artfeld waere nicht noetig, und er
    // haette die Gestalt des bestehenden Kartenereignisses auf der Leitung geaendert.
    [Test]
    public async Task PROBE_Wenn_der_Strom_SseItem_traegt_dann_reisen_zwei_Arten_mit_eigenem_Artnamen_und_vollem_Rumpf()
    {
        var quelle = Channel.CreateUnbounded<SseItem<object>>();
        using var probe = new Zweiartenprobe(quelle.Reader);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var antwort = await probe.OeffneStrom(abbruch.Token);
        await using var strom = await antwort.Content.ReadAsStreamAsync(abbruch.Token);
        var gelesene = SseParser.Create(strom).EnumerateAsync(abbruch.Token).GetAsyncEnumerator(abbruch.Token);

        await quelle.Writer.WriteAsync(new SseItem<object>(new Erstesprobeereignis(7, "sieben"), "ersteart"), abbruch.Token);
        await gelesene.MoveNextAsync();
        var erstes = gelesene.Current;
        await quelle.Writer.WriteAsync(new SseItem<object>(new Zweitesprobeereignis(41), "zweiteart"), abbruch.Token);
        await gelesene.MoveNextAsync();
        var zweites = gelesene.Current;

        Assert.Multiple(() =>
        {
            Assert.That(erstes.EventType, Is.EqualTo("ersteart"));
            Assert.That(erstes.Data, Does.Contain("\"sieben\""));
            Assert.That(erstes.Data, Does.Contain("7"));
            Assert.That(zweites.EventType, Is.EqualTo("zweiteart"));
            Assert.That(zweites.Data, Does.Contain("41"));
            Assert.That(zweites.Data, Does.Not.Contain("sieben"), "Der Rumpf der zweiten Art trug Felder der ersten.");
        });

        quelle.Writer.Complete();
        await gelesene.DisposeAsync();
    }

    private sealed record Erstesprobeereignis(long Nummer, string Name);

    private sealed record Zweitesprobeereignis(int Zahl);

    // Dieselbe Probeanwendung wie oben, nur mit der Ueberladung, die je Element einen Artnamen
    // traegt.
    private sealed class Zweiartenprobe : IDisposable
    {
        private readonly WebApplication _anwendung;

        public Zweiartenprobe(ChannelReader<SseItem<object>> quelle)
        {
            var erbauer = WebApplication.CreateBuilder();
            erbauer.WebHost.UseTestServer();
            _anwendung = erbauer.Build();
            _anwendung.MapGet(Route, (HttpContext kontext, CancellationToken abbruch) => TypedResults.ServerSentEvents(Melde(kontext, quelle, abbruch)));
            _anwendung.StartAsync().GetAwaiter().GetResult();
            Klient = _anwendung.GetTestClient();
        }

        public HttpClient Klient { get; }

        public async Task<HttpResponseMessage> OeffneStrom(CancellationToken abbruch)
        {
            return await Klient.GetAsync(Route, HttpCompletionOption.ResponseHeadersRead, abbruch);
        }

        private static async IAsyncEnumerable<SseItem<object>> Melde(HttpContext kontext, ChannelReader<SseItem<object>> quelle, [EnumeratorCancellation] CancellationToken abbruch)
        {
            await kontext.Response.Body.FlushAsync(abbruch);
            await foreach (var element in quelle.ReadAllAsync(abbruch))
            {
                yield return element;
            }
        }

        public void Dispose()
        {
            Klient.Dispose();
            _anwendung.StopAsync().GetAwaiter().GetResult();
            ((IDisposable)_anwendung).Dispose();
        }
    }

    // Die Probeanwendung mit genau einer Route: ein Strom, der offen bleibt, bis der Kanal
    // schliesst oder der Leser geht.
    private sealed class Stromprobe : IDisposable
    {
        private readonly WebApplication _anwendung;
        private readonly bool _spueltVorDemWarten;
        private readonly TaskCompletionSource _aufzaehlungEndete = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Stromprobe(ChannelReader<string> quelle, bool spueltVorDemWarten)
        {
            _spueltVorDemWarten = spueltVorDemWarten;
            var erbauer = WebApplication.CreateBuilder();
            erbauer.WebHost.UseTestServer();
            _anwendung = erbauer.Build();
            _anwendung.MapGet(Route, (HttpContext kontext, CancellationToken abbruch) => TypedResults.ServerSentEvents(Melde(kontext, quelle, abbruch), Ereignisart));
            _anwendung.StartAsync().GetAwaiter().GetResult();
            Klient = _anwendung.GetTestClient();
        }

        public HttpClient Klient { get; }

        public async Task<HttpResponseMessage> OeffneStrom(CancellationToken abbruch)
        {
            return await Klient.GetAsync(Route, HttpCompletionOption.ResponseHeadersRead, abbruch);
        }

        public async Task<bool> WarteAufEndeDerAufzaehlung(CancellationToken abbruch)
        {
            var beendet = await Task.WhenAny(_aufzaehlungEndete.Task, Task.Delay(Timeout.Infinite, abbruch));
            return beendet == _aufzaehlungEndete.Task;
        }

        private async IAsyncEnumerable<string> Melde(HttpContext kontext, ChannelReader<string> quelle, [EnumeratorCancellation] CancellationToken abbruch)
        {
            if (_spueltVorDemWarten)
            {
                await kontext.Response.Body.FlushAsync(abbruch);
            }

            try
            {
                await foreach (var element in quelle.ReadAllAsync(abbruch))
                {
                    yield return element;
                }
            }
            finally
            {
                _aufzaehlungEndete.TrySetResult();
            }
        }

        public void Dispose()
        {
            Klient.Dispose();
            _anwendung.StopAsync().GetAwaiter().GetResult();
            ((IDisposable)_anwendung).Dispose();
        }
    }
}
