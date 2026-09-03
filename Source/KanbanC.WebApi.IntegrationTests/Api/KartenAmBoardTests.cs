using System.Net;
using System.Net.Http.Json;
using Dapper;
using KanbanC.Contracts.Boards;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Der reine Lesepfad: das Arrange setzt die Karten per SQL, weil der Schreibweg an eigener
// Stelle geprüft wird (KartenEndpunkteTests).
public class KartenAmBoardTests
{
    private const string BoardsRoute = "/api/boards";

    [Test]
    public async Task Wenn_eine_Spalte_Karten_traegt_dann_liefert_GET_auf_das_Board_sie_mit_KarteId_und_Titel_in_Positionsfolge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var rueckstand = board.Spalten[0].SpalteId;
        FuegeKarteEin(datenbank, rueckstand, "Bahn fuellen", 3);
        FuegeKarteEin(datenbank, rueckstand, "Migration schreiben", 1);
        FuegeKarteEin(datenbank, rueckstand, "Endpunkt bauen", 2);

        var geladen = await LadeBoard(webApi, board.BoardId);

        var karten = geladen.Spalten[0].Karten;
        Assert.Multiple(() =>
        {
            Assert.That(karten.Select(karte => karte.Titel),
                Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen", "Bahn fuellen" }));
            Assert.That(karten.Select(karte => karte.KarteId), Is.EqualTo(new long[] { 2, 3, 1 }));
        });
    }

    [Test]
    public async Task Wenn_eine_Spalte_keine_Karte_traegt_dann_liefert_GET_eine_leere_Kartenliste_statt_null()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        FuegeKarteEin(datenbank, board.Spalten[0].SpalteId, "Migration schreiben", 1);

        var geladen = await LadeBoard(webApi, board.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[1].Karten, Is.Not.Null);
            Assert.That(geladen.Spalten[1].Karten, Is.Empty);
            Assert.That(geladen.Spalten[2].Karten, Is.Empty);
        });
    }

    [Test]
    public async Task Wenn_zwei_Spalten_Karten_tragen_dann_erscheint_jede_Karte_nur_bei_ihrer_eigenen_Spalte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        FuegeKarteEin(datenbank, board.Spalten[0].SpalteId, "Migration schreiben", 1);
        FuegeKarteEin(datenbank, board.Spalten[0].SpalteId, "Endpunkt bauen", 2);
        FuegeKarteEin(datenbank, board.Spalten[1].SpalteId, "Kartenform zeichnen", 1);

        var geladen = await LadeBoard(webApi, board.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Titel),
                Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen" }));
            Assert.That(geladen.Spalten[1].Karten.Select(karte => karte.Titel),
                Is.EqualTo(new[] { "Kartenform zeichnen" }));
        });
    }

    [Test]
    public async Task Wenn_ein_zweites_Board_daneben_Karten_traegt_dann_bleiben_sie_beim_GET_aussen_vor()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var erstes = await LegeBoardAn(webApi);
        var zweites = await LegeBoardAn(webApi);
        FuegeKarteEin(datenbank, erstes.Spalten[0].SpalteId, "Migration schreiben", 1);
        FuegeKarteEin(datenbank, zweites.Spalten[0].SpalteId, "Angebot schreiben", 1);

        var geladen = await LadeBoard(webApi, zweites.BoardId);

        Assert.That(geladen.Spalten.SelectMany(spalte => spalte.Karten).Select(karte => karte.Titel),
            Is.EqualTo(new[] { "Angebot schreiben" }));
    }

    [Test]
    public async Task Wenn_die_boardId_nicht_vergeben_ist_dann_antwortet_GET_auch_mit_Karten_im_Bestand_mit_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        FuegeKarteEin(datenbank, board.Spalten[0].SpalteId, "Migration schreiben", 1);

        var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/99");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "board-unbekannt");
    }

    [Test]
    public async Task Wenn_die_WebApi_auf_derselben_Datei_neu_startet_dann_stehen_die_Karten_unveraendert_in_ihren_Spalten()
    {
        using var datenbank = new TemporaereDatenbank();
        long boardId;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz);
            boardId = board.BoardId;
            FuegeKarteEin(datenbank, board.Spalten[0].SpalteId, "Migration schreiben", 1);
            FuegeKarteEin(datenbank, board.Spalten[0].SpalteId, "Endpunkt bauen", 2);
            FuegeKarteEin(datenbank, board.Spalten[1].SpalteId, "Kartenform zeichnen", 1);
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var geladen = await LadeBoard(zweiteInstanz, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Titel),
                Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen" }));
            Assert.That(geladen.Spalten[1].Karten.Select(karte => karte.Titel),
                Is.EqualTo(new[] { "Kartenform zeichnen" }));
        });
    }

    private static void FuegeKarteEin(TemporaereDatenbank datenbank, long spalteId, string titel, int position)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position)",
            new { Spalte = spalteId, Titel = titel, Position = position });
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
