using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

public class BoardEndpunkteTests
{
    private const string BoardsRoute = "/api/boards";

    [Test]
    public async Task Wenn_ein_Board_per_POST_angelegt_wird_dann_antwortet_die_API_mit_201_Location_und_BoardId_1()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);

        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, anfrage);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        Assert.That(board, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(antwort.Headers.Location?.ToString(), Is.EqualTo($"{BoardsRoute}/1"));
            Assert.That(board.BoardId, Is.EqualTo(1));
            Assert.That(board.Name, Is.EqualTo("Entwicklung"));
            Assert.That(board.Art, Is.EqualTo(BoardArt.Linie));
        });
    }

    [Test]
    public async Task Wenn_zwei_Boards_gleichen_Namens_angelegt_werden_dann_erhalten_sie_die_BoardIds_1_und_2_und_stehen_beide_in_der_Liste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);
        var listeVorher = await webApi.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);
        Assert.That(listeVorher, Is.Empty);

        var erstes = await LegeBoardAn(webApi, anfrage);
        var zweites = await LegeBoardAn(webApi, anfrage);

        var listeNachher = await webApi.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);
        Assert.That(listeNachher, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(erstes.BoardId, Is.EqualTo(1));
            Assert.That(zweites.BoardId, Is.EqualTo(2));
            Assert.That(listeNachher.Select(b => b.BoardId), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(listeNachher.Select(b => b.Name), Is.All.EqualTo("Entwicklung"));
        });
    }

    [Test]
    public async Task Wenn_die_Boardnamen_gemischt_gross_und_klein_geschrieben_sind_dann_liefert_GET_sie_alphabetisch()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Wartung", BoardArt.Linie, null, null));
        await LegeBoardAn(webApi, new BoardAnlegenAnfrage("beschaffung", BoardArt.Linie, null, null));
        await LegeBoardAn(webApi, new BoardAnlegenAnfrage("KanbanC", BoardArt.Projekt, null, null));

        var boards = await webApi.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);

        Assert.That(boards, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(boards.Select(b => b.Name), Is.EqualTo(new[] { "beschaffung", "KanbanC", "Wartung" }));
            Assert.That(boards.Select(b => b.BoardId), Is.EqualTo(new long[] { 2, 3, 1 }));
        });
    }

    [Test]
    public async Task Wenn_zwei_Boards_denselben_Namen_tragen_dann_liefert_GET_das_mit_der_kleineren_BoardId_zuerst()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Zwischenstand", BoardArt.Linie, null, null));
        await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Wartung", BoardArt.Linie, null, null));
        await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Wartung", BoardArt.Projekt, null, null));

        var boards = await webApi.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);

        Assert.That(boards, Is.Not.Null);
        Assert.That(boards.Select(b => b.BoardId), Is.EqualTo(new long[] { 2, 3, 1 }));
    }

    [Test]
    public async Task Wenn_ein_Projektboard_mit_Terminen_angelegt_wird_dann_liefert_GET_die_Termine_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anfrage = new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31));
        var angelegt = await LegeBoardAn(webApi, anfrage);

        var geladen = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{angelegt.BoardId}");

        Assert.That(geladen, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(geladen.Art, Is.EqualTo(BoardArt.Projekt));
            Assert.That(geladen.Starttermin, Is.EqualTo(new DateOnly(2026, 9, 1)));
            Assert.That(geladen.Zieltermin, Is.EqualTo(new DateOnly(2026, 12, 31)));
        });
    }

    [Test]
    public async Task Wenn_ein_Linienboard_mit_Terminen_angelegt_wird_dann_liefert_GET_die_Termine_ebenfalls()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anfrage = new BoardAnlegenAnfrage("Betrieb", BoardArt.Linie, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
        var angelegt = await LegeBoardAn(webApi, anfrage);

        var geladen = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{angelegt.BoardId}");

        Assert.That(geladen, Is.Not.Null);
        Assert.That(geladen.Starttermin, Is.EqualTo(new DateOnly(2026, 1, 1)));
        Assert.That(geladen.Zieltermin, Is.EqualTo(new DateOnly(2026, 6, 30)));
    }

    [Test]
    public async Task Wenn_keine_Termine_angegeben_sind_dann_bleiben_beide_Felder_in_der_Antwort_leer()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var angelegt = await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));

        var geladen = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{angelegt.BoardId}");

        Assert.That(geladen, Is.Not.Null);
        Assert.That(geladen.Starttermin, Is.Null);
        Assert.That(geladen.Zieltermin, Is.Null);
    }

    [Test]
    public async Task Wenn_ein_neues_Board_per_GET_gelesen_wird_dann_hat_es_die_drei_Standardspalten_mit_Erledigt_als_Abschlussspalte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var angelegt = await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));

        var geladen = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{angelegt.BoardId}");

        Assert.That(geladen, Is.Not.Null);
        var abschlussspalten = geladen.Spalten.Where(s => s.IstAbschlussspalte).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten.Select(s => s.Bezeichnung), Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt" }));
            Assert.That(abschlussspalten, Has.Count.EqualTo(1));
            Assert.That(abschlussspalten[0].Bezeichnung, Is.EqualTo("Erledigt"));
            Assert.That(abschlussspalten[0].Anzeigegrenze, Is.EqualTo(20));
        });
    }

    [Test]
    public async Task Wenn_die_BoardId_nicht_vergeben_ist_dann_antwortet_GET_mit_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));

        var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/99");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "board-unbekannt");
    }

    [Test]
    public async Task Wenn_der_Name_leer_ist_dann_antwortet_POST_mit_400_und_einem_lesbaren_Befund_und_es_entsteht_kein_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anfrage = new BoardAnlegenAnfrage("   ", BoardArt.Linie, null, null);

        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, anfrage);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung, Is.Not.Null);
        Assert.That(zurueckweisung.Befunde.Select(befund => befund.Meldung), Is.EqualTo(new[] { "Der Name darf nicht leer sein." }));
        var boards = await webApi.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);
        Assert.That(boards, Is.Empty);
    }

    [Test]
    public async Task Wenn_der_Zieltermin_vor_dem_Starttermin_liegt_dann_antwortet_POST_mit_400_und_es_entsteht_kein_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anfrage = new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 1));

        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, anfrage);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung, Is.Not.Null);
        Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("Zieltermin"));
        var boards = await webApi.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);
        Assert.That(boards, Is.Empty);
    }

    [Test]
    public async Task Wenn_die_Art_ein_unbekannter_Text_ist_dann_antwortet_POST_mit_400_und_es_entsteht_kein_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var rumpf = new { name = "Entwicklung", art = "Sprint" };

        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, rumpf);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var boards = await webApi.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);
        Assert.That(boards, Is.Empty);
    }

    [Test]
    public async Task Wenn_die_Art_eine_unbekannte_Zahl_ist_dann_antwortet_POST_mit_400_und_einem_Befund_zur_Art()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var rumpf = new { name = "Entwicklung", art = 7 };

        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, rumpf);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        Assert.That(zurueckweisung, Is.Not.Null);
        Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("Board-Art"));
        var boards = await webApi.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);
        Assert.That(boards, Is.Empty);
    }

    [Test]
    public async Task Wenn_ein_Board_neu_angelegt_ist_dann_liefert_GET_zeigtKartenzahl_falsch()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var angelegt = await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));

        var geladen = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{angelegt.BoardId}");

        Assert.That(geladen, Is.Not.Null);
        Assert.That(angelegt.ZeigtKartenzahl, Is.False);
        Assert.That(geladen.ZeigtKartenzahl, Is.False);
    }

    [Test]
    public async Task Wenn_die_Kartenzahl_per_PUT_eingeschaltet_wird_dann_antwortet_die_API_mit_200_und_GET_liefert_denselben_Wert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));

        var antwort = await webApi.Klient.PutAsJsonAsync(KartenzahlRoute(board.BoardId), new Kartenzahlanzeige(true));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geschaltet = await antwort.Content.ReadFromJsonAsync<Board>();
        var geladen = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{board.BoardId}");
        Assert.Multiple(() =>
        {
            Assert.That(geschaltet!.ZeigtKartenzahl, Is.True);
            Assert.That(geschaltet.Spalten.Select(spalte => spalte.SpalteId), Is.EqualTo(board.Spalten.Select(spalte => spalte.SpalteId)));
            Assert.That(geladen!.ZeigtKartenzahl, Is.True);
        });
    }

    [Test]
    public async Task Wenn_zweimal_eingeschaltet_und_dann_ausgeschaltet_wird_dann_folgt_die_API_dem_letzten_Aufruf()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
        var erstes = await SchalteKartenzahl(webApi, board.BoardId, true);
        var zweites = await SchalteKartenzahl(webApi, board.BoardId, true);

        var ausgeschaltet = await SchalteKartenzahl(webApi, board.BoardId, false);

        var geladen = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{board.BoardId}");
        Assert.Multiple(() =>
        {
            Assert.That(erstes.ZeigtKartenzahl, Is.True);
            // Nicht die Records vergleichen: nach dem Weg über JSON baut jede Antwort frische
            // Spaltenlisten, und der Record vergleicht diesen Member per Referenz (Spalte.cs).
            Assert.That(zweites.ZeigtKartenzahl, Is.True);
            Assert.That(zweites.Spalten.Select(spalte => spalte.SpalteId), Is.EqualTo(erstes.Spalten.Select(spalte => spalte.SpalteId)));
            Assert.That(ausgeschaltet.ZeigtKartenzahl, Is.False);
            Assert.That(geladen!.ZeigtKartenzahl, Is.False);
        });
    }

    [Test]
    public async Task Wenn_die_Kartenzahl_auf_eine_unbekannte_BoardId_geschaltet_wird_dann_antwortet_die_API_mit_404_und_einem_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));

        var antwort = await webApi.Klient.PutAsJsonAsync(KartenzahlRoute(999), new Kartenzahlanzeige(true));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Kartenzahl an unbekanntem Board");
        var geladen = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{board.BoardId}");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("/api/boards"));
            Assert.That(geladen!.ZeigtKartenzahl, Is.False);
        });
    }

    // Die angezeigte Zahl ist die Länge der gelieferten Kartenliste. Träte daneben ein Zählfeld,
    // gäbe es einen zweiten Ort für dieselbe Wahrheit, der veralten könnte.
    [Test]
    public async Task Wenn_ein_Board_mit_Karten_abgerufen_wird_dann_traegt_eine_Spalte_neben_ihren_Karten_keine_zweite_Zahl()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
        await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Endpunkt bauen");
        await SchalteKartenzahl(webApi, board.BoardId, true);

        var rumpf = await webApi.Klient.GetStringAsync($"{BoardsRoute}/{board.BoardId}");

        using var dokument = JsonDocument.Parse(rumpf);
        var ersteSpalte = dokument.RootElement.GetProperty("spalten")[0];
        Assert.Multiple(() =>
        {
            Assert.That(ersteSpalte.EnumerateObject().Select(feld => feld.Name),
                Is.EqualTo(new[] { "spalteId", "bezeichnung", "position", "istAbschlussspalte", "anzeigegrenze", "karten" }));
            Assert.That(ersteSpalte.GetProperty("karten").GetArrayLength(), Is.EqualTo(2));
        });
    }

    private static async Task LegeKarteAn(TestWebApi webApi, long boardId, long spalteId, string titel)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        antwort.EnsureSuccessStatusCode();
    }

    private static string KartenzahlRoute(long boardId)
    {
        return $"{BoardsRoute}/{boardId}/kartenzahl";
    }

    private static async Task<Board> SchalteKartenzahl(TestWebApi webApi, long boardId, bool zeigtKartenzahl)
    {
        var antwort = await webApi.Klient.PutAsJsonAsync(KartenzahlRoute(boardId), new Kartenzahlanzeige(zeigtKartenzahl));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        if (board is null)
        {
            throw new InvalidOperationException("Die API hat kein Board zurückgegeben.");
        }

        return board;
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, BoardAnlegenAnfrage anfrage)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, anfrage);
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        if (board is null)
        {
            throw new InvalidOperationException("Die API hat kein Board zurückgegeben.");
        }

        return board;
    }
}
