using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Text.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Ereignisse;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// **Ein Lauf, eine Meldung.** Ein Import legt viele Karten an und meldet einen Vorgang — nicht
// eines je Karte: der Kanal je Abonnent hält 64 Plätze mit DropOldest, und ein Bubble-Schnitt mit
// 445 Karten verlöre stillschweigend Ereignisse.
// Kein Test wartet hier eine feste Pause ab, wie in EreignisEndpunkteTests.
public class ImportereignisTests
{
    private const string Ereignisroute = "/api/ereignisse";
    private static readonly TimeSpan Zeitschranke = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Stillefrist = TimeSpan.FromMilliseconds(300);

    [Test]
    public async Task Wenn_ein_Import_schreibt_dann_kommt_genau_ein_Ereignis_mit_Board_Urheber_Weg_Zeitpunkt_und_Kartenzahl()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await using var leser = await Meldungsleser.Oeffne(webApi);
        var vorDemLauf = DateTimeOffset.UtcNow;

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "false"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var meldung = await leser.LiesNaechste();
        Assert.That(meldung, Is.Not.Null, "Der Strom hat kein Ereignis geliefert.");
        var ereignis = meldung!.Als<Importereignis>();
        Assert.Multiple(() =>
        {
            Assert.That(meldung.Art, Is.EqualTo("importereignis"));
            Assert.That(ereignis.Board, Is.EqualTo(aufbau.Board.BoardId));
            Assert.That(ereignis.Urheber, Is.EqualTo(aufbau.Kontributor.KontributorId));
            Assert.That(ereignis.Kartenzahl, Is.EqualTo(2));
            Assert.That(ereignis.Weg, Is.EqualTo(Ereignisweg.Api));
            Assert.That(ereignis.Zeitpunkt, Is.InRange(vorDemLauf, DateTimeOffset.UtcNow));
        });
        Assert.That(await leser.BleibtStill(Stillefrist), Is.True, "Ein Lauf hat mehr als ein Ereignis erzeugt.");
    }

    // Auch ein Lauf mit einundvierzig Karten meldet **einen** Vorgang.
    [Test]
    public async Task Wenn_ein_Import_viele_Karten_anlegt_dann_bleibt_es_bei_einem_Ereignis()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await using var leser = await Meldungsleser.Oeffne(webApi);

        using var antwort = await webApi.Klient.PostAsync(
            Importroute(aufbau.Board.BoardId),
            WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "false", dateitext: VieleKnoten(41)));

        antwort.EnsureSuccessStatusCode();
        var meldung = await leser.LiesNaechste();
        Assert.That(meldung!.Als<Importereignis>().Kartenzahl, Is.EqualTo(41));
        Assert.That(await leser.BleibtStill(Stillefrist), Is.True, "Es kam mehr als ein Ereignis für einen Lauf.");
    }

    [Test]
    public async Task Wenn_ein_Import_trocken_laeuft_dann_meldet_er_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await using var leser = await Meldungsleser.Oeffne(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "true"));

        antwort.EnsureSuccessStatusCode();
        Assert.That(await leser.BleibtStill(Stillefrist), Is.True, "Ein trockener Lauf hat gemeldet.");
    }

    [Test]
    public async Task Wenn_ein_Import_zurueckgewiesen_wird_dann_meldet_er_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await using var leser = await Meldungsleser.Oeffne(webApi);

        using var antwort = await webApi.Klient.PostAsync(
            Importroute(aufbau.Board.BoardId),
            WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "false", dateitext: "# Kein Frontmatter", dateiname: "Protokoll.md"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await leser.BleibtStill(Stillefrist), Is.True, "Eine Zurückweisung hat gemeldet.");
    }

    // **Ein Lauf ohne Wirkung meldet nichts.** Jede offene Sicht lüde sonst umsonst neu, um
    // dasselbe Board noch einmal zu zeigen — und ein geplanter Import im Hintergrund setzte die
    // Ansicht bei jedem Durchgang zurück.
    [Test]
    public async Task Wenn_ein_Lauf_nichts_anlegt_und_nichts_aendert_dann_meldet_er_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        using var ersterLauf = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "false"));
        ersterLauf.EnsureSuccessStatusCode();
        await using var leser = await Meldungsleser.Oeffne(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "false"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(await leser.BleibtStill(Stillefrist), Is.True, "Ein wirkungsloser Lauf hat gemeldet.");
    }

    // **Kartenzahl ist angelegt plus geändert** — ein Lauf, der zwei Karten anlegt und eine
    // nachzieht, meldet drei.
    [Test]
    public async Task Wenn_ein_Lauf_anlegt_und_aendert_dann_meldet_er_die_Summe_beider_Zahlen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        using var ersterLauf = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "false"));
        ersterLauf.EnsureSuccessStatusCode();
        await using var leser = await Meldungsleser.Oeffne(webApi);

        using var antwort = await webApi.Klient.PostAsync(
            Importroute(aufbau.Board.BoardId),
            WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "false", dateitext: MitZweiNeuenUndEinemGeaendertenKnoten()));

        antwort.EnsureSuccessStatusCode();
        var meldung = await leser.LiesNaechste();
        Assert.That(meldung!.Als<Importereignis>().Kartenzahl, Is.EqualTo(3));
        Assert.That(await leser.BleibtStill(Stillefrist), Is.True, "Es kam mehr als ein Ereignis für einen Lauf.");
    }

    // Zwei Arten auf **einer** Leitung: das Kartenereignis behält Gestalt und Artnamen, das
    // Importereignis tritt daneben — ein Abonnent unterscheidet sie am Artnamen.
    [Test]
    public async Task Wenn_beide_Arten_laufen_dann_reisen_sie_ueber_denselben_Strom_mit_eigenem_Artnamen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        var karte = await LegeKarteAn(webApi, aufbau.Board);
        await using var leser = await Meldungsleser.Oeffne(webApi);

        await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "false"));
        await webApi.Klient.PutAsJsonAsync(
            $"/api/boards/{aufbau.Board.BoardId}/karten/{karte.KarteId}/lage",
            new Kartenlage(aufbau.Board.Spalten[1].SpalteId, 1, aufbau.Kontributor.KontributorId));

        var erste = await leser.LiesNaechste();
        var zweite = await leser.LiesNaechste();
        Assert.Multiple(() =>
        {
            Assert.That(erste!.Art, Is.EqualTo("importereignis"));
            Assert.That(erste.Als<Importereignis>().Kartenzahl, Is.EqualTo(2));
            Assert.That(zweite!.Art, Is.EqualTo("kartenereignis"));
            Assert.That(zweite.Als<Kartenereignis>().Karte, Is.EqualTo(karte.KarteId));
            Assert.That(zweite.Als<Kartenereignis>().Urheber, Is.EqualTo(aufbau.Kontributor.KontributorId));
        });
    }

    private static string Importroute(long boardId)
    {
        return $"/api/boards/{boardId}/wbs-import";
    }

    private static string MitZweiNeuenUndEinemGeaendertenKnoten()
    {
        var geaendert = WbsImportEndpunkteTests.Probedatei()
            .Replace(
                "| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |",
                "| I0002 | Interaction | D0001 | Boards auflisten und filtern | rot | Die Liste zeigt alle Boards | | | | | R00002 | |",
                StringComparison.Ordinal);
        return string.Join(
            '\n',
            geaendert,
            "| I0003 | Interaction | D0001 | Boards archivieren | rot | | | | | | | |",
            "| I0004 | Interaction | D0001 | Boards benennen | rot | | | | | | | |");
    }

    private static string VieleKnoten(int interactions)
    {
        var zeilen = new List<string>
        {
            "---",
            "application: Probe",
            "---",
            "| ID | Ebene | Eltern | Name | Status | Fertig-Kriterium | Eingabe → Ausgabe | Aufwand | Ausbaustufe | Braucht | Requirement | Notiz |",
            "|---|---|---|---|---|---|---|---|---|---|---|---|",
            "| A0001 | Application | — | Probe | gelb | | | | | | | |",
            "| D0001 | Dialog | A0001 | Bahn | gelb | | | | | | | |",
        };
        for (var nummer = 1; nummer <= interactions; nummer++)
        {
            zeilen.Add($"| I{nummer:D4} | Interaction | D0001 | Knoten {nummer} | rot | | | | | | | |");
        }

        return string.Join('\n', zeilen);
    }

    private static async Task<Karte> LegeKarteAn(TestWebApi webApi, Board board)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(
            $"/api/boards/{board.BoardId}/spalten/{board.Spalten[0].SpalteId}/karten",
            new KarteAnlegenAnfrage("Migration schreiben"));
        antwort.EnsureSuccessStatusCode();
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        return karte!;
    }

    // Wie der Ereignisleser der Nachbarsuite, nur mit dem Artnamen daneben: dieser Slice bringt die
    // zweite Art, und ohne den Namen wäre sie vom Kartenereignis nicht zu unterscheiden.
    private sealed record Gelesenemeldung(string? Art, string Rumpf)
    {
        private static readonly JsonSerializerOptions Jsonform = new(JsonSerializerDefaults.Web);

        public T Als<T>()
        {
            var ereignis = JsonSerializer.Deserialize<T>(Rumpf, Jsonform);
            Assert.That(ereignis, Is.Not.Null, $"Der Rumpf war kein {typeof(T).Name}.");
            return ereignis!;
        }
    }

    private sealed class Meldungsleser : IAsyncDisposable
    {
        private readonly HttpResponseMessage _antwort;
        private readonly Stream _strom;
        private readonly IAsyncEnumerator<SseItem<string>> _elemente;
        private readonly CancellationTokenSource _abbruch;

        private Meldungsleser(HttpResponseMessage antwort, Stream strom, IAsyncEnumerator<SseItem<string>> elemente, CancellationTokenSource abbruch)
        {
            _antwort = antwort;
            _strom = strom;
            _elemente = elemente;
            _abbruch = abbruch;
        }

        public static async Task<Meldungsleser> Oeffne(TestWebApi webApi)
        {
            var abbruch = new CancellationTokenSource(Zeitschranke);
            var antwort = await webApi.Klient.GetAsync(Ereignisroute, HttpCompletionOption.ResponseHeadersRead, abbruch.Token);
            var strom = await antwort.Content.ReadAsStreamAsync(abbruch.Token);
            var elemente = SseParser.Create(strom).EnumerateAsync(abbruch.Token).GetAsyncEnumerator(abbruch.Token);
            return new Meldungsleser(antwort, strom, elemente, abbruch);
        }

        public async Task<Gelesenemeldung?> LiesNaechste()
        {
            var esKamEtwas = await _elemente.MoveNextAsync();
            if (!esKamEtwas)
            {
                return null;
            }

            return new Gelesenemeldung(_elemente.Current.EventType, _elemente.Current.Data);
        }

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
