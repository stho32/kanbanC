using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Text.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Ereignisse;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Der Rückweg aus der Sicht eines Abonnenten — der Oberfläche wie jedes Agenten. Kein Test wartet
// hier eine feste Pause ab: was kommen soll, wird mit Zeitschranke gelesen; was ausbleiben soll,
// wird daran belegt, dass **das nächste** gelesene Ereignis ein anderes ist.
public class EreignisEndpunkteTests
{
    private const string BoardsRoute = "/api/boards";
    private const string Ereignisroute = "/api/ereignisse";
    private static readonly TimeSpan Zeitschranke = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Stillefrist = TimeSpan.FromMilliseconds(300);

    [Test]
    public async Task Wenn_der_Ereignisstrom_geoeffnet_wird_dann_antwortet_die_API_mit_200_und_text_event_stream_und_schliesst_nicht()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        await using var leser = await Ereignisleser.Oeffne(webApi);

        Assert.Multiple(() =>
        {
            Assert.That(leser.Status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(leser.Inhaltstyp, Is.EqualTo("text/event-stream"));
        });
        Assert.That(await leser.BleibtStill(Stillefrist), Is.True, "Der Strom hat gemeldet oder geschlossen, obwohl sich nichts bewegt hat.");
    }

    [Test]
    public async Task Wenn_eine_Karte_verschoben_wird_dann_traegt_das_Ereignis_Board_Karte_Zielspalte_Urheber_Weg_und_Zeitpunkt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var kontributor = await LegeKontributorAn(webApi, "Nina Barth");
        var zielspalteId = board.Spalten[1].SpalteId;
        await using var leser = await Ereignisleser.Oeffne(webApi);
        var vorDerBewegung = DateTimeOffset.UtcNow;

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(zielspalteId, 1, kontributor.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var ereignis = await leser.LiesNaechstes();
        Assert.That(ereignis, Is.Not.Null, "Der Strom hat kein Ereignis geliefert.");
        Assert.Multiple(() =>
        {
            Assert.That(ereignis.Board, Is.EqualTo(board.BoardId));
            Assert.That(ereignis.Karte, Is.EqualTo(karte.KarteId));
            Assert.That(ereignis.SpalteId, Is.EqualTo(zielspalteId));
            Assert.That(ereignis.Urheber, Is.EqualTo(kontributor.KontributorId));
            Assert.That(ereignis.Weg, Is.EqualTo(Ereignisweg.Api));
            Assert.That(ereignis.Zeitpunkt, Is.InRange(vorDerBewegung, DateTimeOffset.UtcNow));
        });
        Assert.That(await leser.BleibtStill(Stillefrist), Is.True, "Eine Bewegung hat mehr als ein Ereignis erzeugt.");
    }

    [Test]
    public async Task Wenn_zwei_Abonnenten_gleichzeitig_lauschen_dann_bekommt_jeder_dasselbe_Ereignis_genau_einmal()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var zielspalteId = board.Spalten[1].SpalteId;
        await using var erster = await Ereignisleser.Oeffne(webApi);
        await using var zweiter = await Ereignisleser.Oeffne(webApi);

        await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(zielspalteId, 1));

        var beimErsten = await erster.LiesNaechstes();
        var beimZweiten = await zweiter.LiesNaechstes();
        Assert.Multiple(() =>
        {
            Assert.That(beimErsten?.Karte, Is.EqualTo(karte.KarteId));
            Assert.That(beimZweiten?.Karte, Is.EqualTo(karte.KarteId));
        });
        var derErsteBliebStill = await erster.BleibtStill(Stillefrist);
        var derZweiteBliebStill = await zweiter.BleibtStill(Stillefrist);
        Assert.Multiple(() =>
        {
            Assert.That(derErsteBliebStill, Is.True, "Der erste Abonnent hat dasselbe Ereignis doppelt bekommen.");
            Assert.That(derZweiteBliebStill, Is.True, "Der zweite Abonnent hat dasselbe Ereignis doppelt bekommen.");
        });
    }

    // Gemessen statt gehofft: nach drei Zurueckweisungen folgt eine Bewegung, die gelingt. Kommt
    // als erstes Ereignis die gelungene Karte, hat keine der drei gemeldet.
    [Test]
    public async Task Wenn_eine_Bewegung_zurueckgewiesen_wird_dann_bleibt_der_Strom_still()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var abgewiesene = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var gelungene = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Endpunkt bauen");
        var fremdesBoard = await LegeBoardAn(webApi);
        await using var leser = await Ereignisleser.Oeffne(webApi);

        var fremdeZielspalte = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, abgewiesene.KarteId), new Kartenlage(fremdesBoard.Spalten[0].SpalteId, 1));
        var unbekannteKarte = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, 999), new Kartenlage(board.Spalten[1].SpalteId, 1));
        var ungueltigePosition = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, abgewiesene.KarteId), new Kartenlage(board.Spalten[1].SpalteId, 0));
        await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, gelungene.KarteId), new Kartenlage(board.Spalten[1].SpalteId, 1));

        Assert.Multiple(() =>
        {
            Assert.That(fremdeZielspalte.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(unbekannteKarte.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(ungueltigePosition.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        });
        var ereignis = await leser.LiesNaechstes();
        Assert.That(ereignis?.Karte, Is.EqualTo(gelungene.KarteId), "Eine zurueckgewiesene Bewegung hat gemeldet.");
    }

    [Test]
    public async Task Wenn_die_Anfrage_den_Wegkopf_der_Oberflaeche_traegt_dann_steht_im_Ereignis_der_Weg_Oberflaeche()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        await using var leser = await Ereignisleser.Oeffne(webApi);

        using var anfrage = new HttpRequestMessage(HttpMethod.Put, Lageroute(board.BoardId, karte.KarteId))
        {
            Content = JsonContent.Create(new Kartenlage(board.Spalten[1].SpalteId, 1)),
        };
        anfrage.Headers.Add(Wegkopf.Name, Wegkopf.Oberflaechenwert);
        using var antwort = await webApi.Klient.SendAsync(anfrage);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var ereignis = await leser.LiesNaechstes();
        Assert.That(ereignis?.Weg, Is.EqualTo(Ereignisweg.Oberflaeche));
    }

    // Ein Kopf mit fremdem Wert ist kein Sonderfall: alles, was nicht die Oberfläche nennt, ist die
    // API — ein Agent muss nichts setzen, um richtig genannt zu werden.
    [Test]
    public async Task Wenn_der_Wegkopf_einen_fremden_Wert_traegt_dann_steht_im_Ereignis_der_Weg_Api()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        await using var leser = await Ereignisleser.Oeffne(webApi);

        using var anfrage = new HttpRequestMessage(HttpMethod.Put, Lageroute(board.BoardId, karte.KarteId))
        {
            Content = JsonContent.Create(new Kartenlage(board.Spalten[1].SpalteId, 1)),
        };
        anfrage.Headers.Add(Wegkopf.Name, "raumschiff");
        using var antwort = await webApi.Klient.SendAsync(anfrage);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var ereignis = await leser.LiesNaechstes();
        Assert.That(ereignis?.Weg, Is.EqualTo(Ereignisweg.Api));
    }

    [Test]
    public async Task Wenn_die_Bewegung_keinen_Kontributor_nennt_dann_gelingt_sie_und_der_Urheber_bleibt_leer()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        await using var leser = await Ereignisleser.Oeffne(webApi);

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(board.Spalten[1].SpalteId, 1));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var ereignis = await leser.LiesNaechstes();
        Assert.That(ereignis?.Urheber, Is.Null);
    }

    // Ein JSON ohne das Feld Kontributor ist gültig — das ist die Verträglichkeitszusage des
    // Vorgabewerts, an der die grüne R00007-Suite hängt.
    [Test]
    public async Task Wenn_der_Rumpf_das_Feld_Kontributor_gar_nicht_traegt_dann_wird_die_Karte_trotzdem_verschoben()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var zielspalteId = board.Spalten[1].SpalteId;
        using var rumpfOhneKontributor = new StringContent($"{{\"spalteId\":{zielspalteId},\"position\":1}}", System.Text.Encoding.UTF8, "application/json");

        var antwort = await webApi.Klient.PutAsync(Lageroute(board.BoardId, karte.KarteId), rumpfOhneKontributor);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[1].Karten.Select(gezogene => gezogene.KarteId), Does.Contain(karte.KarteId));
    }

    // Ein Abonnent, der geht, hinterlässt keinen Kanal: sonst hielte die Drehscheibe für jeden
    // abgerissenen Leser Speicher fest, den niemand mehr liest.
    [Test]
    public async Task Wenn_ein_Abonnent_die_Verbindung_schliesst_dann_raeumt_die_Drehscheibe_ihn_ab()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var drehscheibe = (Ereignisdrehscheibe)webApi.Dienste.GetService(typeof(Ereignisdrehscheibe))!;
        int waehrendDerVerbindung;
        await using (var leser = await Ereignisleser.Oeffne(webApi))
        {
            await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(board.Spalten[1].SpalteId, 1));
            await leser.LiesNaechstes();
            waehrendDerVerbindung = drehscheibe.Abonnentenzahl;
        }

        var nachDemSchliessen = await WarteAufAbonnentenzahl(drehscheibe, 0);

        Assert.Multiple(() =>
        {
            Assert.That(waehrendDerVerbindung, Is.EqualTo(1));
            Assert.That(nachDemSchliessen, Is.EqualTo(0), "Der abgerissene Abonnent blieb in der Drehscheibe stehen.");
        });
    }

    // Kein Ereignisspeicher: was geschah, bevor jemand zuhörte, ist weg — das ist die benannte
    // Lücke, die I0029 schließt. Gemessen statt gehofft: nach dem Verbinden folgt eine zweite
    // Bewegung; kommt sie als **erstes** Ereignis, wurde die erste nicht nachgeholt.
    [Test]
    public async Task Wenn_eine_Bewegung_vor_dem_Verbinden_geschah_dann_wird_sie_nicht_nachgeholt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var verpasste = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var spaetere = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Endpunkt bauen");

        await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, verpasste.KarteId), new Kartenlage(board.Spalten[1].SpalteId, 1));
        await using var leser = await Ereignisleser.Oeffne(webApi);
        await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, spaetere.KarteId), new Kartenlage(board.Spalten[1].SpalteId, 1));

        var ereignis = await leser.LiesNaechstes();
        Assert.That(ereignis?.Karte, Is.EqualTo(spaetere.KarteId), "Der Strom hat eine Bewegung von vor dem Verbinden nachgeholt.");
    }

    [Test]
    public void Wenn_die_Routen_der_WebApi_gelesen_werden_dann_ist_der_Ereignisstrom_genau_einmal_registriert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var ereignisrouten = webApi.Routen.Where(route => route.Contains("ereignisse", StringComparison.Ordinal)).ToList();

        Assert.That(ereignisrouten, Is.EqualTo(new[] { $"GET {Ereignisroute}" }));
    }

    // Das Abräumen geschieht auf dem Faden des Endpunkts und nicht auf dem des Tests; gewartet wird
    // mit Zeitschranke statt mit fester Pause.
    private static async Task<int> WarteAufAbonnentenzahl(Ereignisdrehscheibe drehscheibe, int erwartet)
    {
        var frist = DateTimeOffset.UtcNow + Zeitschranke;
        while (drehscheibe.Abonnentenzahl != erwartet && DateTimeOffset.UtcNow < frist)
        {
            await Task.Delay(10);
        }

        return drehscheibe.Abonnentenzahl;
    }

    private static string Lageroute(long boardId, long karteId)
    {
        return $"{BoardsRoute}/{boardId}/karten/{karteId}/lage";
    }

    private static string KartenRoute(long boardId, long spalteId)
    {
        return $"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten";
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        if (board is null)
        {
            throw new InvalidOperationException("Die API hat kein Board zurückgegeben.");
        }

        return board;
    }

    private static async Task<Board> LadeBoard(TestWebApi webApi, long boardId)
    {
        var board = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{boardId}");
        if (board is null)
        {
            throw new InvalidOperationException("Die API hat kein Board zurückgegeben.");
        }

        return board;
    }

    private static async Task<Karte> LegeKarteAn(TestWebApi webApi, long boardId, long spalteId, string titel)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(KartenRoute(boardId, spalteId), new KarteAnlegenAnfrage(titel));
        antwort.EnsureSuccessStatusCode();
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        if (karte is null)
        {
            throw new InvalidOperationException("Die API hat keine Karte zurückgegeben.");
        }

        return karte;
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync("/api/kontributoren", new KontributorAnlegenAnfrage(name, Kontributorart.Mensch));
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        if (kontributor is null)
        {
            throw new InvalidOperationException("Die API hat keinen Kontributor zurückgegeben.");
        }

        return kontributor;
    }

    // Der Abonnent der Tests: hält den offenen Strom, liest das nächste Ereignis mit Zeitschranke
    // und beantwortet die Gegenfrage — bleibt der Strom eine gemessene Frist lang still?
    private sealed class Ereignisleser : IAsyncDisposable
    {
        private static readonly JsonSerializerOptions Jsonform = new(JsonSerializerDefaults.Web);
        private readonly HttpResponseMessage _antwort;
        private readonly Stream _strom;
        private readonly IAsyncEnumerator<SseItem<string>> _elemente;
        private readonly CancellationTokenSource _abbruch;

        private Ereignisleser(HttpResponseMessage antwort, Stream strom, IAsyncEnumerator<SseItem<string>> elemente, CancellationTokenSource abbruch)
        {
            _antwort = antwort;
            _strom = strom;
            _elemente = elemente;
            _abbruch = abbruch;
        }

        public static async Task<Ereignisleser> Oeffne(TestWebApi webApi)
        {
            var abbruch = new CancellationTokenSource(Zeitschranke);
            var antwort = await webApi.Klient.GetAsync(Ereignisroute, HttpCompletionOption.ResponseHeadersRead, abbruch.Token);
            var strom = await antwort.Content.ReadAsStreamAsync(abbruch.Token);
            var elemente = SseParser.Create(strom).EnumerateAsync(abbruch.Token).GetAsyncEnumerator(abbruch.Token);
            return new Ereignisleser(antwort, strom, elemente, abbruch);
        }

        public HttpStatusCode Status => _antwort.StatusCode;

        public string? Inhaltstyp => _antwort.Content.Headers.ContentType?.MediaType;

        public async Task<Kartenereignis?> LiesNaechstes()
        {
            var esKamEtwas = await _elemente.MoveNextAsync();
            if (!esKamEtwas)
            {
                return null;
            }

            return JsonSerializer.Deserialize<Kartenereignis>(_elemente.Current.Data, Jsonform);
        }

        // Gemessen und nicht gehofft: die Frist läuft wirklich ab, und der Abbruch beweist, dass in
        // dieser Zeit nichts kam. Danach ist der Leser verbraucht — jeder Test nutzt ihn zuletzt.
        public async Task<bool> BleibtStill(TimeSpan frist)
        {
            _abbruch.CancelAfter(frist);
            try
            {
                await _elemente.MoveNextAsync();
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _abbruch.CancelAsync();
            try
            {
                await _elemente.DisposeAsync();
            }
            catch (OperationCanceledException)
            {
                // Der Abbruch ist der gewollte Weg, einen offenen Strom zu verlassen.
            }

            await _strom.DisposeAsync();
            _antwort.Dispose();
            _abbruch.Dispose();
        }
    }
}
