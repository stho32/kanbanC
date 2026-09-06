using System.Net;
using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Klassen;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

public class KartenklassenEndpunkteTests
{
    private const string BoardsRoute = "/api/boards";

    [Test]
    public async Task Wenn_eine_Kartenklasse_angelegt_wird_dann_antwortet_die_Route_mit_201_der_Kartenklasse_und_der_Location()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenklassenRoute(boardId), new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var kartenklasse = await antwort.Content.ReadFromJsonAsync<Kartenklasse>();
        Assert.That(kartenklasse, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(kartenklasse!.Name, Is.EqualTo("WBS"));
            Assert.That(kartenklasse.Praefix, Is.EqualTo("WBS-"));
            Assert.That(kartenklasse.Zaehlerstand, Is.EqualTo(0));
            Assert.That(antwort.Headers.Location!.ToString(), Is.EqualTo($"/api/boards/{boardId}/kartenklassen/{kartenklasse.KartenklasseId}"));
        });
    }

    [Test]
    public async Task Wenn_das_Board_noch_keine_Kartenklasse_hat_dann_antwortet_der_Abruf_mit_200_und_einer_leeren_Liste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");

        var antwort = await webApi.Klient.GetAsync(KartenklassenRoute(boardId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var kartenklassen = await antwort.Content.ReadFromJsonAsync<List<Kartenklasse>>();
        Assert.That(kartenklassen, Is.Empty);
    }

    [Test]
    public async Task Wenn_drei_Kartenklassen_angelegt_wurden_dann_liefert_der_Abruf_sie_in_Anlagereihenfolge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");
        await LegeKartenklasseAn(webApi, boardId, "WBS", "WBS-");
        await LegeKartenklasseAn(webApi, boardId, "Bugmeldungen", "BUG-");
        await LegeKartenklasseAn(webApi, boardId, "Beschaffung", "BES-");

        var kartenklassen = await LadeKartenklassen(webApi, boardId);

        Assert.That(kartenklassen.Select(kartenklasse => kartenklasse.Name), Is.EqualTo(new[] { "WBS", "Bugmeldungen", "Beschaffung" }));
    }

    [Test]
    public async Task Wenn_Name_und_Praefix_Raender_tragen_dann_kommen_sie_getrimmt_und_sonst_zeichengleich_zurueck()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");

        await LegeKartenklasseAn(webApi, boardId, "  Dokumentation  ", "  DOK-  ");

        var kartenklassen = await LadeKartenklassen(webApi, boardId);
        Assert.Multiple(() =>
        {
            Assert.That(kartenklassen[0].Name, Is.EqualTo("Dokumentation"));
            Assert.That(kartenklassen[0].Praefix, Is.EqualTo("DOK-"));
        });
    }

    [Test]
    public async Task Wenn_der_Name_leer_ist_dann_antwortet_die_Route_mit_400_und_Rumpf_und_es_entsteht_keine_Zeile()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenklassenRoute(boardId), new KartenklasseAnlegenAnfrage("  ", "WBS-"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Kartenklasse ohne Namen");
        Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kartenklasse-name-leer"));
        Assert.That(zurueckweisung.Befunde[0].Meldung, Is.EqualTo("Eine Klasse braucht einen Namen."));
        Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain($"POST /api/boards/{boardId}/kartenklassen"));
        Assert.That(await LadeKartenklassen(webApi, boardId), Is.Empty);
    }

    [Test]
    public async Task Wenn_das_Praefix_leer_ist_dann_antwortet_die_Route_mit_400_und_Rumpf()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenklassenRoute(boardId), new KartenklasseAnlegenAnfrage("WBS", "   "));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kartenklasse-praefix-leer");
        Assert.That(await LadeKartenklassen(webApi, boardId), Is.Empty);
    }

    [Test]
    [TestCase("WB S-")]
    [TestCase("WBS/")]
    [TestCase("WBS.")]
    public async Task Wenn_das_Praefix_ein_unerlaubtes_Zeichen_traegt_dann_antwortet_die_Route_mit_400_und_Rumpf(string praefix)
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenklassenRoute(boardId), new KartenklasseAnlegenAnfrage("WBS", praefix));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kartenklasse-praefix-ungueltig");
        Assert.That(await LadeKartenklassen(webApi, boardId), Is.Empty);
    }

    [Test]
    public async Task Wenn_das_Praefix_ueber_der_Hoechstlaenge_liegt_dann_nennt_der_Befund_die_Hoechstlaenge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenklassenRoute(boardId), new KartenklasseAnlegenAnfrage("Auslieferung", "ABCDEFGHI"));

        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Praefix zu lang");
        Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kartenklasse-praefix-ungueltig"));
        Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("8"));
    }

    [Test]
    public async Task Wenn_das_Praefix_genau_die_Hoechstlaenge_hat_dann_wird_es_angenommen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenklassenRoute(boardId), new KartenklasseAnlegenAnfrage("Auslieferung", "ABCDEFGH"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    [TestCase("wbs-")]
    [TestCase(" WBS- ")]
    [TestCase("WBS-")]
    public async Task Wenn_das_Praefix_auf_diesem_Board_schon_vergeben_ist_dann_nennt_der_Befund_die_haltende_Klasse(string zweitesPraefix)
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");
        await LegeKartenklasseAn(webApi, boardId, "WBS", "WBS-");

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenklassenRoute(boardId), new KartenklasseAnlegenAnfrage("Arbeitspakete", zweitesPraefix));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Praefix vergeben");
        Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kartenklasse-praefix-vergeben"));
        Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("„WBS“"));
        Assert.That(await LadeKartenklassen(webApi, boardId), Has.Count.EqualTo(1));
    }

    // Die Identitaet einer Kartenklasse ist ihr Praefix: zwei Klassen duerfen denselben Namen
    // tragen, solange die Praefixe verschieden sind.
    [Test]
    public async Task Wenn_der_Name_schon_vergeben_ist_das_Praefix_aber_frei_dann_wird_die_Klasse_angenommen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");
        await LegeKartenklasseAn(webApi, boardId, "WBS", "WBS-");

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenklassenRoute(boardId), new KartenklasseAnlegenAnfrage("WBS", "WB2-"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(await LadeKartenklassen(webApi, boardId), Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Wenn_dasselbe_Praefix_auf_einem_zweiten_Board_angelegt_wird_dann_fuehren_beide_Boards_es_nebeneinander()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var projektA = await LegeBoardAn(webApi, "Projekt A");
        var projektB = await LegeBoardAn(webApi, "Projekt B");
        await LegeKartenklasseAn(webApi, projektA, "WBS", "WBS-");

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenklassenRoute(projektB), new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.Multiple(async () =>
        {
            Assert.That((await LadeKartenklassen(webApi, projektA)).Select(kartenklasse => kartenklasse.Praefix), Is.EqualTo(new[] { "WBS-" }));
            Assert.That((await LadeKartenklassen(webApi, projektB)).Select(kartenklasse => kartenklasse.Praefix), Is.EqualTo(new[] { "WBS-" }));
        });
    }

    [Test]
    public async Task Wenn_das_Board_unbekannt_ist_dann_antwortet_das_Anlegen_mit_404_und_Rumpf()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenklassenRoute(99999), new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Anlegen an unbekanntem Board");
        Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
        Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("99999"));
    }

    [Test]
    public async Task Wenn_das_Board_unbekannt_ist_dann_antwortet_auch_der_Abruf_mit_404_und_Rumpf()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var antwort = await webApi.Klient.GetAsync(KartenklassenRoute(99999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Abruf an unbekanntem Board");
        Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
        Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("99999"));
    }

    // Die Kartenklasse reist nicht am Board-Vertrag mit: sie haengt an ihrer eigenen Route.
    [Test]
    public async Task Wenn_das_Board_gelesen_wird_dann_traegt_es_keine_Kartenklassenliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");
        await LegeKartenklasseAn(webApi, boardId, "WBS", "WBS-");

        var rumpf = await webApi.Klient.GetStringAsync($"{BoardsRoute}/{boardId}");

        Assert.That(rumpf, Does.Not.Contain("kartenklasse").IgnoreCase);
    }

    // Die Route heisst im ganzen Stack kartenklassen; das Artboard zeichnet klassen, und die gibt
    // es nicht.
    [Test]
    public async Task Wenn_die_Route_klassen_gerufen_wird_dann_gibt_es_sie_nicht()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var boardId = await LegeBoardAn(webApi, "Entwicklung");

        var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{boardId}/klassen");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private static string KartenklassenRoute(long boardId)
    {
        return $"{BoardsRoute}/{boardId}/kartenklassen";
    }

    private static async Task<long> LegeBoardAn(TestWebApi webApi, string name)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage(name, BoardArt.Linie, null, null));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        Assert.That(board, Is.Not.Null);
        return board!.BoardId;
    }

    private static async Task LegeKartenklasseAn(TestWebApi webApi, long boardId, string name, string praefix)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(KartenklassenRoute(boardId), new KartenklasseAnlegenAnfrage(name, praefix));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task<IReadOnlyList<Kartenklasse>> LadeKartenklassen(TestWebApi webApi, long boardId)
    {
        var kartenklassen = await webApi.Klient.GetFromJsonAsync<List<Kartenklasse>>(KartenklassenRoute(boardId));
        Assert.That(kartenklassen, Is.Not.Null);
        return kartenklassen!;
    }
}
