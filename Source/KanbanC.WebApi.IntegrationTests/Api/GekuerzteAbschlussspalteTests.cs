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

    [Test]
    public async Task Wenn_die_Karten_einer_Spalte_abgerufen_werden_dann_antwortet_die_API_mit_200_und_allen_Karten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        FuelleAbschlussspalte(datenbank, board, 23);
        Assert.That((await LadeBoard(webApi, board.BoardId)).Spalten[2].Karten, Has.Count.EqualTo(20));

        var antwort = await webApi.Klient.GetAsync(Kartenroute(board.BoardId, abschlussspalteId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var karten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Karte>>();
        Assert.Multiple(() =>
        {
            Assert.That(karten, Has.Count.EqualTo(23));
            Assert.That(karten!.Select(karte => karte.Position), Is.EqualTo(Enumerable.Range(1, 23)));
        });
    }

    // Die vollstaendige Liste steht in derselben Ordnung wie die gekuerzte, aus der sie
    // hervorgeht: sonst stuende die nachgeladene Bahn nach dem Klick anders da als vorher. Das
    // Arrange legt die Karten bewusst gegen die Datumsfolge an — nach Position waere „Gestern“
    // zuerst.
    [Test]
    public async Task Wenn_die_Karten_einer_Abschlussspalte_abgerufen_werden_dann_stehen_sie_in_Anzeigereihenfolge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        FuegeKarteEin(datenbank, abschlussspalteId, "Gestern fertig", 1, "2026-09-04");
        FuegeKarteEin(datenbank, abschlussspalteId, "Bestandskarte", 2, null);
        FuegeKarteEin(datenbank, abschlussspalteId, "Heute fertig", 3, "2026-09-05");

        var karten = await LadeKartenDerSpalte(webApi, board.BoardId, abschlussspalteId);

        Assert.That(karten.Select(karte => karte.Titel),
            Is.EqualTo(new[] { "Heute fertig", "Gestern fertig", "Bestandskarte" }));
    }

    [Test]
    public async Task Wenn_die_Spalte_keine_Abschlussspalte_ist_dann_bleibt_die_Positionsfolge_unangetastet()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var rueckstandId = board.Spalten[0].SpalteId;
        FuegeKarteEin(datenbank, rueckstandId, "Zuerst", 1, null);
        FuegeKarteEin(datenbank, rueckstandId, "Danach", 2, null);

        var karten = await LadeKartenDerSpalte(webApi, board.BoardId, rueckstandId);

        Assert.That(karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Zuerst", "Danach" }));
    }

    [Test]
    public async Task Wenn_die_Karten_einer_Spalte_abgerufen_werden_dann_traegt_jede_ihr_Erledigungsdatum()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        FuegeKarteEin(datenbank, abschlussspalteId, "Gestern fertig", 1, "2026-09-04");
        FuegeKarteEin(datenbank, abschlussspalteId, "Bestandskarte", 2, null);

        var karten = await LadeKartenDerSpalte(webApi, board.BoardId, abschlussspalteId);

        Assert.Multiple(() =>
        {
            Assert.That(karten[0].ErledigtAm, Is.EqualTo(new DateOnly(2026, 9, 4)));
            Assert.That(karten[1].ErledigtAm, Is.Null);
        });
    }

    [Test]
    public async Task Wenn_die_Spalte_keine_Abschlussmarkierung_traegt_dann_liefert_dieselbe_Adresse_ebenso_alle_ihre_Karten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var rueckstandId = board.Spalten[0].SpalteId;
        FuegeKarteEin(datenbank, rueckstandId, "Migration schreiben", 1, null);
        FuegeKarteEin(datenbank, rueckstandId, "Endpunkt bauen", 2, null);

        var karten = await LadeKartenDerSpalte(webApi, board.BoardId, rueckstandId);

        Assert.That(karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Migration schreiben", "Endpunkt bauen" }));
    }

    [Test]
    public async Task Wenn_die_Spalte_leer_ist_dann_liefert_die_Adresse_200_mit_einer_leeren_Liste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        var karten = await LadeKartenDerSpalte(webApi, board.BoardId, board.Spalten[1].SpalteId);

        Assert.That(karten, Is.Empty);
    }

    [Test]
    public async Task Wenn_die_boardId_unbekannt_ist_dann_antwortet_die_Adresse_mit_404_und_einem_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.GetAsync(Kartenroute(999, board.Spalten[0].SpalteId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Karten einer Spalte an unbekanntem Board");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    [Test]
    public async Task Wenn_die_spalteId_unbekannt_ist_dann_antwortet_die_Adresse_mit_404_und_einem_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.GetAsync(Kartenroute(board.BoardId, 999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Karten einer unbekannten Spalte");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("spalte-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain(board.BoardId.ToString()));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain($"GET /api/boards/{board.BoardId}"));
        });
    }

    // US-5: der Befund sagt, welches Board die Spalte traegt.
    [Test]
    public async Task Wenn_die_Spalte_zu_einem_anderen_Board_gehoert_dann_nennt_der_Befund_dieses_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var erstes = await LegeBoardAn(webApi);
        var zweites = await LegeBoardAn(webApi);
        var fremdeSpalteId = erstes.Spalten[0].SpalteId;

        var antwort = await webApi.Klient.GetAsync(Kartenroute(zweites.BoardId, fremdeSpalteId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Karten einer fremden Spalte");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("spalte-fremd"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain($"Board {erstes.BoardId}"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain($"GET /api/boards/{zweites.BoardId}"));
        });
    }

    private static async Task<IReadOnlyList<Karte>> LadeKartenDerSpalte(TestWebApi webApi, long boardId, long spalteId)
    {
        var karten = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Karte>>(Kartenroute(boardId, spalteId));
        if (karten is null)
        {
            throw new InvalidOperationException("Die API hat keine Kartenliste zurückgegeben.");
        }

        return karten;
    }

    private static string Kartenroute(long boardId, long spalteId)
    {
        return $"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten";
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
