using System.Text.Json;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Ereignisse;
using Microsoft.Extensions.Logging.Abstractions;

namespace KanbanC.Blazor.Tests.Services;

// Die Leitung zur WebApi, geprüft unterhalb der E2E-Ebene: dass sie nach einem Abriss von selbst
// wieder aufnimmt, dass ein Ausfall der WebApi sie nicht still beendet und dass ein stiller Strom
// nichts anhält, ist über den Browser nicht auslösbar.
// Jede Erwartung hängt an einer Zeitschranke, keine an einer festen Pause; die Pause zwischen zwei
// Versuchen wird für den Test auf null gesetzt — die Wiederaufnahme selbst ist der Prüfgegenstand,
// nicht ihre Wartezeit.
public class EreignisleitungTests
{
    private static readonly TimeSpan Zeitschranke = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions Jsonform = new(JsonSerializerDefaults.Web);

    [Test]
    public async Task Wenn_ein_Element_aus_dem_Strom_kommt_dann_erreicht_es_den_Verteiler()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        var angekommene = new Ereignisfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, NullLogger<Ereignisleitung>.Instance);
        await leitung.StartAsync(abbruch.Token);
        var verbindung = await strom.NaechsteVerbindung(abbruch.Token);

        await verbindung.Sende(AlsRumpf(new Kartenereignis(3, 14, 7, 2, Ereignisweg.Api, DateTimeOffset.UnixEpoch)));

        var ereignis = await angekommene.WarteAufNaechstes(abbruch.Token);
        Assert.Multiple(() =>
        {
            Assert.That(ereignis.Board, Is.EqualTo(3));
            Assert.That(ereignis.Karte, Is.EqualTo(14));
            Assert.That(ereignis.SpalteId, Is.EqualTo(7));
            Assert.That(ereignis.Urheber, Is.EqualTo(2));
            Assert.That(ereignis.Weg, Is.EqualTo(Ereignisweg.Api));
        });
        await leitung.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task Wenn_der_Strom_abreisst_dann_verbindet_sich_die_Leitung_neu_und_meldet_wieder()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        var angekommene = new Ereignisfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, NullLogger<Ereignisleitung>.Instance);
        await leitung.StartAsync(abbruch.Token);
        var erste = await strom.NaechsteVerbindung(abbruch.Token);

        await erste.Reisse();

        var zweite = await strom.NaechsteVerbindung(abbruch.Token);
        await zweite.Sende(AlsRumpf(new Kartenereignis(3, 21, 7, null, Ereignisweg.Oberflaeche, DateTimeOffset.UnixEpoch)));
        var ereignis = await angekommene.WarteAufNaechstes(abbruch.Token);
        Assert.Multiple(() =>
        {
            Assert.That(strom.Verbindungsversuche, Is.GreaterThanOrEqualTo(2), "Nach dem Abriss wurde keine neue Verbindung aufgebaut.");
            Assert.That(ereignis.Karte, Is.EqualTo(21), "Die neue Verbindung meldet nicht mehr an den Verteiler.");
        });
        await leitung.StopAsync(CancellationToken.None);
    }

    // Fault Injection: die WebApi ist aus. Ein Ausfall darf die Leitung nicht still beenden — sonst
    // wäre die Live-Aktualisierung nach dem ersten Ausfall dauerhaft weg.
    [Test]
    public async Task Wenn_die_WebApi_nicht_erreichbar_ist_dann_versucht_die_Leitung_es_erneut()
    {
        using var strom = TestEreignisstrom.DerNichtErreichbarIst();
        var verteiler = new Ereignisverteiler();
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, NullLogger<Ereignisleitung>.Instance);

        await leitung.StartAsync(abbruch.Token);

        var versuche = await WarteAufVersuche(strom, 3, abbruch.Token);
        Assert.That(versuche, Is.GreaterThanOrEqualTo(3), "Die Leitung hat nach dem ersten Ausfall aufgegeben.");
        await leitung.StopAsync(CancellationToken.None);
    }

    // Ein Strom, der nie etwas liefert, ist der Normalfall: solange sich keine Karte bewegt, steht
    // er still. Er darf weder den Start noch das Herunterfahren aufhalten.
    [Test]
    public async Task Wenn_der_Strom_nie_etwas_liefert_dann_haelt_er_weder_Start_noch_Ende_auf()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        var angekommene = new Ereignisfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, NullLogger<Ereignisleitung>.Instance);

        await leitung.StartAsync(abbruch.Token);
        await strom.NaechsteVerbindung(abbruch.Token);
        await leitung.StopAsync(abbruch.Token);

        Assert.Multiple(() =>
        {
            Assert.That(strom.Verbindungsversuche, Is.EqualTo(1));
            Assert.That(angekommene.Anzahl, Is.Zero, "Ein stiller Strom hat gemeldet.");
        });
    }

    // Genau eine Leitung, auch wenn mehrere Sichten zuhören: die Zahl der offenen Verbindungen
    // hängt am Prozess und nicht an der Zahl der Hörer.
    [Test]
    public async Task Wenn_mehrere_Sichten_zuhoeren_dann_haelt_die_Leitung_trotzdem_nur_eine_Verbindung()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        var ersteSicht = new Ereignisfang(verteiler);
        var zweiteSicht = new Ereignisfang(verteiler);
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, NullLogger<Ereignisleitung>.Instance);
        await leitung.StartAsync(abbruch.Token);
        var verbindung = await strom.NaechsteVerbindung(abbruch.Token);

        await verbindung.Sende(AlsRumpf(new Kartenereignis(3, 14, 7, null, Ereignisweg.Api, DateTimeOffset.UnixEpoch)));

        var beiDerErsten = await ersteSicht.WarteAufNaechstes(abbruch.Token);
        var beiDerZweiten = await zweiteSicht.WarteAufNaechstes(abbruch.Token);
        Assert.Multiple(() =>
        {
            Assert.That(strom.Verbindungsversuche, Is.EqualTo(1), "Je Hörer entstand eine eigene Leitung.");
            Assert.That(beiDerErsten.Karte, Is.EqualTo(14));
            Assert.That(beiDerZweiten.Karte, Is.EqualTo(14));
        });
        await leitung.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task Wenn_die_Leitung_laeuft_dann_ruft_sie_die_Ereignisroute_der_WebApi()
    {
        using var strom = TestEreignisstrom.DerAntwortet();
        var verteiler = new Ereignisverteiler();
        using var abbruch = new CancellationTokenSource(Zeitschranke);
        using var leitung = new Ereignisleitung(strom, verteiler, TimeSpan.Zero, NullLogger<Ereignisleitung>.Instance);

        await leitung.StartAsync(abbruch.Token);
        await strom.NaechsteVerbindung(abbruch.Token);

        Assert.That(strom.LetzteAdresse, Is.EqualTo("http://webapi.test/api/ereignisse"));
        await leitung.StopAsync(CancellationToken.None);
    }

    private static async Task<int> WarteAufVersuche(TestEreignisstrom strom, int erwartet, CancellationToken abbruch)
    {
        while (strom.Verbindungsversuche < erwartet && !abbruch.IsCancellationRequested)
        {
            await Task.Delay(5, CancellationToken.None);
        }

        return strom.Verbindungsversuche;
    }

    private static string AlsRumpf(Kartenereignis ereignis)
    {
        return JsonSerializer.Serialize(ereignis, Jsonform);
    }

    // Was beim Hörer ankommt, gesammelt statt gezählt: der Test wartet auf das nächste Ereignis
    // mit Zeitschranke, statt eine Pause zu raten.
    private sealed class Ereignisfang
    {
        private readonly System.Threading.Channels.Channel<Kartenereignis> _angekommene = System.Threading.Channels.Channel.CreateUnbounded<Kartenereignis>();

        public Ereignisfang(Ereignisverteiler verteiler)
        {
            verteiler.Gemeldet += Nimm;
        }

        public int Anzahl => _angekommene.Reader.Count;

        public async Task<Kartenereignis> WarteAufNaechstes(CancellationToken abbruch)
        {
            return await _angekommene.Reader.ReadAsync(abbruch);
        }

        private void Nimm(Kartenereignis ereignis)
        {
            _angekommene.Writer.TryWrite(ereignis);
        }
    }
}
