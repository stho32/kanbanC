using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Dapper;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

public class KartenEndpunkteTests
{
    private const string BoardsRoute = "/api/boards";
    private const int HoechsteTitellaenge = 1000;
    private const int HoechsteTeilaufgabenlaenge = 200;
    private const int HoechsteKommentarlaenge = 2000;
    private const int HoechstePfadlaenge = 500;

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

    [Test]
    public async Task Wenn_eine_Karte_per_PUT_in_die_Abschlussspalte_zieht_dann_liefert_GET_auf_das_Board_das_heutige_erledigtAm()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        Assert.That((await LadeBoard(webApi, board.BoardId)).Spalten[0].Karten[0].ErledigtAm, Is.Null);

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(board.Spalten[2].SpalteId, 1));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[2].Karten[0].ErledigtAm, Is.EqualTo(DateOnly.FromDateTime(DateTime.Today)));
    }

    [Test]
    public async Task Wenn_eine_erledigte_Karte_innerhalb_der_Abschlussspalte_zieht_dann_bleibt_ihr_erledigtAm_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        var erste = await LegeKarteAn(webApi, board.BoardId, abschlussspalteId, "Zuerst fertig");
        await LegeKarteAn(webApi, board.BoardId, abschlussspalteId, "Danach fertig");
        SetzeErledigung(datenbank, erste.KarteId, "2026-09-01");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, erste.KarteId), new Kartenlage(abschlussspalteId, 2));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geladen = await LadeBoard(webApi, board.BoardId);
        var zuerstFertig = geladen.Spalten[2].Karten.Single(karte => karte.KarteId == erste.KarteId);
        Assert.That(zuerstFertig.ErledigtAm, Is.EqualTo(new DateOnly(2026, 9, 1)));
    }

    [Test]
    public async Task Wenn_eine_erledigte_Karte_die_Abschlussspalte_verlaesst_dann_ist_ihr_erledigtAm_wieder_null()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[2].SpalteId, "Doch nicht fertig");
        Assert.That((await LadeBoard(webApi, board.BoardId)).Spalten[2].Karten[0].ErledigtAm, Is.Not.Null);

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(board.Spalten[1].SpalteId, 1));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[1].Karten[0].ErledigtAm, Is.Null);
            Assert.That(Erledigungszeilen(datenbank), Is.Empty);
        });
    }

    [Test]
    public async Task Wenn_eine_zurueckgeholte_Karte_erneut_abgelegt_wird_dann_nennt_die_API_das_heutige_statt_des_frueheren_Datums()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        var karte = await LegeKarteAn(webApi, board.BoardId, abschlussspalteId, "Wieder aufgemacht");
        SetzeErledigung(datenbank, karte.KarteId, "2026-09-01");
        await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(board.Spalten[1].SpalteId, 1));

        await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(abschlussspalteId, 1));

        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.That(geladen.Spalten[2].Karten[0].ErledigtAm, Is.EqualTo(DateOnly.FromDateTime(DateTime.Today)));
    }

    [Test]
    public async Task Wenn_eine_Karte_direkt_in_der_Abschlussspalte_angelegt_wird_dann_traegt_schon_die_201_Antwort_das_heutige_Datum()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[2].SpalteId, "Sofort fertig");

        Assert.That(karte.ErledigtAm, Is.EqualTo(DateOnly.FromDateTime(DateTime.Today)));
    }

    [Test]
    public async Task Wenn_ein_Zug_zurueckgewiesen_wird_dann_ist_danach_kein_Erledigungsdatum_geschrieben()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(board.Spalten[2].SpalteId, 99));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(Erledigungszeilen(datenbank), Is.Empty);
    }

    // Karten, die vor dieser Anforderung in einer Abschlussspalte lagen, haben keine Zeile in
    // Karteerledigung; das Arrange setzt sie deshalb per SQL an der Anlage vorbei.
    [Test]
    public async Task Wenn_eine_Bestandskarte_in_der_Abschlussspalte_liegt_dann_liefert_die_API_erledigtAm_null()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        FuegeKarteOhneErledigungEin(datenbank, board.Spalten[2].SpalteId, "Vor der Anforderung fertig", 1);

        var geladen = await LadeBoard(webApi, board.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[2].Karten[0].Titel, Is.EqualTo("Vor der Anforderung fertig"));
            Assert.That(geladen.Spalten[2].Karten[0].ErledigtAm, Is.Null);
        });
    }

    private static void SetzeErledigung(TemporaereDatenbank datenbank, long karteId, string erledigtAm)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Karteerledigung (Karte, ErledigtAm)
            VALUES (@Karte, @ErledigtAm)
            ON CONFLICT (Karte) DO UPDATE SET ErledigtAm = excluded.ErledigtAm",
            new { Karte = karteId, ErledigtAm = erledigtAm });
    }

    private static void FuegeKarteOhneErledigungEin(TemporaereDatenbank datenbank, long spalteId, string titel, int position)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position)", new { Spalte = spalteId, Titel = titel, Position = position });
    }

    private static long[] Erledigungszeilen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<long>(@"
            SELECT Karte
              FROM Karteerledigung
             ORDER BY Karte").ToArray();
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

    [Test]
    public async Task Wenn_eine_Karte_per_PUT_archiviert_wird_dann_antwortet_die_API_mit_200_und_den_Spalten_ohne_sie()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        await LegeKarteAn(webApi, board.BoardId, spalteId, "C");

        var antwort = await webApi.Klient.PutAsJsonAsync(Archivierungsroute(board.BoardId, b.KarteId), new Archivierung(true));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var spalten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Spalte>>();
        Assert.That(spalten, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "C" }));
            Assert.That(spalten[0].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(spalten[0].Kartenzahl, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Wenn_eine_Karte_archiviert_ist_dann_fehlt_sie_im_geladenen_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        await LegeKarteAn(webApi, board.BoardId, spalteId, "C");
        await Archiviere(webApi, board.BoardId, b.KarteId, true);

        var geladen = await LadeBoard(webApi, board.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "C" }));
            Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1, 2 }));
        });
    }

    [Test]
    public async Task Wenn_eine_archivierte_Karte_zurueckgeholt_wird_dann_steht_sie_wieder_im_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        await Archiviere(webApi, board.BoardId, b.KarteId, true);

        var spalten = await Archiviere(webApi, board.BoardId, b.KarteId, false);

        Assert.Multiple(() =>
        {
            Assert.That(spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(spalten[0].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1, 2 }));
        });
    }

    // Die Route ist ein Umschalter auf einen Zielzustand, kein Ereignis: ein Agent darf denselben
    // Aufruf wiederholen, ohne einen Zustand zu zerstoeren, den er nicht kennt.
    [Test]
    public async Task Wenn_dieselbe_Karte_zweimal_archiviert_wird_dann_ist_der_zweite_Aufruf_kein_Fehler()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        await Archiviere(webApi, board.BoardId, b.KarteId, true);

        var antwort = await webApi.Klient.PutAsJsonAsync(Archivierungsroute(board.BoardId, b.KarteId), new Archivierung(true));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var spalten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Spalte>>();
        Assert.That(spalten!, Is.Not.Null);
        Assert.That(spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A" }));
    }

    [Test]
    public async Task Wenn_die_KarteId_unbekannt_ist_dann_antwortet_die_Archivierung_mit_404_und_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.PutAsJsonAsync(Archivierungsroute(board.BoardId, 999), new Archivierung(true));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Archivierung mit unbekannter KarteId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("999"));
        });
    }

    [Test]
    public async Task Wenn_die_Karte_zu_einem_anderen_Board_gehoert_dann_nennt_der_Befund_der_Archivierung_dieses_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var erstes = await LegeBoardAn(webApi);
        var zweites = await LegeBoardAn(webApi);
        var fremde = await LegeKarteAn(webApi, erstes.BoardId, erstes.Spalten[0].SpalteId, "A");

        var antwort = await webApi.Klient.PutAsJsonAsync(Archivierungsroute(zweites.BoardId, fremde.KarteId), new Archivierung(true));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Archivierung mit fremder KarteId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-fremd"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain($"Board {erstes.BoardId}"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain($"/api/boards/{erstes.BoardId}"));
        });
    }

    [Test]
    public async Task Wenn_die_Archivierung_zurueckgewiesen_wird_dann_bleibt_der_Bestand_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        await LegeKarteAn(webApi, board.BoardId, spalteId, "B");

        await webApi.Klient.PutAsJsonAsync(Archivierungsroute(board.BoardId, 999), new Archivierung(true));

        var geladen = await LadeBoard(webApi, board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1, 2 }));
        });
    }

    [Test]
    public async Task Wenn_die_Adresse_mit_archiviert_true_gerufen_wird_dann_liefert_sie_genau_die_archivierten_Karten_der_Spalte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        await LegeKarteAn(webApi, board.BoardId, spalteId, "C");
        await Archiviere(webApi, board.BoardId, b.KarteId, true);

        var antwort = await webApi.Klient.GetAsync($"{KartenRoute(board.BoardId, spalteId)}?archiviert=true");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var karten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Karte>>();
        Assert.That(karten!.Select(karte => karte.Titel), Is.EqualTo(new[] { "B" }));
    }

    [Test]
    public async Task Wenn_die_Adresse_ohne_Parameter_gerufen_wird_dann_liefert_sie_unveraendert_nur_die_aktiven_Karten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        await LegeKarteAn(webApi, board.BoardId, spalteId, "C");
        await Archiviere(webApi, board.BoardId, b.KarteId, true);

        var karten = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Karte>>(KartenRoute(board.BoardId, spalteId));

        Assert.That(karten!.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "C" }));
    }

    [Test]
    public async Task Wenn_die_Adresse_mit_archiviert_false_gerufen_wird_dann_liefert_sie_dasselbe_wie_ohne_Parameter()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        await Archiviere(webApi, board.BoardId, b.KarteId, true);

        var mitFalse = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Karte>>($"{KartenRoute(board.BoardId, spalteId)}?archiviert=false");
        var ohneParameter = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Karte>>(KartenRoute(board.BoardId, spalteId));

        Assert.That(mitFalse!.Select(karte => karte.Titel), Is.EqualTo(ohneParameter!.Select(karte => karte.Titel)));
    }

    // Leeres Archiv ist eine Antwort, kein Fehler: die Spalte gibt es, sie hat nur nichts abgelegt.
    [Test]
    public async Task Wenn_die_Spalte_keine_archivierte_Karte_traegt_dann_antwortet_das_Archiv_mit_200_und_leerer_Liste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");

        var antwort = await webApi.Klient.GetAsync($"{KartenRoute(board.BoardId, spalteId)}?archiviert=true");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var karten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Karte>>();
        Assert.That(karten, Is.Empty);
    }

    [Test]
    public async Task Wenn_der_Archivfilter_der_Kartenadresse_unlesbar_ist_dann_nennt_die_Kompensation_diese_Adresse()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;

        var antwort = await webApi.Klient.GetAsync($"{KartenRoute(board.BoardId, spalteId)}?archiviert=vielleicht");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Kartenadresse mit unlesbarem Archivfilter");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("archiv-filter-unlesbar"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("vielleicht"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain($"GET {KartenRoute(board.BoardId, spalteId)}?archiviert=true"));
        });
    }

    // Jede der beiden Adressen erklaert sich selbst: die Boardliste nennt weiterhin sich.
    [Test]
    public async Task Wenn_der_Archivfilter_der_Boardliste_unlesbar_ist_dann_nennt_die_Kompensation_weiterhin_die_Boardliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}?archiviert=vielleicht");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Boardliste mit unlesbarem Archivfilter");
        Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("GET /api/boards?archiviert=true"));
    }

    [Test]
    public async Task Wenn_die_BoardId_unbekannt_ist_dann_antwortet_auch_das_Archiv_mit_404_und_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/999/spalten/{board.Spalten[0].SpalteId}/karten?archiviert=true");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "board-unbekannt");
    }

    [Test]
    public async Task Wenn_die_SpalteId_unbekannt_ist_dann_antwortet_auch_das_Archiv_mit_404_und_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        var antwort = await webApi.Klient.GetAsync($"{KartenRoute(board.BoardId, 999)}?archiviert=true");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "spalte-unbekannt");
    }

    // Der Rundlauf, der „fort heisst nicht weg“ belegt: archivieren, im Archiv wiederfinden,
    // zurueckholen — und danach steht die Karte wieder in ihrer Bahn.
    [Test]
    public async Task Wenn_eine_Karte_archiviert_und_zurueckgeholt_wird_dann_steht_sie_dazwischen_nur_im_Archiv_und_danach_wieder_im_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        await LegeKarteAn(webApi, board.BoardId, spalteId, "C");

        await Archiviere(webApi, board.BoardId, b.KarteId, true);

        var imArchiv = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Karte>>($"{KartenRoute(board.BoardId, spalteId)}?archiviert=true");
        var waehrendDesArchivs = await LadeBoard(webApi, board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(imArchiv!.Select(karte => karte.Titel), Is.EqualTo(new[] { "B" }));
            Assert.That(waehrendDesArchivs.Spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "C" }));
        });

        await Archiviere(webApi, board.BoardId, b.KarteId, false);

        var danach = await LadeBoard(webApi, board.BoardId);
        var archivDanach = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Karte>>($"{KartenRoute(board.BoardId, spalteId)}?archiviert=true");
        Assert.Multiple(() =>
        {
            Assert.That(danach.Spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(danach.Spalten[0].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(archivDanach, Is.Empty);
        });
    }

    [Test]
    public async Task Wenn_eine_erledigte_Karte_den_Rundlauf_durchlaeuft_dann_bleibt_ihr_Erledigungsdatum_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        var fertig = await LegeKarteAn(webApi, board.BoardId, abschlussspalteId, "Fertig");
        Assert.That(fertig.ErledigtAm, Is.Not.Null);

        await Archiviere(webApi, board.BoardId, fertig.KarteId, true);
        var imArchiv = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Karte>>($"{KartenRoute(board.BoardId, abschlussspalteId)}?archiviert=true");
        await Archiviere(webApi, board.BoardId, fertig.KarteId, false);
        var danach = await LadeBoard(webApi, board.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(imArchiv![0].ErledigtAm, Is.EqualTo(fertig.ErledigtAm));
            Assert.That(danach.Spalten[2].Karten[0].ErledigtAm, Is.EqualTo(fertig.ErledigtAm));
        });
    }

    [Test]
    public async Task Wenn_eine_Karte_der_Zielspalte_archiviert_ist_dann_wird_die_Zielposition_gegen_die_aktiven_geprueft()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var zielspalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, zielspalteId, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, zielspalteId, "B");
        await LegeKarteAn(webApi, board.BoardId, zielspalteId, "C");
        await Archiviere(webApi, board.BoardId, b.KarteId, true);
        var wanderer = await LegeKarteAn(webApi, board.BoardId, board.Spalten[1].SpalteId, "Wanderer");

        var angenommen = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, wanderer.KarteId), new Kartenlage(zielspalteId, 3));
        Assert.That(angenommen.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var zurueck = await LegeKarteAn(webApi, board.BoardId, board.Spalten[1].SpalteId, "Zweiter Wanderer");
        var abgewiesen = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, zurueck.KarteId), new Kartenlage(zielspalteId, 5));

        Assert.That(abgewiesen.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(abgewiesen, "position-ausserhalb");
    }

    [Test]
    public async Task Wenn_eine_Karte_archiviert_ist_dann_bekommt_die_naechste_neue_Karte_die_Position_hinter_der_letzten_aktiven()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        await Archiviere(webApi, board.BoardId, b.KarteId, true);

        var neue = await LegeKarteAn(webApi, board.BoardId, spalteId, "C");

        Assert.That(neue.Position, Is.EqualTo(2));
    }

    // Eine archivierte Karte ist kein Bestand: sie ist weder Zugobjekt noch Bezugspunkt.
    [Test]
    public async Task Wenn_eine_archivierte_Karte_verschoben_werden_soll_dann_verhaelt_sich_die_Lage_wie_bei_einer_fehlenden_Karte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        await LegeKarteAn(webApi, board.BoardId, spalteId, "A");
        var b = await LegeKarteAn(webApi, board.BoardId, spalteId, "B");
        await Archiviere(webApi, board.BoardId, b.KarteId, true);

        var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, b.KarteId), new Kartenlage(spalteId, 1));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "karte-unbekannt");
    }

    // Die Kuerzung rechnet auf den aktiven Karten, ohne einen eigenen Archivbegriff zu lernen.
    [Test]
    public async Task Wenn_eine_Karte_der_vollen_Abschlussspalte_archiviert_wird_dann_ist_die_Bahn_nicht_mehr_gekuerzt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var abschlussspalteId = board.Spalten[2].SpalteId;
        var erste = await LegeKarteAn(webApi, board.BoardId, abschlussspalteId, "Fertig 1");
        for (var nummer = 2; nummer <= 21; nummer++)
        {
            await LegeKarteAn(webApi, board.BoardId, abschlussspalteId, $"Fertig {nummer}");
        }

        var vorher = await LadeBoard(webApi, board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(vorher.Spalten[2].Karten, Has.Count.EqualTo(20));
            Assert.That(vorher.Spalten[2].Kartenzahl, Is.EqualTo(21));
        });

        await Archiviere(webApi, board.BoardId, erste.KarteId, true);

        var nachher = await LadeBoard(webApi, board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(nachher.Spalten[2].Karten, Has.Count.EqualTo(20));
            Assert.That(nachher.Spalten[2].Kartenzahl, Is.EqualTo(20));
        });
    }

    private static async Task<IReadOnlyList<Spalte>> Archiviere(TestWebApi webApi, long boardId, long karteId, bool istArchiviert)
    {
        var antwort = await webApi.Klient.PutAsJsonAsync(Archivierungsroute(boardId, karteId), new Archivierung(istArchiviert));
        antwort.EnsureSuccessStatusCode();
        var spalten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Spalte>>();
        if (spalten is null)
        {
            throw new InvalidOperationException("Die API hat keine Spalten zurückgegeben.");
        }

        return spalten;
    }

    private static string Archivierungsroute(long boardId, long karteId)
    {
        return $"{BoardsRoute}/{boardId}/karten/{karteId}/archivierung";
    }

    private static string Lageroute(long boardId, long karteId)
    {
        return $"{BoardsRoute}/{boardId}/karten/{karteId}/lage";
    }

    [Test]
    public async Task Wenn_die_KarteId_bekannt_ist_dann_liefert_GET_karten_das_Kartendetail_mit_Board_und_Spalte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalte = board.Spalten[1];
        var karte = await LegeKarteAn(webApi, board.BoardId, spalte.SpalteId, "Migration schreiben");

        using var antwort = await webApi.Klient.GetAsync(Kartendetailroute(karte.KarteId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        Assert.That(detail, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(detail.Karte.KarteId, Is.EqualTo(karte.KarteId));
            Assert.That(detail.Karte.Titel, Is.EqualTo("Migration schreiben"));
            Assert.That(detail.Board, Is.EqualTo(board.BoardId));
            Assert.That(detail.Boardname, Is.EqualTo("Entwicklung"));
            Assert.That(detail.Spalte, Is.EqualTo(spalte.SpalteId));
            Assert.That(detail.Spaltenbezeichnung, Is.EqualTo(spalte.Bezeichnung));
        });
    }

    // US-7: eine archivierte Karte verschwindet vom Board, behaelt aber ihre Adresse.
    [Test]
    public async Task Wenn_die_Karte_archiviert_ist_dann_liefert_GET_karten_dasselbe_Kartendetail_wie_zuvor()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        using var vorher = await webApi.Klient.GetAsync(Kartendetailroute(karte.KarteId));
        var detailVorher = await vorher.Content.ReadFromJsonAsync<Kartendetail>();
        await Archiviere(webApi, board.BoardId, karte.KarteId, istArchiviert: true);
        await ErwarteBoardOhneKarten(webApi, board.BoardId);

        using var antwort = await webApi.Klient.GetAsync(Kartendetailroute(karte.KarteId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        Kartendetailvergleich.ErwarteGleichesDetail(detail, detailVorher);
    }

    [Test]
    public async Task Wenn_die_KarteId_unbekannt_ist_dann_antwortet_GET_karten_mit_404_und_einem_Befund_ohne_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeBoardAn(webApi);

        using var antwort = await webApi.Klient.GetAsync(Kartendetailroute(9999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Kartendetail mit unbekannter KarteId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("9999"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("Board"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    [Test]
    public async Task Wenn_PUT_karten_die_vier_Felder_setzt_dann_antwortet_die_API_mit_dem_vollstaendigen_Kartendetail()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");

        using var antwort = await webApi.Klient.PutAsJsonAsync(
            Kartendetailroute(karte.KarteId),
            new KarteAendernAnfrage("WBS-Import", "Knoten in Karten überführen", new DateOnly(2026, 9, 2), Kartenfarbe.Terrakotta, Kontributor: null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        Assert.That(detail, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(detail.Karte.Titel, Is.EqualTo("WBS-Import"));
            Assert.That(detail.Karte.Beschreibung, Is.EqualTo("Knoten in Karten überführen"));
            Assert.That(detail.Karte.FaelligAm, Is.EqualTo(new DateOnly(2026, 9, 2)));
            Assert.That(detail.Karte.Farbe, Is.EqualTo(Kartenfarbe.Terrakotta));
            Assert.That(detail.Boardname, Is.EqualTo("Entwicklung"));
        });
    }

    // US-3: die geaenderten Werte reisen an der Karte mit und stehen ohne zweiten Aufruf auch
    // in der Boardantwort.
    [Test]
    public async Task Wenn_die_Karte_geaendert_wurde_dann_traegt_sie_ihre_drei_Werte_auch_in_der_Boardantwort()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        await Aendere(webApi, karte.KarteId, new KarteAendernAnfrage("WBS-Import", "Knoten überführen", new DateOnly(2026, 9, 2), Kartenfarbe.Olive, Kontributor: null));

        var geladen = await LadeBoard(webApi, board.BoardId);

        var ausDemBoard = geladen.Spalten[0].Karten[0];
        Assert.Multiple(() =>
        {
            Assert.That(ausDemBoard.Titel, Is.EqualTo("WBS-Import"));
            Assert.That(ausDemBoard.Beschreibung, Is.EqualTo("Knoten überführen"));
            Assert.That(ausDemBoard.FaelligAm, Is.EqualTo(new DateOnly(2026, 9, 2)));
            Assert.That(ausDemBoard.Farbe, Is.EqualTo(Kartenfarbe.Olive));
        });
    }

    [Test]
    public async Task Wenn_eine_Karte_frisch_angelegt_ist_dann_liefert_das_Kartendetail_null_null_und_Farbe_Ohne()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");

        using var antwort = await webApi.Klient.GetAsync(Kartendetailroute(karte.KarteId));

        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        Assert.Multiple(() =>
        {
            Assert.That(detail!.Karte.Beschreibung, Is.Null);
            Assert.That(detail.Karte.FaelligAm, Is.Null);
            Assert.That(detail.Karte.Farbe, Is.EqualTo(Kartenfarbe.Ohne));
        });
    }

    [Test]
    public async Task Wenn_der_Titel_beim_Aendern_geleert_wird_dann_antwortet_die_API_mit_400_und_es_bleibt_alles_stehen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var vorher = await Aendere(webApi, karte.KarteId, new KarteAendernAnfrage("WBS-Import", "Knoten überführen", new DateOnly(2026, 9, 2), Kartenfarbe.Terrakotta, Kontributor: null));

        using var antwort = await webApi.Klient.PutAsJsonAsync(Kartendetailroute(karte.KarteId), new KarteAendernAnfrage("", null, null, Kartenfarbe.Ohne, Kontributor: null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Karte ändern mit geleertem Titel");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kartentitel-leer"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Is.EqualTo("Der Titel darf nicht leer sein."));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain($"PUT /api/karten/{karte.KarteId}"));
        });
        using var danach = await webApi.Klient.GetAsync(Kartendetailroute(karte.KarteId));
        Kartendetailvergleich.ErwarteGleichesDetail(await danach.Content.ReadFromJsonAsync<Kartendetail>(), vorher);
    }

    [Test]
    public async Task Wenn_die_KarteId_unbekannt_ist_dann_antwortet_PUT_karten_mit_404_und_einem_Befund_ohne_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeBoardAn(webApi);

        using var antwort = await webApi.Klient.PutAsJsonAsync(Kartendetailroute(9999), new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Karte ändern mit unbekannter KarteId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("9999"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("Board"));
        });
    }

    [Test]
    public async Task Wenn_ein_aktiver_Kontributor_gesetzt_wird_dann_traegt_das_Kartendetail_ihn_mit_Name_und_Art()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var agent = await LegeKontributorAn(webApi, "Claude-Agent", Kontributorart.Agent);

        var detail = await Aendere(webApi, karte.KarteId, new KarteAendernAnfrage("Migration schreiben", null, null, Kartenfarbe.Ohne, agent.KontributorId));

        Assert.Multiple(() =>
        {
            Assert.That(detail.Karte.Kontributor, Is.EqualTo(agent.KontributorId));
            Assert.That(detail.Verantwortlicher, Is.EqualTo(agent));
        });
    }

    [Test]
    public async Task Wenn_die_Kontributornummer_unbekannt_ist_dann_antwortet_PUT_karten_mit_404_und_Befund_und_speichert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var vorher = await Aendere(webApi, karte.KarteId, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: null));

        using var antwort = await webApi.Klient.PutAsJsonAsync(
            Kartendetailroute(karte.KarteId),
            new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: 999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kontributor-unbekannt");
        using var danach = await webApi.Klient.GetAsync(Kartendetailroute(karte.KarteId));
        Kartendetailvergleich.ErwarteGleichesDetail(await danach.Content.ReadFromJsonAsync<Kartendetail>(), vorher);
    }

    // Der Stillgelegte ist eine Regelverletzung, kein fehlendes Ding — deshalb 400 und nicht 404.
    [Test]
    public async Task Wenn_der_Kontributor_stillgelegt_ist_dann_antwortet_PUT_karten_mit_400_und_eigenem_Befund_und_speichert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var jan = await LegeKontributorAn(webApi, "Jan R.", Kontributorart.Mensch);
        var vorher = await Aendere(webApi, karte.KarteId, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: null));
        var stillgelegt = await webApi.Klient.PutAsJsonAsync($"/api/kontributoren/{jan.KontributorId}/stilllegung", new Stilllegung(true));
        stillgelegt.EnsureSuccessStatusCode();

        using var antwort = await webApi.Klient.PutAsJsonAsync(
            Kartendetailroute(karte.KarteId),
            new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, jan.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kontributor-stillgelegt");
        using var danach = await webApi.Klient.GetAsync(Kartendetailroute(karte.KarteId));
        Kartendetailvergleich.ErwarteGleichesDetail(await danach.Content.ReadFromJsonAsync<Kartendetail>(), vorher);
    }

    // Die Einloesung der zweiten Haelfte von I0009 ueber die API: gesetzt bleibt gesetzt.
    [Test]
    public async Task Wenn_der_gesetzte_Verantwortliche_danach_stillgelegt_wird_dann_steht_er_weiterhin_am_Kartendetail()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var jan = await LegeKontributorAn(webApi, "Jan R.", Kontributorart.Mensch);
        await Aendere(webApi, karte.KarteId, new KarteAendernAnfrage("Migration schreiben", null, null, Kartenfarbe.Ohne, jan.KontributorId));

        var stillgelegt = await webApi.Klient.PutAsJsonAsync($"/api/kontributoren/{jan.KontributorId}/stilllegung", new Stilllegung(true));
        stillgelegt.EnsureSuccessStatusCode();

        using var antwort = await webApi.Klient.GetAsync(Kartendetailroute(karte.KarteId));
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        Assert.Multiple(() =>
        {
            Assert.That(detail!.Verantwortlicher!.Name, Is.EqualTo("Jan R."));
            Assert.That(detail.Verantwortlicher.StillgelegtAm, Is.Not.Null);
        });
    }

    [Test]
    public async Task Wenn_PUT_etiketten_eine_Liste_setzt_dann_ist_sie_danach_exakt_die_Liste_der_Karte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        await SetzeEtiketten(webApi, karte.KarteId, ["Import", "Doku"]);

        var detail = await SetzeEtiketten(webApi, karte.KarteId, ["Doku", "Refactoring"]);

        Assert.That(detail.Etiketten, Is.EqualTo(new[] { "Doku", "Refactoring" }));
    }

    [Test]
    public async Task Wenn_PUT_etiketten_eine_leere_Liste_setzt_dann_traegt_die_Karte_danach_keine_Etiketten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        await SetzeEtiketten(webApi, karte.KarteId, ["Import"]);

        var detail = await SetzeEtiketten(webApi, karte.KarteId, []);

        Assert.That(detail.Etiketten, Is.Empty);
    }

    // US-3: die Etiketten haengen am Kartendetail und nicht an der Karte — die Boardantwort
    // bleibt unveraendert.
    [Test]
    public async Task Wenn_eine_Karte_Etiketten_traegt_dann_bekommt_die_Boardantwort_keine_Etikettenliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        await SetzeEtiketten(webApi, karte.KarteId, ["Import"]);

        var rumpf = await webApi.Klient.GetStringAsync($"{BoardsRoute}/{board.BoardId}");

        Assert.That(rumpf, Does.Not.Contain("etiketten"));
        Assert.That(rumpf, Does.Not.Contain("Import"));
    }

    [Test]
    public async Task Wenn_ein_Etikett_leer_ist_dann_antwortet_PUT_etiketten_mit_400_und_Befund_und_speichert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        await SetzeEtiketten(webApi, karte.KarteId, ["Import"]);

        using var antwort = await webApi.Klient.PutAsJsonAsync(Etikettenroute(karte.KarteId), new Kartenetiketten(["Import", "  "]));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "etikett-leer");
        using var danach = await webApi.Klient.GetAsync(Kartendetailroute(karte.KarteId));
        var detail = await danach.Content.ReadFromJsonAsync<Kartendetail>();
        Assert.That(detail!.Etiketten, Is.EqualTo(new[] { "Import" }));
    }

    [Test]
    public async Task Wenn_ein_Etikett_doppelt_uebergeben_wird_dann_antwortet_PUT_etiketten_mit_400_und_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");

        using var antwort = await webApi.Klient.PutAsJsonAsync(Etikettenroute(karte.KarteId), new Kartenetiketten(["Import", " Import "]));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "etikett-doppelt");
    }

    [Test]
    public async Task Wenn_die_KarteId_unbekannt_ist_dann_antwortet_PUT_etiketten_mit_404_und_einem_Befund_ohne_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeBoardAn(webApi);

        using var antwort = await webApi.Klient.PutAsJsonAsync(Etikettenroute(9999), new Kartenetiketten(["Import"]));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Etiketten setzen mit unbekannter KarteId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("Board"));
        });
    }

    // Die Zusage „reisen an Karte mit" gilt auch fuer die Antwort des Zugs — sie traegt die
    // Spalten und damit dieselben Karten.
    [Test]
    public async Task Wenn_die_geaenderte_Karte_gezogen_wird_dann_traegt_sie_ihre_Werte_auch_in_der_Antwort_von_PUT_lage()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var zielspalteId = board.Spalten[1].SpalteId;
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        await Aendere(webApi, karte.KarteId, new KarteAendernAnfrage("WBS-Import", "Knoten überführen", new DateOnly(2026, 9, 2), Kartenfarbe.Olive, Kontributor: null));

        using var antwort = await webApi.Klient.PutAsJsonAsync(Lageroute(board.BoardId, karte.KarteId), new Kartenlage(zielspalteId, 1));

        antwort.EnsureSuccessStatusCode();
        var spalten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Spalte>>();
        var gezogene = spalten!.Single(spalte => spalte.SpalteId == zielspalteId).Karten[0];
        Assert.Multiple(() =>
        {
            Assert.That(gezogene.Titel, Is.EqualTo("WBS-Import"));
            Assert.That(gezogene.Beschreibung, Is.EqualTo("Knoten überführen"));
            Assert.That(gezogene.FaelligAm, Is.EqualTo(new DateOnly(2026, 9, 2)));
            Assert.That(gezogene.Farbe, Is.EqualTo(Kartenfarbe.Olive));
        });
    }

    // US-4: die Antwort traegt die ganze Seite, nicht die angelegte Zeile — und 200, nicht 201.
    [Test]
    public async Task Wenn_eine_Teilaufgabe_per_POST_angelegt_wird_dann_antwortet_die_API_mit_200_und_dem_ganzen_Kartendetail()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");

        using var antwort = await webApi.Klient.PostAsJsonAsync(Teilaufgabenroute(karte.KarteId), new TeilaufgabeAnlegenAnfrage("Lizenztext lesen"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        Assert.Multiple(() =>
        {
            Assert.That(detail!.Karte.Titel, Is.EqualTo("Playwright-Lizenz klären"));
            Assert.That(detail.Boardname, Is.EqualTo("Entwicklung"));
            Assert.That(detail.Teilaufgaben.Select(teilaufgabe => teilaufgabe.Text), Is.EqualTo(new[] { "Lizenztext lesen" }));
            Assert.That(detail.Teilaufgaben[0].TeilaufgabeId, Is.GreaterThan(0));
            Assert.That(detail.Teilaufgaben[0].Abgehakt, Is.False);
        });
    }

    // Das Rechenbeispiel der Anforderung: A, B, C angelegt, danach liest GET dieselbe Reihenfolge.
    [Test]
    public async Task Wenn_drei_Teilaufgaben_angelegt_werden_dann_liefert_GET_karten_sie_in_Anlegereihenfolge_mit_eigenen_Nummern()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        await LegeTeilaufgabeAn(webApi, karte.KarteId, "A");
        await LegeTeilaufgabeAn(webApi, karte.KarteId, "B");
        await LegeTeilaufgabeAn(webApi, karte.KarteId, "C");

        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(karte.KarteId));

        Assert.That(detail!.Teilaufgaben.Select(teilaufgabe => teilaufgabe.Text), Is.EqualTo(new[] { "A", "B", "C" }));
        Assert.That(detail.Teilaufgaben.Select(teilaufgabe => teilaufgabe.TeilaufgabeId).Distinct().Count(), Is.EqualTo(3));
    }

    // Zwei gleich benannte Arbeiten sind zwei Arbeiten — anders als bei zwei gleichen Etiketten.
    [Test]
    public async Task Wenn_derselbe_Text_zweimal_angelegt_wird_dann_stehen_zwei_Eintraege_mit_verschiedenen_Nummern()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        await LegeTeilaufgabeAn(webApi, karte.KarteId, "Nachfassen");

        var detail = await LegeTeilaufgabeAn(webApi, karte.KarteId, "Nachfassen");

        Assert.That(detail.Teilaufgaben.Select(teilaufgabe => teilaufgabe.Text), Is.EqualTo(new[] { "Nachfassen", "Nachfassen" }));
        Assert.That(detail.Teilaufgaben[0].TeilaufgabeId, Is.Not.EqualTo(detail.Teilaufgaben[1].TeilaufgabeId));
    }

    // US-5: die Teilaufgaben haengen am Kartendetail und nicht an der Karte — die Boardantwort
    // bleibt unveraendert, und ein Fortschrittsfeld gibt es nirgends.
    [Test]
    public async Task Wenn_eine_Karte_Teilaufgaben_traegt_dann_bekommt_die_Boardantwort_keine_Teilaufgabenliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        await LegeTeilaufgabeAn(webApi, karte.KarteId, "Lizenztext lesen");

        var rumpf = await webApi.Klient.GetStringAsync($"{BoardsRoute}/{board.BoardId}");

        Assert.Multiple(() =>
        {
            Assert.That(rumpf, Does.Not.Contain("teilaufgaben"));
            Assert.That(rumpf, Does.Not.Contain("Lizenztext lesen"));
        });
    }

    // Der Fortschritt wird gerechnet, nicht gespeichert: kein Feld der Antwort traegt ihn.
    [Test]
    public async Task Wenn_das_Kartendetail_gelesen_wird_dann_traegt_es_kein_Feld_mit_dem_Fortschritt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        await LegeTeilaufgabeAn(webApi, karte.KarteId, "A");
        await LegeTeilaufgabeAn(webApi, karte.KarteId, "B");

        var rumpf = await webApi.Klient.GetStringAsync(Kartendetailroute(karte.KarteId));

        Assert.Multiple(() =>
        {
            Assert.That(rumpf, Does.Not.Contain("fortschritt"));
            Assert.That(rumpf, Does.Not.Contain("abgehaktezahl"));
            Assert.That(rumpf, Does.Contain("\"teilaufgaben\""));
        });
    }

    // Das Rechenbeispiel der Anforderung: Karte mit A, B, C; B abhaken laesst A und C offen.
    [Test]
    public async Task Wenn_eine_Teilaufgabe_abgehakt_wird_dann_ist_genau_sie_abgehakt_und_die_uebrigen_stehen_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        await LegeTeilaufgabeAn(webApi, karte.KarteId, "A");
        await LegeTeilaufgabeAn(webApi, karte.KarteId, "B");
        var vorher = (await LegeTeilaufgabeAn(webApi, karte.KarteId, "C")).Teilaufgaben;

        var detail = await SetzeAbhakung(webApi, karte.KarteId, vorher[1].TeilaufgabeId, abgehakt: true);

        Assert.That(detail.Teilaufgaben.Select(teilaufgabe => teilaufgabe.Abgehakt), Is.EqualTo(new[] { false, true, false }));
        Assert.Multiple(() =>
        {
            Assert.That(detail.Teilaufgaben.Select(teilaufgabe => teilaufgabe.Text), Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(detail.Teilaufgaben.Select(teilaufgabe => teilaufgabe.TeilaufgabeId),
                Is.EqualTo(vorher.Select(teilaufgabe => teilaufgabe.TeilaufgabeId)));
        });
    }

    // Der Stand kippt nicht: derselbe Aufruf ein zweites Mal antwortet mit 200 und demselben Stand.
    [Test]
    public async Task Wenn_derselbe_Stand_ein_zweites_Mal_gesetzt_wird_dann_antwortet_die_API_mit_200_und_aendert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var teilaufgabeId = (await LegeTeilaufgabeAn(webApi, karte.KarteId, "Lizenztext lesen")).Teilaufgaben[0].TeilaufgabeId;
        var nachDemErsten = await SetzeAbhakung(webApi, karte.KarteId, teilaufgabeId, abgehakt: true);

        using var antwort = await webApi.Klient.PutAsJsonAsync(Teilaufgabenstandsroute(karte.KarteId, teilaufgabeId), new Teilaufgabenstand(true));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var nachDemZweiten = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        Assert.That(nachDemZweiten!.Teilaufgaben, Is.EqualTo(nachDemErsten.Teilaufgaben));
    }

    [Test]
    public async Task Wenn_der_Stand_auf_nicht_abgehakt_gesetzt_wird_dann_nimmt_er_das_Abhaken_zurueck()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var teilaufgabeId = (await LegeTeilaufgabeAn(webApi, karte.KarteId, "Lizenztext lesen")).Teilaufgaben[0].TeilaufgabeId;
        await SetzeAbhakung(webApi, karte.KarteId, teilaufgabeId, abgehakt: true);

        var detail = await SetzeAbhakung(webApi, karte.KarteId, teilaufgabeId, abgehakt: false);

        Assert.That(detail.Teilaufgaben[0].Abgehakt, Is.False);
    }

    [Test]
    public async Task Wenn_der_Text_leer_ist_dann_antwortet_POST_teilaufgaben_mit_400_und_Befund_und_speichert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        await LegeTeilaufgabeAn(webApi, karte.KarteId, "Lizenztext lesen");

        using var antwort = await webApi.Klient.PostAsJsonAsync(Teilaufgabenroute(karte.KarteId), new TeilaufgabeAnlegenAnfrage("   "));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "teilaufgabe-leer");
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(karte.KarteId));
        Assert.That(detail!.Teilaufgaben.Select(teilaufgabe => teilaufgabe.Text), Is.EqualTo(new[] { "Lizenztext lesen" }));
    }

    [Test]
    public async Task Wenn_der_Text_zu_lang_ist_dann_antwortet_POST_teilaufgaben_mit_400_und_Befund_und_speichert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");

        using var antwort = await webApi.Klient.PostAsJsonAsync(
            Teilaufgabenroute(karte.KarteId),
            new TeilaufgabeAnlegenAnfrage(new string('a', HoechsteTeilaufgabenlaenge + 1)));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "teilaufgabe-zu-lang");
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(karte.KarteId));
        Assert.That(detail!.Teilaufgaben, Is.Empty);
    }

    // Das Rechenbeispiel der Anforderung: „  Kaffee  " wird als „Kaffee" gespeichert.
    [Test]
    public async Task Wenn_der_Text_Randleerzeichen_traegt_dann_steht_er_ohne_sie_in_der_Liste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");

        var detail = await LegeTeilaufgabeAn(webApi, karte.KarteId, "  Kaffee Holen  ");

        Assert.That(detail.Teilaufgaben.Select(teilaufgabe => teilaufgabe.Text), Is.EqualTo(new[] { "Kaffee Holen" }));
    }

    [Test]
    public async Task Wenn_die_KarteId_unbekannt_ist_dann_antwortet_POST_teilaufgaben_mit_404_und_einem_Befund_ohne_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeBoardAn(webApi);

        using var antwort = await webApi.Klient.PostAsJsonAsync(Teilaufgabenroute(9999), new TeilaufgabeAnlegenAnfrage("Lizenztext lesen"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Teilaufgabe anlegen mit unbekannter KarteId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("9999"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("Board"));
        });
    }

    [Test]
    public async Task Wenn_die_KarteId_unbekannt_ist_dann_antwortet_PUT_teilaufgabenstand_mit_404_und_einem_Befund_ohne_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeBoardAn(webApi);

        using var antwort = await webApi.Klient.PutAsJsonAsync(Teilaufgabenstandsroute(9999, 1), new Teilaufgabenstand(true));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Teilaufgabe abhaken an unbekannter Karte");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("9999"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("Board"));
        });
    }

    // US-4: die Teilaufgabe gehoert zur Karte 14, aufgerufen wird sie an der Karte 15 — der Befund
    // nennt beide Nummern, und an keiner der beiden Karten hat sich etwas geaendert.
    [Test]
    public async Task Wenn_die_Teilaufgabe_zu_einer_anderen_Karte_gehoert_dann_antwortet_PUT_mit_404_und_nennt_beide_Nummern()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var spalteId = board.Spalten[0].SpalteId;
        var eigene = await LegeKarteAn(webApi, board.BoardId, spalteId, "Eigene");
        var fremde = await LegeKarteAn(webApi, board.BoardId, spalteId, "Fremde");
        await LegeTeilaufgabeAn(webApi, eigene.KarteId, "Lizenztext lesen");
        var fremdeTeilaufgabeId = (await LegeTeilaufgabeAn(webApi, fremde.KarteId, "Nur woanders")).Teilaufgaben[0].TeilaufgabeId;

        using var antwort = await webApi.Klient.PutAsJsonAsync(Teilaufgabenstandsroute(eigene.KarteId, fremdeTeilaufgabeId), new Teilaufgabenstand(true));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Teilaufgabe einer fremden Karte abhaken");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("teilaufgabe-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain(fremdeTeilaufgabeId.ToString(CultureInfo.InvariantCulture)));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain(eigene.KarteId.ToString(CultureInfo.InvariantCulture)));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Is.Not.Empty);
        });
        await ErwarteKeineAbgehakteTeilaufgabe(webApi, eigene.KarteId);
        await ErwarteKeineAbgehakteTeilaufgabe(webApi, fremde.KarteId);
    }

    // US-4: die Antwort ist 200 mit dem **ganzen** Kartendetail, nicht 201 mit der geschriebenen
    // Zeile — dieselbe Antwortgestalt, die diese Seite ueberall hat.
    [Test]
    public async Task Wenn_ein_Kommentar_geschrieben_wird_dann_antwortet_POST_kommentare_mit_200_und_dem_ganzen_Kartendetail()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsJsonAsync(
            Kommentarroute(aufbau.KarteId),
            new KommentarSchreibenAnfrage("Der Parser liest jetzt auch Ebene 4.", aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await AlsKartendetail(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(detail.Karte.Titel, Is.EqualTo("Playwright-Lizenz klären"));
            Assert.That(detail.Boardname, Is.EqualTo("Entwicklung"));
            Assert.That(detail.Kommentare[^1].Text, Is.EqualTo("Der Parser liest jetzt auch Ebene 4."));
            Assert.That(detail.Kommentare[^1].KommentarId, Is.GreaterThan(0));
        });
    }

    // Der Eintrag traegt den **ganzen** Urheber und nicht nur seine Nummer, und einen Zeitpunkt
    // im Fenster des Aufrufs — das Rechenbeispiel der Anforderung.
    [Test]
    public async Task Wenn_ein_Kommentar_geschrieben_wird_dann_traegt_er_den_ganzen_Urheber_und_einen_Zeitpunkt_im_Fenster_des_Aufrufs()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        var vorher = DateTimeOffset.UtcNow;
        var detail = await SchreibeKommentar(webApi, aufbau.KarteId, "Bitte prüfen", aufbau.Urheber.KontributorId);
        var nachher = DateTimeOffset.UtcNow;

        Assert.Multiple(() =>
        {
            Assert.That(detail.Kommentare[0].Urheber, Is.EqualTo(aufbau.Urheber));
            Assert.That(detail.Kommentare[0].Zeitpunkt, Is.GreaterThanOrEqualTo(vorher));
            Assert.That(detail.Kommentare[0].Zeitpunkt, Is.LessThanOrEqualTo(nachher));
        });
    }

    // Der Aufrufer kann den Zeitpunkt nicht mitgeben: ein mitgeschicktes Feld aendert nichts am
    // gespeicherten Wert — sonst koennte ein Agent die Reihenfolge des Gespraechs faelschen.
    [Test]
    public async Task Wenn_der_Aufruf_einen_Zeitpunkt_mitschickt_dann_bleibt_der_gespeicherte_Wert_der_der_Anwendung()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var gefaelscht = new DateTimeOffset(1999, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var rumpf = JsonContent.Create(new
        {
            text = "Bitte prüfen",
            kontributor = aufbau.Urheber.KontributorId,
            zeitpunkt = gefaelscht,
        });

        using var antwort = await webApi.Klient.PostAsync(Kommentarroute(aufbau.KarteId), rumpf);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await AlsKartendetail(antwort);
        Assert.That(detail.Kommentare[0].Zeitpunkt, Is.GreaterThan(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    // Das Rechenbeispiel der Reihenfolge: A, B, C nacheinander geschrieben, GET liefert dieselbe
    // Liste in derselben Reihenfolge.
    [Test]
    public async Task Wenn_drei_Kommentare_geschrieben_werden_dann_liefert_GET_sie_in_derselben_Reihenfolge_mit_eigenen_Nummern()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await SchreibeKommentar(webApi, aufbau.KarteId, "A", aufbau.Urheber.KontributorId);
        await SchreibeKommentar(webApi, aufbau.KarteId, "B", aufbau.Urheber.KontributorId);
        var nachDemSchreiben = await SchreibeKommentar(webApi, aufbau.KarteId, "C", aufbau.Urheber.KontributorId);

        var gelesen = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));

        Assert.Multiple(() =>
        {
            Assert.That(nachDemSchreiben.Kommentare.Select(kommentar => kommentar.Text), Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(gelesen!.Kommentare.Select(kommentar => kommentar.Text), Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(gelesen.Kommentare.Select(kommentar => kommentar.KommentarId), Is.EqualTo(nachDemSchreiben.Kommentare.Select(kommentar => kommentar.KommentarId)));
            Assert.That(gelesen.Kommentare.Select(kommentar => kommentar.KommentarId).Distinct().Count(), Is.EqualTo(3));
        });
    }

    // Die Sortierung ist die Zeitordnung und nicht die des Schreibens: nachtraeglich anders
    // datiert, steht der aeltere oben.
    [Test]
    public async Task Wenn_zwei_Kommentare_nachtraeglich_anders_datiert_werden_dann_steht_der_aeltere_oben()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var beide = await SchreibeZwei(webApi, aufbau, "Zuerst geschrieben", "Danach geschrieben");
        DatiereZurueck(datenbank, beide.Kommentare[1].KommentarId, new DateTimeOffset(2020, 1, 1, 8, 0, 0, TimeSpan.Zero));

        var gelesen = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));

        Assert.That(gelesen!.Kommentare.Select(kommentar => kommentar.Text), Is.EqualTo(new[] { "Danach geschrieben", "Zuerst geschrieben" }));
    }

    [Test]
    public async Task Wenn_derselbe_Kommentar_zweimal_geschrieben_wird_dann_stehen_zwei_Eintraege_mit_verschiedenen_Nummern()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        await SchreibeKommentar(webApi, aufbau.KarteId, "Nachfassen", aufbau.Urheber.KontributorId);
        var detail = await SchreibeKommentar(webApi, aufbau.KarteId, "Nachfassen", aufbau.Urheber.KontributorId);

        Assert.That(detail.Kommentare.Select(kommentar => kommentar.Text), Is.EqualTo(new[] { "Nachfassen", "Nachfassen" }));
        Assert.That(detail.Kommentare[0].KommentarId, Is.Not.EqualTo(detail.Kommentare[1].KommentarId));
    }

    // Das Rechenbeispiel von US-3: die Randleerzeichen fallen weg, Gross- und Kleinschreibung
    // bleibt.
    [Test]
    public async Task Wenn_der_Kommentartext_Randleerzeichen_traegt_dann_steht_er_ohne_sie_in_der_Liste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        var detail = await SchreibeKommentar(webApi, aufbau.KarteId, "  Bitte Prüfen  ", aufbau.Urheber.KontributorId);

        Assert.That(detail.Kommentare.Select(kommentar => kommentar.Text), Is.EqualTo(new[] { "Bitte Prüfen" }));
    }

    // US-6: wer geht, bleibt an seinen alten Aeusserungen sichtbar — mit Name und
    // Stilllegungsstand.
    [Test]
    public async Task Wenn_der_Urheber_nach_dem_Kommentar_stillgelegt_wird_dann_bleibt_er_an_der_Karte_sichtbar()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var maria = await LegeKontributorAn(webApi, "Maria Lenz", Kontributorart.Mensch);
        await SchreibeKommentar(webApi, karte.KarteId, "Die Lizenz gilt nur pro Rechner.", maria.KontributorId);

        using var stillgelegt = await webApi.Klient.PutAsJsonAsync($"/api/kontributoren/{maria.KontributorId}/stilllegung", new Stilllegung(true));
        stillgelegt.EnsureSuccessStatusCode();

        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(karte.KarteId));
        Assert.Multiple(() =>
        {
            Assert.That(detail!.Kommentare[0].Text, Is.EqualTo("Die Lizenz gilt nur pro Rechner."));
            Assert.That(detail.Kommentare[0].Urheber.Name, Is.EqualTo("Maria Lenz"));
            Assert.That(detail.Kommentare[0].Urheber.StillgelegtAm, Is.Not.Null);
        });
    }

    // US-7 als Gegenprobe: die Kommentare haengen am Kartendetail und nicht an der Karte — die
    // Boardantwort bleibt unveraendert.
    [Test]
    public async Task Wenn_eine_Karte_Kommentare_traegt_dann_bekommt_die_Boardantwort_keine_Kommentarliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await SchreibeKommentar(webApi, aufbau.KarteId, "Die Lizenz gilt nur pro Rechner.", aufbau.Urheber.KontributorId);

        var rumpf = await webApi.Klient.GetStringAsync($"{BoardsRoute}/{aufbau.BoardId}");

        Assert.Multiple(() =>
        {
            Assert.That(rumpf, Does.Not.Contain("kommentare"));
            Assert.That(rumpf, Does.Not.Contain("Die Lizenz gilt nur pro Rechner."));
        });
    }

    // Die Anzahl wird gerechnet und nicht mitgesendet, und eine Position gibt es nicht: kein Feld
    // der Antwort traegt sie.
    [Test]
    public async Task Wenn_das_Kartendetail_gelesen_wird_dann_traegt_es_weder_eine_Kommentarzahl_noch_eine_Position()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await SchreibeZwei(webApi, aufbau, "A", "B");

        var rumpf = await webApi.Klient.GetStringAsync(Kartendetailroute(aufbau.KarteId));

        Assert.Multiple(() =>
        {
            Assert.That(rumpf, Does.Contain("\"kommentare\""));
            Assert.That(rumpf, Does.Not.Contain("kommentarzahl"));
            Assert.That(rumpf, Does.Not.Contain("kommentaranzahl"));
        });

        // Die Karte selbst traegt weiterhin ihre Position auf der Bahn; geprueft wird deshalb der
        // Ausschnitt der Kommentarliste und nicht der ganze Rumpf.
        var kommentarliste = rumpf[rumpf.IndexOf("\"kommentare\"", StringComparison.Ordinal)..];
        Assert.That(kommentarliste, Does.Not.Contain("position"));
    }

    [Test]
    public async Task Wenn_der_Kommentartext_leer_ist_dann_antwortet_POST_kommentare_mit_400_und_Befund_und_speichert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await SchreibeKommentar(webApi, aufbau.KarteId, "Bitte prüfen", aufbau.Urheber.KontributorId);

        using var antwort = await webApi.Klient.PostAsJsonAsync(Kommentarroute(aufbau.KarteId), new KommentarSchreibenAnfrage("   ", aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kommentar-leer");
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));
        Assert.That(detail!.Kommentare.Select(kommentar => kommentar.Text), Is.EqualTo(new[] { "Bitte prüfen" }));
    }

    [Test]
    public async Task Wenn_der_Kommentartext_zu_lang_ist_dann_antwortet_POST_kommentare_mit_400_und_Befund_und_speichert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsJsonAsync(
            Kommentarroute(aufbau.KarteId),
            new KommentarSchreibenAnfrage(new string('a', HoechsteKommentarlaenge + 1), aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kommentar-zu-lang");
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));
        Assert.That(detail!.Kommentare, Is.Empty);
    }

    [Test]
    public async Task Wenn_die_KarteId_unbekannt_ist_dann_antwortet_POST_kommentare_mit_404_und_einem_Befund_ohne_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsJsonAsync(Kommentarroute(9999), new KommentarSchreibenAnfrage("Bitte prüfen", aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Kommentar schreiben mit unbekannter KarteId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("9999"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("Board"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Is.Not.Empty);
        });
    }

    [Test]
    public async Task Wenn_die_KontributorId_unbekannt_ist_dann_antwortet_POST_kommentare_mit_404_und_nennt_die_Kontributorenliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsJsonAsync(Kommentarroute(aufbau.KarteId), new KommentarSchreibenAnfrage("Bitte prüfen", 9999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Kommentar schreiben mit unbekannter KontributorId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("9999"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("GET /api/kontributoren"));
        });
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));
        Assert.That(detail!.Kommentare, Is.Empty);
    }

    // 400 und nicht 404: es fehlt kein Ding, es wurde eine Regel verletzt. Und die Meldung sagt
    // **nicht** „kann nicht verantwortlich sein" — dieser Wortlaut gehoert der Karte.
    [Test]
    public async Task Wenn_die_KontributorId_stillgelegt_ist_dann_antwortet_POST_kommentare_mit_400_und_einer_Meldung_zum_Kommentar()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var maria = await LegeKontributorAn(webApi, "Maria Lenz", Kontributorart.Mensch);
        using var stillgelegt = await webApi.Klient.PutAsJsonAsync($"/api/kontributoren/{maria.KontributorId}/stilllegung", new Stilllegung(true));
        stillgelegt.EnsureSuccessStatusCode();

        using var antwort = await webApi.Klient.PostAsJsonAsync(Kommentarroute(aufbau.KarteId), new KommentarSchreibenAnfrage("Bitte prüfen", maria.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Kommentar schreiben mit stillgelegter KontributorId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-stillgelegt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("Kommentar"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("verantwortlich"));
        });
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));
        Assert.That(detail!.Kommentare, Is.Empty);
    }

    // US-7: die Antwort ist 200 mit dem **ganzen** Kartendetail, nicht 201 mit der geschriebenen
    // Zeile — dieselbe Antwortgestalt, die diese Seite ueberall hat.
    [Test]
    public async Task Wenn_ein_Dateiverweis_eingetragen_wird_dann_antwortet_POST_dateiverweise_mit_200_und_dem_ganzen_Kartendetail()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsJsonAsync(
            Dateiverweisroute(aufbau.KarteId),
            new DateiverweisEintragenAnfrage("Dokumentation/Planung/kanbanc.md", aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await AlsKartendetail(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(detail.Karte.Titel, Is.EqualTo("Playwright-Lizenz klären"));
            Assert.That(detail.Boardname, Is.EqualTo("Entwicklung"));
            Assert.That(detail.Dateiverweise[^1].Pfad, Is.EqualTo("Dokumentation/Planung/kanbanc.md"));
            Assert.That(detail.Dateiverweise[^1].DateiverweisId, Is.GreaterThan(0));
        });
    }

    // Der Eintrag traegt den **ganzen** Urheber und einen Zeitpunkt im Fenster des Aufrufs — das
    // Rechenbeispiel der Anforderung.
    [Test]
    public async Task Wenn_ein_Dateiverweis_eingetragen_wird_dann_traegt_er_den_ganzen_Urheber_und_einen_Zeitpunkt_im_Fenster_des_Aufrufs()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        var vorher = DateTimeOffset.UtcNow.AddSeconds(-1);
        var detail = await TrageDateiverweisEin(webApi, aufbau.KarteId, "Dokumentation/Planung/kanbanc.md", aufbau.Urheber.KontributorId);
        var nachher = DateTimeOffset.UtcNow.AddSeconds(1);

        Assert.Multiple(() =>
        {
            Assert.That(detail.Dateiverweise[0].Urheber, Is.EqualTo(aufbau.Urheber));
            Assert.That(detail.Dateiverweise[0].Zeitpunkt, Is.GreaterThanOrEqualTo(vorher));
            Assert.That(detail.Dateiverweise[0].Zeitpunkt, Is.LessThanOrEqualTo(nachher));
        });
    }

    // Der Aufrufer kann den Zeitpunkt nicht mitgeben: die Anfrage hat kein Feld dafuer, und ein
    // mitgeschicktes aendert nichts am gespeicherten Wert.
    [Test]
    public async Task Wenn_der_Aufrufer_einen_Zeitpunkt_mitschickt_dann_aendert_das_nichts_am_gespeicherten_Wert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var vorher = DateTimeOffset.UtcNow.AddSeconds(-1);

        using var antwort = await webApi.Klient.PostAsJsonAsync(
            Dateiverweisroute(aufbau.KarteId),
            new { pfad = "Dokumentation/Planung/kanbanc.md", kontributor = aufbau.Urheber.KontributorId, zeitpunkt = "1999-01-01T00:00:00+00:00" });

        antwort.EnsureSuccessStatusCode();
        var detail = await AlsKartendetail(antwort);
        Assert.That(detail.Dateiverweise[0].Zeitpunkt, Is.GreaterThanOrEqualTo(vorher));
    }

    // Randgetrimmt, sonst zeichengleich — das Rechenbeispiel der Anforderung, samt Windows-Pfad.
    [Test]
    public async Task Wenn_ein_Pfad_mit_Raendern_oder_Rueckstrichen_eingetragen_wird_dann_kommt_er_getrimmt_und_zeichengleich_zurueck()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        await TrageDateiverweisEin(webApi, aufbau.KarteId, "  Dokumentation/Planung/kanbanc.md  ", aufbau.Urheber.KontributorId);
        var detail = await TrageDateiverweisEin(webApi, aufbau.KarteId, @"Dokumentation\Planung\kanbanc.md", aufbau.Urheber.KontributorId);

        Assert.That(detail.Dateiverweise.Select(dateiverweis => dateiverweis.Pfad),
            Is.EqualTo(new[] { "Dokumentation/Planung/kanbanc.md", @"Dokumentation\Planung\kanbanc.md" }));
    }

    // Was ausdruecklich **angenommen** wird: eine nicht existierende Datei, ein absoluter Pfad
    // ausserhalb jedes Repositorys, ein Pfad ohne Endung.
    [Test]
    public async Task Wenn_der_Pfad_ins_Leere_zeigt_oder_absolut_ist_dann_nimmt_die_API_ihn_trotzdem_an()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        await TrageDateiverweisEin(webApi, aufbau.KarteId, "Dokumentation/gibt-es-nicht.md", aufbau.Urheber.KontributorId);
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "/home/shoff/notizen.txt", aufbau.Urheber.KontributorId);
        var detail = await TrageDateiverweisEin(webApi, aufbau.KarteId, "Dokumentation/Planung", aufbau.Urheber.KontributorId);

        Assert.That(detail.Dateiverweise.Select(dateiverweis => dateiverweis.Pfad),
            Is.EqualTo(new[] { "Dokumentation/gibt-es-nicht.md", "/home/shoff/notizen.txt", "Dokumentation/Planung" }));
    }

    // Rechenbeispiel Reihenfolge: a.md, b.md, c.md — aeltester oben, in jedem folgenden Abruf.
    [Test]
    public async Task Wenn_drei_Dateiverweise_eingetragen_werden_dann_liefert_GET_karten_dieselbe_Liste_in_derselben_Reihenfolge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "a.md", aufbau.Urheber.KontributorId);
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "b.md", aufbau.Urheber.KontributorId);
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "c.md", aufbau.Urheber.KontributorId);

        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));

        Assert.That(detail!.Dateiverweise.Select(dateiverweis => dateiverweis.Pfad), Is.EqualTo(new[] { "a.md", "b.md", "c.md" }));
    }

    // US-7 als Gegenprobe: die Dateiverweise haengen am Kartendetail und nicht an der Karte — die
    // Boardantwort bleibt unveraendert.
    [Test]
    public async Task Wenn_eine_Karte_Dateiverweise_traegt_dann_bekommt_die_Boardantwort_keine_Dateiverweisliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "Dokumentation/Planung/kanbanc.md", aufbau.Urheber.KontributorId);

        var rumpf = await webApi.Klient.GetStringAsync($"{BoardsRoute}/{aufbau.BoardId}");

        Assert.Multiple(() =>
        {
            Assert.That(rumpf, Does.Not.Contain("dateiverweise"));
            Assert.That(rumpf, Does.Not.Contain("Dokumentation/Planung/kanbanc.md"));
        });
    }

    // Kein Zaehlfeld und keine Position neben der Liste.
    [Test]
    public async Task Wenn_das_Kartendetail_gelesen_wird_dann_traegt_es_weder_eine_Dateiverweiszahl_noch_eine_Position()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "a.md", aufbau.Urheber.KontributorId);
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "b.md", aufbau.Urheber.KontributorId);

        var rumpf = await webApi.Klient.GetStringAsync(Kartendetailroute(aufbau.KarteId));

        Assert.Multiple(() =>
        {
            Assert.That(rumpf, Does.Contain("\"dateiverweise\""));
            Assert.That(rumpf, Does.Not.Contain("dateiverweiszahl"));
            Assert.That(rumpf, Does.Not.Contain("dateiverweisanzahl"));
        });

        var dateiverweisliste = rumpf[rumpf.IndexOf("\"dateiverweise\"", StringComparison.Ordinal)..];
        Assert.That(dateiverweisliste, Does.Not.Contain("position"));
    }

    [Test]
    public async Task Wenn_der_Pfad_leer_ist_dann_antwortet_POST_dateiverweise_mit_400_und_Befund_und_speichert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "Dokumentation/Planung/kanbanc.md", aufbau.Urheber.KontributorId);

        using var antwort = await webApi.Klient.PostAsJsonAsync(Dateiverweisroute(aufbau.KarteId), new DateiverweisEintragenAnfrage("   ", aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "dateiverweis-pfad-leer");
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));
        Assert.That(detail!.Dateiverweise.Select(dateiverweis => dateiverweis.Pfad), Is.EqualTo(new[] { "Dokumentation/Planung/kanbanc.md" }));
    }

    [Test]
    public async Task Wenn_der_Pfad_zu_lang_ist_dann_antwortet_POST_dateiverweise_mit_400_und_nennt_die_Hoechstlaenge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsJsonAsync(
            Dateiverweisroute(aufbau.KarteId),
            new DateiverweisEintragenAnfrage(new string('a', HoechstePfadlaenge + 1), aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Dateiverweis eintragen mit zu langem Pfad");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("dateiverweis-pfad-zu-lang"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("500"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain($"POST /api/karten/{aufbau.KarteId}/dateiverweise"));
        });
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));
        Assert.That(detail!.Dateiverweise, Is.Empty);
    }

    // Ein Pfad aus genau 500 Zeichen geht durch — die andere Haelfte des Rechenbeispiels.
    [Test]
    public async Task Wenn_der_Pfad_genau_die_Hoechstlaenge_hat_dann_nimmt_die_API_ihn_an()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        var detail = await TrageDateiverweisEin(webApi, aufbau.KarteId, new string('a', HoechstePfadlaenge), aufbau.Urheber.KontributorId);

        Assert.That(detail.Dateiverweise[0].Pfad, Has.Length.EqualTo(HoechstePfadlaenge));
    }

    // Der Fall, den es bei Anhang, Teilaufgabe und Kommentar nicht gibt.
    [Test]
    public async Task Wenn_derselbe_Pfad_ein_zweites_Mal_eingetragen_wird_dann_antwortet_POST_dateiverweise_mit_400_und_lesbarem_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "Dokumentation/Planung/kanbanc.md", aufbau.Urheber.KontributorId);

        using var antwort = await webApi.Klient.PostAsJsonAsync(
            Dateiverweisroute(aufbau.KarteId),
            new DateiverweisEintragenAnfrage("  Dokumentation/Planung/kanbanc.md  ", aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Dateiverweis eintragen mit schon vorhandenem Pfad");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("dateiverweis-doppelt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("Dokumentation/Planung/kanbanc.md"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain(aufbau.KarteId.ToString(CultureInfo.InvariantCulture)));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("UNIQUE"), "Der Aufrufer trifft nie auf eine nackte Datenbankmeldung.");
        });
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));
        Assert.That(detail!.Dateiverweise, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Wenn_derselbe_Pfad_an_eine_zweite_Karte_geht_dann_nimmt_die_API_ihn_an()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var zweite = await LegeKarteAn(webApi, aufbau.BoardId, (await LadeBoard(webApi, aufbau.BoardId)).Spalten[0].SpalteId, "Migration schreiben");
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "Dokumentation/Planung/kanbanc.md", aufbau.Urheber.KontributorId);

        var detail = await TrageDateiverweisEin(webApi, zweite.KarteId, "Dokumentation/Planung/kanbanc.md", aufbau.Urheber.KontributorId);

        Assert.That(detail.Dateiverweise.Select(dateiverweis => dateiverweis.Pfad), Is.EqualTo(new[] { "Dokumentation/Planung/kanbanc.md" }));
    }

    [Test]
    public async Task Wenn_die_KarteId_unbekannt_ist_dann_antwortet_POST_dateiverweise_mit_404_und_einem_Befund_ohne_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsJsonAsync(Dateiverweisroute(9999), new DateiverweisEintragenAnfrage("kanbanc.md", aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Dateiverweis eintragen mit unbekannter KarteId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("9999"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("Board"));
        });
    }

    [Test]
    public async Task Wenn_die_KontributorId_unbekannt_ist_dann_antwortet_POST_dateiverweise_mit_404_und_nennt_die_Kontributorenliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsJsonAsync(Dateiverweisroute(aufbau.KarteId), new DateiverweisEintragenAnfrage("kanbanc.md", 9999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Dateiverweis eintragen mit unbekannter KontributorId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("GET /api/kontributoren"));
        });
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));
        Assert.That(detail!.Dateiverweise, Is.Empty);
    }

    // 400 und nicht 404, und die Meldung spricht vom **Dateiverweis** — weder von Verantwortung
    // noch vom Kommentar noch vom Anhang.
    [Test]
    public async Task Wenn_die_KontributorId_stillgelegt_ist_dann_antwortet_POST_dateiverweise_mit_400_und_einer_Meldung_zum_Dateiverweis()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var maria = await LegeKontributorAn(webApi, "Maria Lenz", Kontributorart.Mensch);
        using var stillgelegt = await webApi.Klient.PutAsJsonAsync($"/api/kontributoren/{maria.KontributorId}/stilllegung", new Stilllegung(true));
        stillgelegt.EnsureSuccessStatusCode();

        using var antwort = await webApi.Klient.PostAsJsonAsync(Dateiverweisroute(aufbau.KarteId), new DateiverweisEintragenAnfrage("kanbanc.md", maria.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Dateiverweis eintragen mit stillgelegter KontributorId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-stillgelegt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("Dateiverweis"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("verantwortlich"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("anhängen"));
        });
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));
        Assert.That(detail!.Dateiverweise, Is.Empty);
    }

    // Ein Dateiverweis eines inzwischen stillgelegten Kontributors bleibt an der Karte sichtbar,
    // mit Name und Stilllegungsstand.
    [Test]
    public async Task Wenn_der_Urheber_nach_dem_Eintragen_stillgelegt_wird_dann_bleibt_sein_Dateiverweis_sichtbar()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var maria = await LegeKontributorAn(webApi, "Maria Lenz", Kontributorart.Mensch);
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "Dokumentation/Planung/kanbanc.md", maria.KontributorId);
        using var stillgelegt = await webApi.Klient.PutAsJsonAsync($"/api/kontributoren/{maria.KontributorId}/stilllegung", new Stilllegung(true));
        stillgelegt.EnsureSuccessStatusCode();

        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));

        Assert.Multiple(() =>
        {
            Assert.That(detail!.Dateiverweise[0].Urheber.Name, Is.EqualTo("Maria Lenz"));
            Assert.That(detail.Dateiverweise[0].Urheber.StillgelegtAm, Is.Not.Null);
        });
    }

    [Test]
    public async Task Wenn_ein_Dateiverweis_entfernt_wird_dann_antwortet_DELETE_mit_200_und_dem_Kartendetail_ohne_diesen_Eintrag()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await TrageDateiverweisEin(webApi, aufbau.KarteId, "a.md", aufbau.Urheber.KontributorId);
        var mitZweien = await TrageDateiverweisEin(webApi, aufbau.KarteId, "b.md", aufbau.Urheber.KontributorId);
        var ersteId = mitZweien.Dateiverweise[0].DateiverweisId;

        using var antwort = await webApi.Klient.DeleteAsync(Dateiverweiszeilenroute(aufbau.KarteId, ersteId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await AlsKartendetail(antwort);
        Assert.That(detail.Dateiverweise.Select(dateiverweis => dateiverweis.Pfad), Is.EqualTo(new[] { "b.md" }));
    }

    [Test]
    public async Task Wenn_derselbe_Dateiverweis_ein_zweites_Mal_entfernt_wird_dann_antwortet_DELETE_mit_404_und_Rumpf()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var detail = await TrageDateiverweisEin(webApi, aufbau.KarteId, "kanbanc.md", aufbau.Urheber.KontributorId);
        var dateiverweisId = detail.Dateiverweise[0].DateiverweisId;
        using var erste = await webApi.Klient.DeleteAsync(Dateiverweiszeilenroute(aufbau.KarteId, dateiverweisId));
        erste.EnsureSuccessStatusCode();

        using var antwort = await webApi.Klient.DeleteAsync(Dateiverweiszeilenroute(aufbau.KarteId, dateiverweisId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "dateiverweis-unbekannt");
    }

    // Eine DateiverweisId, die es gibt, aber nicht an **dieser** Karte: 404, und die andere Karte
    // traegt ihre Zeile danach unveraendert.
    [Test]
    public async Task Wenn_der_Dateiverweis_zu_einer_anderen_Karte_gehoert_dann_antwortet_DELETE_mit_404_und_entfernt_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var fremde = await LegeKarteAn(webApi, aufbau.BoardId, (await LadeBoard(webApi, aufbau.BoardId)).Spalten[0].SpalteId, "Migration schreiben");
        var beiDerFremden = await TrageDateiverweisEin(webApi, fremde.KarteId, "nur-woanders.md", aufbau.Urheber.KontributorId);
        var fremdeId = beiDerFremden.Dateiverweise[0].DateiverweisId;

        using var antwort = await webApi.Klient.DeleteAsync(Dateiverweiszeilenroute(aufbau.KarteId, fremdeId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Fremden Dateiverweis entfernen");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("dateiverweis-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain(fremdeId.ToString(CultureInfo.InvariantCulture)));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain(aufbau.KarteId.ToString(CultureInfo.InvariantCulture)));
        });
        var fremdesDetail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(fremde.KarteId));
        Assert.That(fremdesDetail!.Dateiverweise, Has.Count.EqualTo(1));
    }

    // Nach dem Entfernen laesst sich derselbe Pfad wieder eintragen — er ist keine Dublette mehr.
    [Test]
    public async Task Wenn_ein_Pfad_entfernt_und_erneut_eingetragen_wird_dann_nimmt_die_API_ihn_wieder_an()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var eingetragen = await TrageDateiverweisEin(webApi, aufbau.KarteId, "kanbanc.md", aufbau.Urheber.KontributorId);
        using var entfernt = await webApi.Klient.DeleteAsync(Dateiverweiszeilenroute(aufbau.KarteId, eingetragen.Dateiverweise[0].DateiverweisId));
        entfernt.EnsureSuccessStatusCode();

        var detail = await TrageDateiverweisEin(webApi, aufbau.KarteId, "kanbanc.md", aufbau.Urheber.KontributorId);

        Assert.That(detail.Dateiverweise.Select(dateiverweis => dateiverweis.Pfad), Is.EqualTo(new[] { "kanbanc.md" }));
    }

    // **Es gibt keine Route zum Aendern eines Dateiverweises.** Geprueft am Bestand der
    // registrierten Routen und nicht an einem einzelnen Aufruf: ein 404 sagte nur, dass diese
    // eine Adresse nichts kann.
    [Test]
    public void Wenn_die_registrierten_Routen_durchgesehen_werden_dann_gibt_es_keine_zum_Aendern_eines_Dateiverweises()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var dateiverweisrouten = webApi.Routen.Where(route => route.Contains("dateiverweise", StringComparison.Ordinal));

        Assert.That(dateiverweisrouten, Is.EquivalentTo(new[]
        {
            "POST /api/karten/{karteId:long}/dateiverweise",
            "DELETE /api/karten/{karteId:long}/dateiverweise/{dateiverweisId:long}",
        }));
    }

    private static string Dateiverweisroute(long karteId)
    {
        return $"/api/karten/{karteId}/dateiverweise";
    }

    private static string Dateiverweiszeilenroute(long karteId, long dateiverweisId)
    {
        return $"/api/karten/{karteId}/dateiverweise/{dateiverweisId}";
    }

    private static async Task<Kartendetail> TrageDateiverweisEin(TestWebApi webApi, long karteId, string pfad, long kontributorId)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(Dateiverweisroute(karteId), new DateiverweisEintragenAnfrage(pfad, kontributorId));
        antwort.EnsureSuccessStatusCode();
        return await AlsKartendetail(antwort);
    }

    private static string Kommentarroute(long karteId)
    {
        return $"/api/karten/{karteId}/kommentare";
    }

    private static async Task<Kartendetail> SchreibeKommentar(TestWebApi webApi, long karteId, string text, long kontributorId)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(Kommentarroute(karteId), new KommentarSchreibenAnfrage(text, kontributorId));
        antwort.EnsureSuccessStatusCode();
        return await AlsKartendetail(antwort);
    }

    private static async Task<Kartendetail> SchreibeZwei(TestWebApi webApi, Kommentaraufbau aufbau, string erster, string zweiter)
    {
        await SchreibeKommentar(webApi, aufbau.KarteId, erster, aufbau.Urheber.KontributorId);
        return await SchreibeKommentar(webApi, aufbau.KarteId, zweiter, aufbau.Urheber.KontributorId);
    }

    // Am Dienst vorbei: zwei verschiedene Zeitpunkte lassen sich ueber die Uhr des Testlaufs nicht
    // herstellen, und ein Testhaken in der WebApi waere Produktionscode, den nur der Test braucht.
    private static void DatiereZurueck(TemporaereDatenbank datenbank, long kommentarId, DateTimeOffset zeitpunkt)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            UPDATE Kommentar
               SET Zeitpunkt = @Zeitpunkt
             WHERE KommentarId = @KommentarId",
            new { KommentarId = kommentarId, Zeitpunkt = zeitpunkt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) });
    }

    private static async Task<Kommentaraufbau> KarteMitUrheber(TestWebApi webApi)
    {
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        var urheber = await LegeKontributorAn(webApi, "Stefan", Kontributorart.Mensch);
        return new Kommentaraufbau(board.BoardId, karte.KarteId, urheber);
    }

    private sealed record Kommentaraufbau(long BoardId, long KarteId, Kontributor Urheber);

    private static async Task ErwarteKeineAbgehakteTeilaufgabe(TestWebApi webApi, long karteId)
    {
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(karteId));
        Assert.That(detail!.Teilaufgaben.Any(teilaufgabe => teilaufgabe.Abgehakt), Is.False);
    }

    private static string Teilaufgabenroute(long karteId)
    {
        return $"/api/karten/{karteId}/teilaufgaben";
    }

    private static string Teilaufgabenstandsroute(long karteId, long teilaufgabeId)
    {
        return $"/api/karten/{karteId}/teilaufgaben/{teilaufgabeId}";
    }

    private static async Task<Kartendetail> LegeTeilaufgabeAn(TestWebApi webApi, long karteId, string text)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(Teilaufgabenroute(karteId), new TeilaufgabeAnlegenAnfrage(text));
        antwort.EnsureSuccessStatusCode();
        return await AlsKartendetail(antwort);
    }

    private static async Task<Kartendetail> SetzeAbhakung(TestWebApi webApi, long karteId, long teilaufgabeId, bool abgehakt)
    {
        using var antwort = await webApi.Klient.PutAsJsonAsync(Teilaufgabenstandsroute(karteId, teilaufgabeId), new Teilaufgabenstand(abgehakt));
        antwort.EnsureSuccessStatusCode();
        return await AlsKartendetail(antwort);
    }

    private static async Task<Kartendetail> AlsKartendetail(HttpResponseMessage antwort)
    {
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        if (detail is null)
        {
            throw new InvalidOperationException("Die API hat kein Kartendetail zurückgegeben.");
        }

        return detail;
    }

    private static string Etikettenroute(long karteId)
    {
        return $"/api/karten/{karteId}/etiketten";
    }

    private static async Task<Kartendetail> SetzeEtiketten(TestWebApi webApi, long karteId, IReadOnlyList<string> etiketten)
    {
        using var antwort = await webApi.Klient.PutAsJsonAsync(Etikettenroute(karteId), new Kartenetiketten(etiketten));
        antwort.EnsureSuccessStatusCode();
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        if (detail is null)
        {
            throw new InvalidOperationException("Die API hat kein Kartendetail zurückgegeben.");
        }

        return detail;
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name, Kontributorart art)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync("/api/kontributoren", new KontributorAnlegenAnfrage(name, art));
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        if (kontributor is null)
        {
            throw new InvalidOperationException("Die API hat keinen Kontributor zurückgegeben.");
        }

        return kontributor;
    }

    private static async Task<Kartendetail> Aendere(TestWebApi webApi, long karteId, KarteAendernAnfrage anfrage)
    {
        using var antwort = await webApi.Klient.PutAsJsonAsync(Kartendetailroute(karteId), anfrage);
        antwort.EnsureSuccessStatusCode();
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        if (detail is null)
        {
            throw new InvalidOperationException("Die API hat kein Kartendetail zurückgegeben.");
        }

        return detail;
    }

    private static string Kartendetailroute(long karteId)
    {
        return $"/api/karten/{karteId}";
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
