using System.Net;
using System.Net.Http.Json;
using Dapper;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Die Kürzung sitzt am Ausgang der Dienste. Das Arrange setzt Karten und Erledigungsdaten per SQL:
// über die Uhr des Testlaufs ließen sich zwei verschiedene Tage nicht herstellen.
public class GekuerzteAbschlussspalteTests
{
    private const string BoardsRoute = "/api/boards";
    private const int Anzeigegrenze = 20;

    [Test]
    public async Task Wenn_die_Abschlussspalte_mehr_Karten_traegt_als_ihre_Grenze_dann_liefert_GET_hoechstens_N_und_die_wahre_Kartenzahl()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        FuelleAbschlussspalte(datenbank, board, 23);

        var geladen = await LadeBoard(webApi, board.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[2].Karten, Has.Count.EqualTo(20));
            Assert.That(geladen.Spalten[2].Kartenzahl, Is.EqualTo(23));
        });
    }

    [Test]
    public async Task Wenn_die_Abschlussspalte_gekuerzt_wird_dann_sind_es_die_neuesten_und_die_Karte_ohne_Datum_faellt_zuerst_heraus()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        await SetzeAnzeigegrenze(webApi, board, 3);
        FuegeKarteEin(datenbank, abschlussspalteId, "Vorgestern", 1, "2026-09-03");
        FuegeKarteEin(datenbank, abschlussspalteId, "Gestern", 2, "2026-09-04");
        FuegeKarteEin(datenbank, abschlussspalteId, "Heute", 3, "2026-09-05");
        FuegeKarteEin(datenbank, abschlussspalteId, "Bestandskarte", 4, null);

        var geladen = await LadeBoard(webApi, board.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[2].Karten.Select(karte => karte.Titel),
                Is.EqualTo(new[] { "Heute", "Gestern", "Vorgestern" }));
            Assert.That(geladen.Spalten[2].Kartenzahl, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task Wenn_die_Abschlussspalte_genau_ihre_Grenze_traegt_dann_wird_nicht_gekuerzt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        FuelleAbschlussspalte(datenbank, board, Anzeigegrenze);

        var geladen = await LadeBoard(webApi, board.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[2].Karten, Has.Count.EqualTo(Anzeigegrenze));
            Assert.That(geladen.Spalten[2].Kartenzahl, Is.EqualTo(Anzeigegrenze));
        });
    }

    [Test]
    public async Task Wenn_eine_Spalte_ohne_Abschlussmarkierung_viele_Karten_traegt_dann_wird_sie_nie_gekuerzt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var rueckstandId = board.Spalten[0].SpalteId;
        for (var nummer = 1; nummer <= 23; nummer++)
        {
            FuegeKarteEin(datenbank, rueckstandId, $"K{nummer}", nummer, null);
        }

        var geladen = await LadeBoard(webApi, board.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[0].Karten, Has.Count.EqualTo(23));
            Assert.That(geladen.Spalten[0].Kartenzahl, Is.EqualTo(23));
        });
    }

    [Test]
    public async Task Wenn_eine_Karte_verschoben_wird_dann_liefert_PUT_die_Spalten_in_derselben_gekuerzten_Gestalt_wie_GET()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        FuelleAbschlussspalte(datenbank, board, 23);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Jetzt fertig");

        var antwort = await webApi.Klient.PutAsJsonAsync(
            Lageroute(board.BoardId, karte.KarteId), new Kartenlage(board.Spalten[2].SpalteId, 1));

        antwort.EnsureSuccessStatusCode();
        var spalten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Spalte>>();
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(spalten![2].Karten, Has.Count.EqualTo(20));
            Assert.That(spalten[2].Kartenzahl, Is.EqualTo(24));
            Assert.That(spalten[2].Karten[0].Titel, Is.EqualTo("Jetzt fertig"));
            Assert.That(spalten[2].Karten.Select(k => k.KarteId), Is.EqualTo(geladen.Spalten[2].Karten.Select(k => k.KarteId)));
            Assert.That(spalten[2].Kartenzahl, Is.EqualTo(geladen.Spalten[2].Kartenzahl));
        });
    }

    // Der Kern der Entscheidung, die Kürzung an den Ausgang der Dienste zu legen: geprüft wird
    // weiterhin gegen den ganzen Bestand, nicht gegen die gekürzte Liste.
    [Test]
    public async Task Wenn_eine_Karte_auf_Position_22_von_23_gezogen_wird_dann_wird_der_Zug_nicht_zurueckgewiesen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        FuelleAbschlussspalte(datenbank, board, 22);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Ans Ende");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(abschlussspalteId, 22));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Kartenzahl(datenbank, abschlussspalteId), Is.EqualTo(23));
    }

    [Test]
    public async Task Wenn_eine_Karte_hinter_den_ganzen_Bestand_gezogen_wird_dann_wird_der_Zug_ebenfalls_angenommen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        FuelleAbschlussspalte(datenbank, board, 23);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Ganz hinten");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(abschlussspalteId, 24));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Kartenzahl(datenbank, abschlussspalteId), Is.EqualTo(24));
    }

    private static void FuelleAbschlussspalte(TemporaereDatenbank datenbank, Board board, int anzahl)
    {
        var abschlussspalteId = board.Spalten[2].SpalteId;
        for (var nummer = 1; nummer <= anzahl; nummer++)
        {
            FuegeKarteEin(datenbank, abschlussspalteId, $"Fertig {nummer}", nummer, "2026-09-04");
        }
    }

    private static void FuegeKarteEin(TemporaereDatenbank datenbank, long spalteId, string titel, int position, string? erledigtAm)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var karteId = verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position);
            SELECT last_insert_rowid();", new { Spalte = spalteId, Titel = titel, Position = position });
        if (erledigtAm is null)
        {
            return;
        }

        verbindung.Execute(@"
            INSERT INTO Karteerledigung (Karte, ErledigtAm)
            VALUES (@Karte, @ErledigtAm)", new { Karte = karteId, ErledigtAm = erledigtAm });
    }

    private static int Kartenzahl(TemporaereDatenbank datenbank, long spalteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<int>(@"
            SELECT COUNT(*)
              FROM Karte
             WHERE Spalte = @Spalte", new { Spalte = spalteId });
    }

    private static async Task SetzeAnzeigegrenze(TestWebApi webApi, Board board, int anzeigegrenze)
    {
        var spalte = board.Spalten[2];
        var antwort = await webApi.Klient.PutAsJsonAsync(
            $"{BoardsRoute}/{board.BoardId}/spalten/{spalte.SpalteId}",
            new SpalteAendernAnfrage(spalte.Bezeichnung, true, anzeigegrenze));
        antwort.EnsureSuccessStatusCode();
    }

    private static string Lageroute(long boardId, long karteId)
    {
        return $"{BoardsRoute}/{boardId}/karten/{karteId}/lage";
    }

    private static async Task<Karte> LegeKarteAn(TestWebApi webApi, long boardId, long spalteId, string titel)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        antwort.EnsureSuccessStatusCode();
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        if (karte is null)
        {
            throw new InvalidOperationException("Die API hat keine Karte zurückgegeben.");
        }

        return karte;
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
}
