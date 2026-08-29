using System.Net;
using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

public class SpaltenEndpunkteTests
{
    private const string BoardsRoute = "/api/boards";

    [Test]
    public async Task Wenn_eine_Spalte_per_POST_angelegt_wird_dann_antwortet_die_API_mit_201_und_Position_4()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten",
            new SpalteAnlegenAnfrage("Wartet auf Zulieferung", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var spalte = await antwort.Content.ReadFromJsonAsync<Spalte>();
        Assert.That(spalte, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(spalte.Position, Is.EqualTo(4));
            Assert.That(spalte.SpalteId, Is.GreaterThan(0));
            Assert.That(antwort.Headers.Location?.ToString(), Is.EqualTo($"{BoardsRoute}/{boardId}/spalten/{spalte.SpalteId}"));
        });
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten.Select(s => s.Bezeichnung),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt", "Wartet auf Zulieferung" }));
    }

    [Test]
    public async Task Wenn_zwei_Spalten_derselben_Bezeichnung_angelegt_werden_dann_tragen_sie_verschiedene_SpalteIds()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var erste = await LegeSpalteAn(webApi, boardId, new SpalteAnlegenAnfrage("Prüfung", false, null));
        var zweite = await LegeSpalteAn(webApi, boardId, new SpalteAnlegenAnfrage("Prüfung", false, null));

        Assert.That(erste.SpalteId, Is.Not.EqualTo(zweite.SpalteId));
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten, Has.Count.EqualTo(5));
    }

    [Test]
    public async Task Wenn_die_BoardId_nicht_vergeben_ist_dann_antwortet_POST_mit_404_und_es_entsteht_keine_Spalte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId + 1}/spalten",
            new SpalteAnlegenAnfrage("Eingang", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Wenn_eine_Spalte_per_PUT_umbenannt_wird_dann_liefert_der_Boardabruf_die_neue_Bezeichnung_an_gleicher_Position()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);
        var inArbeit = board.Spalten[1];

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{inArbeit.SpalteId}",
            new SpalteAendernAnfrage("In Umsetzung", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geaendert = await LadeBoard(webApi, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(geaendert.Spalten[1].Bezeichnung, Is.EqualTo("In Umsetzung"));
            Assert.That(geaendert.Spalten[1].Position, Is.EqualTo(2));
            Assert.That(geaendert.Spalten.Select(s => s.Bezeichnung),
                Is.EqualTo(new[] { "Zu erledigen", "In Umsetzung", "Erledigt" }));
        });
    }

    [Test]
    public async Task Wenn_zwei_Spalten_als_Abschlussspalte_markiert_werden_dann_behaelt_jede_ihre_eigene_Anzeigegrenze()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);

        await AendereSpalte(webApi, boardId, board.Spalten[0].SpalteId, new SpalteAendernAnfrage("Abgenommen", true, 10));

        var geaendert = await LadeBoard(webApi, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(geaendert.Spalten[0].IstAbschlussspalte, Is.True);
            Assert.That(geaendert.Spalten[0].Anzeigegrenze, Is.EqualTo(10));
            Assert.That(geaendert.Spalten[2].IstAbschlussspalte, Is.True);
            Assert.That(geaendert.Spalten[2].Anzeigegrenze, Is.EqualTo(20));
        });
    }

    [Test]
    public async Task Wenn_die_einzige_Abschlussspalte_entmarkiert_wird_dann_traegt_das_Board_danach_keine_Markierung_mehr()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{board.Spalten[2].SpalteId}",
            new SpalteAendernAnfrage("Erledigt", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geaendert = await LadeBoard(webApi, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(geaendert.Spalten.Any(s => s.IstAbschlussspalte), Is.False);
            Assert.That(geaendert.Spalten[2].Anzeigegrenze, Is.Null);
        });
    }

    [Test]
    public async Task Wenn_die_SpalteId_zu_einem_anderen_Board_gehoert_dann_antwortet_PUT_mit_404_und_aendert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var erstesBoard = await LegeBoardAn(webApi);
        var zweitesBoard = await LegeBoardAn(webApi);
        var fremdeSpalte = (await LadeBoard(webApi, erstesBoard)).Spalten[0];

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{zweitesBoard}/spalten/{fremdeSpalte.SpalteId}",
            new SpalteAendernAnfrage("Gekapert", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var unveraendert = await LadeBoard(webApi, erstesBoard);
        Assert.That(unveraendert.Spalten[0].Bezeichnung, Is.EqualTo("Zu erledigen"));
    }

    [Test]
    public async Task Wenn_die_SpalteId_nicht_vergeben_ist_dann_antwortet_PUT_mit_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/999",
            new SpalteAendernAnfrage("Erfunden", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Wenn_die_Bezeichnung_leer_ist_dann_antwortet_die_API_mit_400_und_lesbaren_Befunden()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten",
            new SpalteAnlegenAnfrage("   ", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung, Is.Not.Null);
        Assert.That(zurueckweisung.Befunde, Has.Count.GreaterThan(0));
        Assert.That(zurueckweisung.Befunde[0], Does.Contain("Bezeichnung"));
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Wenn_eine_Bezeichnung_beim_Aendern_leer_ist_dann_antwortet_die_API_mit_400_und_die_Spalte_bleibt_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{board.Spalten[0].SpalteId}",
            new SpalteAendernAnfrage("", false, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var unveraendert = await LadeBoard(webApi, boardId);
        Assert.That(unveraendert.Spalten[0].Bezeichnung, Is.EqualTo("Zu erledigen"));
    }

    [Test]
    public async Task Wenn_eine_Markierung_ohne_Anzeigegrenze_kommt_dann_antwortet_die_API_mit_400()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten",
            new SpalteAnlegenAnfrage("Abgenommen", true, null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung!.Befunde[0], Does.Contain("Anzeigegrenze"));
        var board = await LadeBoard(webApi, boardId);
        Assert.That(board.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Wenn_die_Anzeigegrenze_null_ist_dann_antwortet_die_API_mit_400()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten",
            new SpalteAnlegenAnfrage("Abgenommen", true, 0));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung!.Befunde[0], Does.Contain("größer"));
    }

    [Test]
    public async Task Wenn_eine_nicht_markierte_Spalte_eine_Anzeigegrenze_traegt_dann_antwortet_die_API_mit_400_und_die_Spalte_bleibt_ohne_Grenze()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi);
        var board = await LadeBoard(webApi, boardId);

        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{board.Spalten[0].SpalteId}",
            new SpalteAendernAnfrage("Zu erledigen", false, 5));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var unveraendert = await LadeBoard(webApi, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(unveraendert.Spalten[0].IstAbschlussspalte, Is.False);
            Assert.That(unveraendert.Spalten[0].Anzeigegrenze, Is.Null);
        });
    }

    private static async Task<long> LegeBoardAn(TestWebApi webApi)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute,
            new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        return board!.BoardId;
    }

    private static async Task<Board> LadeBoard(TestWebApi webApi, long boardId)
    {
        var board = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{boardId}");
        return board!;
    }

    private static async Task<Spalte> LegeSpalteAn(TestWebApi webApi, long boardId, SpalteAnlegenAnfrage anfrage)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten", anfrage);
        antwort.EnsureSuccessStatusCode();
        var spalte = await antwort.Content.ReadFromJsonAsync<Spalte>();
        return spalte!;
    }

    private static async Task AendereSpalte(TestWebApi webApi, long boardId, long spalteId, SpalteAendernAnfrage anfrage)
    {
        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}", anfrage);
        antwort.EnsureSuccessStatusCode();
    }
}
