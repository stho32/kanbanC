using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Dapper;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Der Weg des Agenten zum Burndown: die Reihe kommt **gerechnet** zurück — je Kalendertag der
// Stand offener Karten und die an dem Tag erledigten Karten, dazu die Kopfzahlen. Der Aufrufer
// muss nichts nachrechnen.
// Das Erledigungsdatum entsteht beim Zug in die Abschlussspalte und trägt deshalb immer das
// heutige Datum; für eine Achse über mehrere Tage datiert der Test es in der Ablage zurück — das
// ist derselbe Zustand, den ein über Tage gefülltes Board hätte.
public class BurndownEndpunktTests
{
    private const string BoardsRoute = "/api/boards";
    private const string Isodatumsformat = "yyyy-MM-dd";

    [Test]
    public async Task Wenn_der_Bestand_abgerufen_wird_dann_traegt_die_Reihe_jeden_Kalendertag_von_der_ersten_Erledigung_bis_heute()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitFuenfKarten(webApi, datenbank);

        var auswertung = await LiesBurndown(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId, seit: null);

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Tage, Has.Count.EqualTo(5));
            Assert.That(auswertung.Tage[0].Tag, Is.EqualTo(Heute().AddDays(-4)));
            Assert.That(auswertung.Tage[^1].Tag, Is.EqualTo(Heute()));
        });
    }

    // Das Rechenbeispiel der Anforderung über die echte Ablage: fünf Karten, Reihe 4, 4, 2, 2, 2.
    [Test]
    public async Task Wenn_der_Bestand_des_Rechenbeispiels_abgerufen_wird_dann_lautet_die_Reihe_4_4_2_2_2()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitFuenfKarten(webApi, datenbank);

        var auswertung = await LiesBurndown(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId, seit: null);

        Assert.That(auswertung.Tage.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 4, 4, 2, 2, 2 }));
    }

    [Test]
    public async Task Wenn_der_Bestand_des_Rechenbeispiels_abgerufen_wird_dann_lauten_die_Kopfzahlen_zwei_drei_fuenf_und_eins_ohne_Datum()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitFuenfKarten(webApi, datenbank);

        var auswertung = await LiesBurndown(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId, seit: null);

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Kopfzahlen.Offen, Is.EqualTo(2));
            Assert.That(auswertung.Kopfzahlen.Erledigt, Is.EqualTo(3));
            Assert.That(auswertung.Kopfzahlen.ImBestand, Is.EqualTo(5));
            Assert.That(auswertung.Kopfzahlen.OhneErledigungsdatum, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Wenn_die_Tageszeilen_gelesen_werden_dann_tragen_die_erledigten_Karten_Nummer_und_Titel()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitFuenfKarten(webApi, datenbank);

        var auswertung = await LiesBurndown(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId, seit: null);

        var ersteErledigte = auswertung.Tage[0].ErledigteKarten.Single();
        Assert.Multiple(() =>
        {
            Assert.That(ersteErledigte.Kartennummer, Is.EqualTo("WBS-01"));
            Assert.That(ersteErledigte.Titel, Is.EqualTo("Board anlegen"));
            Assert.That(ersteErledigte.KarteId, Is.GreaterThan(0));
            Assert.That(auswertung.Tage[1].ErledigteKarten, Is.Empty);
            Assert.That(auswertung.Tage[2].ErledigteKarten.Select(karte => karte.Kartennummer), Is.EqualTo(new[] { "WBS-02", "WBS-03" }));
        });
    }

    // Der Zeitraum schneidet die Achse, nicht den Bestand: die Kopfzahlen bleiben.
    [Test]
    public async Task Wenn_ein_Zeitraum_gewaehlt_ist_dann_ist_die_Reihe_kuerzer_und_die_Kopfzahlen_bleiben()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitFuenfKarten(webApi, datenbank);

        var auswertung = await LiesBurndown(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId, Heute().AddDays(-2));

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Tage.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 2, 2, 2 }));
            Assert.That(auswertung.Kopfzahlen.ImBestand, Is.EqualTo(5));
            Assert.That(auswertung.Kopfzahlen.Offen, Is.EqualTo(2));
        });
    }

    // Ein Beginn vor dem frühesten Abschluss gibt der Kurve ihr flaches Stück davor.
    [Test]
    public async Task Wenn_der_gewaehlte_Beginn_vor_dem_fruehesten_Abschluss_liegt_dann_traegt_die_Reihe_ein_flaches_Stueck_davor()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitFuenfKarten(webApi, datenbank);

        var auswertung = await LiesBurndown(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId, Heute().AddDays(-6));

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Tage, Has.Count.EqualTo(7));
            Assert.That(auswertung.Tage.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 5, 5, 4, 4, 2, 2, 2 }));
            Assert.That(auswertung.Kopfzahlen.Offen, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Wenn_der_gewaehlte_Beginn_nach_heute_liegt_dann_steht_genau_der_eine_Tag_heute_da()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitFuenfKarten(webApi, datenbank);

        var auswertung = await LiesBurndown(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId, Heute().AddDays(13));

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Tage, Has.Count.EqualTo(1));
            Assert.That(auswertung.Tage[0].Tag, Is.EqualTo(Heute()));
        });
    }

    // Die Kurve ist eine **Momentaufnahme**: wer eine Karte aus der Abschlussspalte zieht, verliert
    // ihr Datum — sie ist danach rückwirkend an allen Tagen offen. Belegt statt behauptet.
    [Test]
    public async Task Wenn_eine_Karte_die_Abschlussspalte_verlaesst_dann_ist_sie_rueckwirkend_an_allen_Tagen_offen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitFuenfKarten(webApi, datenbank);
        var vorher = await LiesBurndown(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId, seit: null);
        var ersteKarte = await KarteIdVon(webApi, aufbau, "WBS-01");

        using var gezogen = await webApi.Klient.PutAsJsonAsync(
            $"{BoardsRoute}/{aufbau.Board.BoardId}/karten/{ersteKarte}/lage",
            new Kartenlage(aufbau.Board.Spalten[0].SpalteId, 1));
        gezogen.EnsureSuccessStatusCode();
        var nachher = await LiesBurndown(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId, seit: null);

        Assert.Multiple(() =>
        {
            Assert.That(vorher.Tage.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 4, 4, 2, 2, 2 }));
            Assert.That(nachher.Tage.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 3, 3, 3 }));
            Assert.That(nachher.Kopfzahlen.Offen, Is.EqualTo(3));
            Assert.That(nachher.Kopfzahlen.Erledigt, Is.EqualTo(2));
        });
    }

    // Ein Bestand ohne Karten ist kein Fehler: die Reihe über den einen Tag heute ist die Antwort.
    [Test]
    public async Task Wenn_die_Kartenklasse_keine_Karte_fuehrt_dann_kommt_200_mit_dem_einen_Tag_heute()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        var auswertung = await LiesBurndown(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId, seit: null);

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Tage, Has.Count.EqualTo(1));
            Assert.That(auswertung.Tage[0].Tag, Is.EqualTo(Heute()));
            Assert.That(auswertung.Tage[0].OffeneKarten, Is.Zero);
            Assert.That(auswertung.Kopfzahlen, Is.EqualTo(new Burndownkopfzahlen(0, 0, 0, 0)));
        });
    }

    [Test]
    public async Task Wenn_keine_Karte_des_Bestands_ein_Erledigungsdatum_traegt_dann_kommt_200_mit_dem_einen_Tag_heute_und_allen_Karten_offen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        await LegeKlassenkarteAn(webApi, aufbau, "Board anlegen", aufbau.Board.Spalten[0].SpalteId);
        await LegeKlassenkarteAn(webApi, aufbau, "Boards auflisten", aufbau.Board.Spalten[0].SpalteId);

        var auswertung = await LiesBurndown(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId, seit: null);

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Tage, Has.Count.EqualTo(1));
            Assert.That(auswertung.Tage[0].OffeneKarten, Is.EqualTo(2));
            Assert.That(auswertung.Kopfzahlen.Offen, Is.EqualTo(2));
            Assert.That(auswertung.Kopfzahlen.Erledigt, Is.Zero);
            Assert.That(auswertung.Kopfzahlen.OhneErledigungsdatum, Is.Zero);
        });
    }

    // Ein unlesbares „seit“ wird zurückgewiesen statt still ignoriert — mit **unserem** Befund:
    // der Rumpf trägt „befunde“, und genau das unterscheidet ihn von der Antwort, die ASP.NET bei
    // einer DateOnly-Bindung selbst gäbe.
    [Test]
    public async Task Wenn_seit_unlesbar_ist_dann_kommt_400_mit_dem_gelesenen_Wert_und_der_erwarteten_Form()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.GetAsync($"{BurndownRoute(aufbau.Board.BoardId, aufbau.KartenklasseId)}?seit=gestern");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var befund = (await Fehlerrumpf.Lies(antwort, "Burndown mit unlesbarem seit")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeitraum-filter-unlesbar"));
            Assert.That(befund.Meldung, Does.Contain("gestern"));
            Assert.That(befund.Meldung, Does.Contain(Isodatumsformat));
            Assert.That(befund.Kompensation, Does.Contain(BurndownRoute(aufbau.Board.BoardId, aufbau.KartenklasseId)));
        });
    }

    [Test]
    public async Task Wenn_seit_fehlt_dann_ist_das_kein_Fehler_sondern_die_Standardachse()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.GetAsync(BurndownRoute(aufbau.Board.BoardId, aufbau.KartenklasseId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Wenn_es_das_Board_nicht_gibt_dann_kommt_404_mit_Grund_und_Kompensationsaktion()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.GetAsync(BurndownRoute(999, aufbau.KartenklasseId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Burndown mit unbekannter BoardId")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("board-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    [Test]
    public async Task Wenn_es_die_Kartenklasse_nicht_gibt_dann_kommt_404_mit_ihrem_eigenen_Code()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.GetAsync(BurndownRoute(aufbau.Board.BoardId, 999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kartenklasse-unbekannt");
    }

    [Test]
    public async Task Wenn_die_Kartenklasse_einem_fremden_Board_gehoert_dann_kommt_404_mit_dem_eigenen_Code()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        var nachbar = await LegeBoardAn(webApi, "Beschaffung");
        var fremdeKartenklasse = await LegeKartenklasseAn(webApi, nachbar.BoardId);

        using var antwort = await webApi.Klient.GetAsync(BurndownRoute(aufbau.Board.BoardId, fremdeKartenklasse.KartenklasseId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Burndown mit fremder KartenklasseId")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kartenklasse-fremd"));
            Assert.That(befund.Meldung, Does.Contain(nachbar.BoardId.ToString(CultureInfo.InvariantCulture)));
        });
    }

    // Die Antwort **ist** die Auswertung: sie trägt weder Zeiteinträge noch Kartendetails — ein
    // Agent bekommt die Reihe, nicht ihre Summanden. Die Rohdaten sind I0037.
    [Test]
    public async Task Wenn_die_Antwort_gelesen_wird_dann_stehen_weder_Zeiteintraege_noch_Sollbaender_darin()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitFuenfKarten(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync(BurndownRoute(aufbau.Board.BoardId, aufbau.KartenklasseId));

        var rumpf = await antwort.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(rumpf, Does.Not.Contain("zeiteintraege"));
            Assert.That(rumpf, Does.Not.Contain("sollband"));
            Assert.That(rumpf, Does.Contain("kopfzahlen"));
            Assert.That(rumpf, Does.Contain("offeneKarten"));
        });
    }

    private static async Task<Burndownauswertung> LiesBurndown(TestWebApi webApi, long boardId, long kartenklasseId, DateOnly? seit)
    {
        var auswertung = await webApi.Klient.GetFromJsonAsync<Burndownauswertung>(BurndownRoute(boardId, kartenklasseId) + Zeitraumabfrage(seit));
        Assert.That(auswertung, Is.Not.Null, "Die API hat keine Auswertung zurückgegeben.");
        return auswertung!;
    }

    private static string BurndownRoute(long boardId, long kartenklasseId)
    {
        return $"{BoardsRoute}/{boardId}/kartenklassen/{kartenklasseId}/burndown";
    }

    private static string Zeitraumabfrage(DateOnly? seit)
    {
        if (seit is null)
        {
            return string.Empty;
        }

        return $"?seit={seit.Value.ToString(Isodatumsformat, CultureInfo.InvariantCulture)}";
    }

    private static DateOnly Heute()
    {
        return DateOnly.FromDateTime(DateTime.Today); // stil-check: C03 dieselbe Uhr wie die WebApi, deren Achsenende der Test prüft
    }

    // Fünf Karten: WBS-01 vor vier Tagen erledigt, WBS-02 und WBS-03 vor zwei Tagen, WBS-04 ohne
    // Datum und archiviert, WBS-05 mit einem Datum in der Zukunft.
    private static async Task<Probeaufbau> AufbauMitFuenfKarten(TestWebApi webApi, TemporaereDatenbank datenbank)
    {
        var aufbau = await Aufbau(webApi);
        var abschlussspalte = aufbau.Board.Spalten[^1].SpalteId;
        var erste = await LegeKlassenkarteAn(webApi, aufbau, "Board anlegen", abschlussspalte);
        var zweite = await LegeKlassenkarteAn(webApi, aufbau, "Boards auflisten", abschlussspalte);
        var dritte = await LegeKlassenkarteAn(webApi, aufbau, "Board umbenennen", abschlussspalte);
        var vierte = await LegeKlassenkarteAn(webApi, aufbau, "Board archivieren", aufbau.Board.Spalten[0].SpalteId);
        var fuenfte = await LegeKlassenkarteAn(webApi, aufbau, "Board oeffnen", abschlussspalte);
        using var archiviert = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{aufbau.Board.BoardId}/karten/{vierte}/archivierung", new Archivierung(true));
        archiviert.EnsureSuccessStatusCode();
        DatiereErledigungUm(datenbank, erste, Heute().AddDays(-4));
        DatiereErledigungUm(datenbank, zweite, Heute().AddDays(-2));
        DatiereErledigungUm(datenbank, dritte, Heute().AddDays(-2));
        DatiereErledigungUm(datenbank, fuenfte, Heute().AddDays(2));
        return aufbau;
    }

    // Der Zug in die Abschlussspalte setzt das heutige Datum; für eine Achse über mehrere Tage
    // schreibt der Test es um — derselbe Zustand, den ein über Tage gefülltes Board hätte.
    private static void DatiereErledigungUm(TemporaereDatenbank datenbank, long karteId, DateOnly erledigtAm)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var betroffeneZeilen = verbindung.Execute(@"
            UPDATE Karteerledigung
               SET ErledigtAm = @ErledigtAm
             WHERE Karte = @Karte",
            new { Karte = karteId, ErledigtAm = erledigtAm.ToString(Isodatumsformat, CultureInfo.InvariantCulture) });
        Assert.That(betroffeneZeilen, Is.EqualTo(1), $"Die Karte {karteId} trug keine Erledigungszeile zum Umdatieren.");
    }

    private static async Task<long> LegeKlassenkarteAn(TestWebApi webApi, Probeaufbau aufbau, string titel, long spalteId)
    {
        using var angelegt = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{aufbau.Board.BoardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        angelegt.EnsureSuccessStatusCode();
        var karte = (await angelegt.Content.ReadFromJsonAsync<Karte>())!;
        using var zugeordnet = await webApi.Klient.PutAsJsonAsync($"/api/karten/{karte.KarteId}/kartenklasse", new KartenklasseZuordnenAnfrage(aufbau.KartenklasseId));
        zugeordnet.EnsureSuccessStatusCode();
        return karte.KarteId;
    }

    private static async Task<long> KarteIdVon(TestWebApi webApi, Probeaufbau aufbau, string kartennummer)
    {
        var karten = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Klassenkarte>>($"{BoardsRoute}/{aufbau.Board.BoardId}/kartenklassen/{aufbau.KartenklasseId}/karten");
        Assert.That(karten, Is.Not.Null);
        return karten!.Single(klassenkarte => klassenkarte.Karte.Kartennummer == kartennummer).Karte.KarteId;
    }

    private static async Task<Probeaufbau> Aufbau(TestWebApi webApi)
    {
        var board = await LegeBoardAn(webApi, "KanbanC — Umsetzung");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);
        return new Probeaufbau(board, kartenklasse.KartenklasseId);
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, string name)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage(name, BoardArt.Projekt, null, null));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Board>())!;
    }

    private static async Task<Kartenklasse> LegeKartenklasseAn(TestWebApi webApi, long boardId)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/kartenklassen", new KartenklasseAnlegenAnfrage("WBS", "WBS-"));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Kartenklasse>())!;
    }

    private sealed record Probeaufbau(Board Board, long KartenklasseId);
}
