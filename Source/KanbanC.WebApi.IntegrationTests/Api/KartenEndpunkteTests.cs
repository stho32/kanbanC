using System.Net;
using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

public class KartenEndpunkteTests
{
    private const string BoardsRoute = "/api/boards";
    private const int HoechsteTitellaenge = 1000;

    [Test]
    public async Task Wenn_eine_Karte_per_POST_angelegt_wird_dann_antwortet_die_API_mit_201_Location_und_vergebener_KarteId()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenRoute(board.BoardId, spalteId), new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        Assert.That(karte, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(karte.KarteId, Is.GreaterThan(0));
            Assert.That(karte.Titel, Is.EqualTo("Migration schreiben"));
            Assert.That(karte.Position, Is.EqualTo(1));
            Assert.That(antwort.Headers.Location?.ToString(),
                Is.EqualTo($"{KartenRoute(board.BoardId, spalteId)}/{karte.KarteId}"));
        });
    }

    [Test]
    public async Task Wenn_die_Spalte_drei_Karten_traegt_dann_erhaelt_die_vierte_Position_4_und_steht_im_Board_hinten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "Migration schreiben");
        await LegeKarteAn(webApi, board.BoardId, spalteId, "Endpunkt bauen");
        await LegeKarteAn(webApi, board.BoardId, spalteId, "Bahn fuellen");

        var vierte = await LegeKarteAn(webApi, board.BoardId, spalteId, "Kartenform zeichnen");

        Assert.That(vierte.Position, Is.EqualTo(4));
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Titel),
            Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen", "Bahn fuellen", "Kartenform zeichnen" }));
    }

    [Test]
    public async Task Wenn_die_Spalte_leer_ist_dann_erhaelt_die_neue_Karte_Position_1_und_erscheint_im_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[2].SpalteId;

        var karte = await LegeKarteAn(webApi, board.BoardId, spalteId, "Abnahme dokumentieren");

        Assert.That(karte.Position, Is.EqualTo(1));
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[2].Karten.Select(k => k.KarteId), Is.EqualTo(new[] { karte.KarteId }));
            Assert.That(geladen.Spalten[0].Karten, Is.Empty);
        });
    }

    [Test]
    public async Task Wenn_zwei_Karten_derselben_Spalte_denselben_Titel_tragen_dann_entstehen_beide_mit_verschiedenen_KarteIds()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;

        var erste = await LegeKarteAn(webApi, board.BoardId, spalteId, "Migration schreiben");
        var zweite = await LegeKarteAn(webApi, board.BoardId, spalteId, "Migration schreiben");

        Assert.That(zweite.KarteId, Is.Not.EqualTo(erste.KarteId));
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Titel),
            Is.EqualTo(new[] { "Migration schreiben", "Migration schreiben" }));
    }

    [Test]
    public async Task Wenn_der_Titel_umschliessende_Leerzeichen_traegt_dann_liefert_der_Boardabruf_ihn_getrimmt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "  Migration schreiben  ");

        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[0].Karten[0].Titel, Is.EqualTo("Migration schreiben"));
    }

    [Test]
    public async Task Wenn_die_spalteId_nicht_vergeben_ist_dann_antwortet_POST_mit_404_und_es_entsteht_keine_Karte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenRoute(board.BoardId, 999), new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "spalte-unbekannt");
        await ErwarteBoardOhneKarten(webApi, board.BoardId);
    }

    [Test]
    public async Task Wenn_die_Spalte_zu_einem_anderen_Board_gehoert_dann_antwortet_POST_mit_404_und_es_entsteht_keine_Karte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var erstes = await LegeBoardAn(webApi);
        var zweites = await LegeBoardAn(webApi);
        var fremdeSpalteId = erstes.Spalten[0].SpalteId;

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenRoute(zweites.BoardId, fremdeSpalteId), new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "spalte-unbekannt");
        await ErwarteBoardOhneKarten(webApi, erstes.BoardId);
        await ErwarteBoardOhneKarten(webApi, zweites.BoardId);
    }

    [Test]
    public async Task Wenn_die_boardId_nicht_vergeben_ist_dann_antwortet_POST_mit_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenRoute(99, board.Spalten[0].SpalteId), new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "spalte-unbekannt");
        await ErwarteBoardOhneKarten(webApi, board.BoardId);
    }

    [Test]
    public async Task Wenn_der_Titel_leer_ist_dann_antwortet_POST_mit_400_und_einer_lesbaren_Zurueckweisung()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "Migration schreiben");

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenRoute(board.BoardId, spalteId), new KarteAnlegenAnfrage(""));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung, Is.Not.Null);
        Assert.That(zurueckweisung.Befunde.Select(befund => befund.Meldung), Has.Some.Contains("Titel darf nicht leer sein"));
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Migration schreiben" }));
    }

    [Test]
    public async Task Wenn_der_Titel_nur_aus_Leerzeichen_besteht_dann_antwortet_POST_mit_400_und_es_entsteht_keine_Karte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenRoute(board.BoardId, board.Spalten[0].SpalteId), new KarteAnlegenAnfrage("   "));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await ErwarteBoardOhneKarten(webApi, board.BoardId);
    }

    [Test]
    public async Task Wenn_der_Titel_genau_1000_Zeichen_lang_ist_dann_antwortet_POST_mit_201()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var titel = new string('a', HoechsteTitellaenge);

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenRoute(board.BoardId, board.Spalten[0].SpalteId), new KarteAnlegenAnfrage(titel));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[0].Karten[0].Titel, Has.Length.EqualTo(HoechsteTitellaenge));
    }

    [Test]
    public async Task Wenn_der_Titel_1001_Zeichen_lang_ist_dann_antwortet_POST_mit_400_und_es_entsteht_keine_Karte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var titel = new string('a', HoechsteTitellaenge + 1);

        var antwort = await webApi.Klient.PostAsJsonAsync(KartenRoute(board.BoardId, board.Spalten[0].SpalteId), new KarteAnlegenAnfrage(titel));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung, Is.Not.Null);
        Assert.That(zurueckweisung.Befunde.Select(befund => befund.Meldung), Has.Some.Contains("1000"));
        await ErwarteBoardOhneKarten(webApi, board.BoardId);
    }

    [Test]
    public async Task Wenn_ein_Agent_eine_Karte_anlegt_dann_liefert_der_Boardabruf_sie_danach_an_derselben_Stelle()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var inArbeit = board.Spalten[1].SpalteId;

        var karte = await LegeKarteAn(webApi, board.BoardId, inArbeit, "Kartenform zeichnen");

        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[1].Karten, Is.EqualTo(new[] { karte }));
    }

    private static string KartenRoute(long boardId, long spalteId)
    {
        return $"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten";
    }


    [Test]
    public async Task Wenn_eine_Karte_per_PUT_in_eine_andere_Spalte_zieht_dann_antwortet_die_API_mit_200_und_den_neuen_Spalten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var quelle = board.Spalten[0].SpalteId;
        var ziel = board.Spalten[1].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, quelle, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, quelle, "B");
        await LegeKarteAn(webApi, board.BoardId, quelle, "C");
        await LegeKarteAn(webApi, board.BoardId, ziel, "X");
        await LegeKarteAn(webApi, board.BoardId, ziel, "Y");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, b.KarteId), new Kartenlage(ziel, 1));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var spalten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Spalte>>();
        Assert.That(spalten, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(spalten![0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "C" }));
            Assert.That(spalten[1].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "B", "X", "Y" }));
            Assert.That(spalten[0].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(spalten[1].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1, 2, 3 }));
        });

        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "C" }));
            Assert.That(geladen.Spalten[1].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "B", "X", "Y" }));
        });
    }

    [Test]
    public async Task Wenn_eine_Karte_innerhalb_ihrer_Spalte_umsortiert_wird_dann_liefert_GET_danach_dieselbe_Reihenfolge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        await LegeKarteAn(webApi, board.BoardId, spalteId, "C");
        var d = await LegeKarteAn(webApi, board.BoardId, spalteId, "D");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, d.KarteId), new Kartenlage(spalteId, 2));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "D", "B", "C" }));
    }

    [Test]
    public async Task Wenn_die_boardId_beim_Verschieben_unbekannt_ist_dann_antwortet_die_API_mit_404_und_nichts_bewegt_sich()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        var karte = await LegeKarteAn(webApi, board.BoardId, spalteId, "A");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(999, karte.KarteId), new Kartenlage(spalteId, 1));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "board-unbekannt");
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[0].Karten.Select(k => k.Titel), Is.EqualTo(new[] { "A" }));
    }

    [Test]
    public async Task Wenn_die_karteId_beim_Verschieben_unbekannt_ist_dann_antwortet_die_API_mit_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, 999), new Kartenlage(spalteId, 1));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "karte-unbekannt");
    }

    [Test]
    public async Task Wenn_die_Karte_zu_einem_anderen_Board_gehoert_dann_nennt_die_404_Antwort_dieses_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var erstes = await LegeBoardAn(webApi);
        var zweites = await LegeBoardAn(webApi);
        var fremde = await LegeKarteAn(webApi, erstes.BoardId, erstes.Spalten[0].SpalteId, "A");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(zweites.BoardId, fremde.KarteId),
            new Kartenlage(zweites.Spalten[0].SpalteId, 1));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "fremde Karte verschieben");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-fremd"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain($"/api/boards/{erstes.BoardId}"));
        });
        var geladen = await LadeBoard(webApi, erstes.BoardId);
        Assert.That(geladen.Spalten[0].Karten.Select(k => k.Titel), Is.EqualTo(new[] { "A" }));
    }

    [Test]
    public async Task Wenn_die_Zielspalte_zu_einem_anderen_Board_gehoert_dann_antwortet_die_API_mit_404_spalte_fremd()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var erstes = await LegeBoardAn(webApi);
        var zweites = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, erstes.BoardId, erstes.Spalten[0].SpalteId, "A");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(erstes.BoardId, karte.KarteId),
            new Kartenlage(zweites.Spalten[0].SpalteId, 1));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "spalte-fremd");
        var geladen = await LadeBoard(webApi, erstes.BoardId);
        Assert.That(geladen.Spalten[0].Karten.Select(k => k.Titel), Is.EqualTo(new[] { "A" }));
    }


    // Rechenbeispiel der Anforderung: Zielspalte mit 3 Karten, die Karte kommt aus einer anderen
    // Spalte — gueltig sind 1 bis 4; 0 und 5 werden zurueckgewiesen.
    [Test]
    public async Task Wenn_die_Position_ausserhalb_der_Zielspalte_liegt_dann_antwortet_die_API_mit_400_und_keine_Karte_bewegt_sich()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var quelle = board.Spalten[0].SpalteId;
        var ziel = board.Spalten[1].SpalteId;
        var karte = await LegeKarteAn(webApi, board.BoardId, quelle, "D");
        await LegeKarteAn(webApi, board.BoardId, ziel, "X");
        await LegeKarteAn(webApi, board.BoardId, ziel, "Y");
        await LegeKarteAn(webApi, board.BoardId, ziel, "Z");

        var zuKlein = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(ziel, 0));
        var zuGross = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(ziel, 5));

        Assert.Multiple(() =>
        {
            Assert.That(zuKlein.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(zuGross.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        });
        var zurueckweisung = await Fehlerrumpf.Lies(zuGross, "Position 5 in eine Zielspalte mit vier Karten nach dem Zug");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("position-ausserhalb"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("gültig sind 1 bis 4"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain($"GET /api/boards/{board.BoardId}"));
        });

        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[0].Karten.Select(k => k.Titel), Is.EqualTo(new[] { "D" }));
            Assert.That(geladen.Spalten[1].Karten.Select(k => k.Titel), Is.EqualTo(new[] { "X", "Y", "Z" }));
        });
    }

    [Test]
    public async Task Wenn_die_Position_die_hinterste_Stelle_der_Zielspalte_ist_dann_wird_der_Zug_ausgefuehrt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var quelle = board.Spalten[0].SpalteId;
        var ziel = board.Spalten[1].SpalteId;
        var karte = await LegeKarteAn(webApi, board.BoardId, quelle, "D");
        await LegeKarteAn(webApi, board.BoardId, ziel, "X");
        await LegeKarteAn(webApi, board.BoardId, ziel, "Y");
        await LegeKarteAn(webApi, board.BoardId, ziel, "Z");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(ziel, 4));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[0].Karten, Is.Empty);
            Assert.That(geladen.Spalten[1].Karten.Select(k => k.Titel), Is.EqualTo(new[] { "X", "Y", "Z", "D" }));
        });
    }

    // Liegt die Karte schon in der Zielspalte, traegt diese nach dem Zug unveraendert 3 Karten:
    // Position 4 ist dann keine gueltige Stelle mehr.
    [Test]
    public async Task Wenn_die_Karte_schon_in_der_Zielspalte_liegt_dann_endet_der_gueltige_Bereich_bei_ihrer_Kartenzahl()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        var c = await LegeKarteAn(webApi, board.BoardId, spalteId, "C");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, c.KarteId), new Kartenlage(spalteId, 4));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "position-ausserhalb");
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[0].Karten.Select(k => k.Titel), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    private static string Lageroute(long boardId, long karteId)
    {
        return $"{BoardsRoute}/{boardId}/karten/{karteId}/lage";
    }

    private static async Task ErwarteBoardOhneKarten(TestWebApi webApi, long boardId)
    {
        var geladen = await LadeBoard(webApi, boardId);
        Assert.That(geladen.Spalten.SelectMany(spalte => spalte.Karten), Is.Empty);
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
