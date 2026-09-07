using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Der Weg des Agenten zum Soll-Ist-Vergleich: **gerechnet** kommt zurück, was ein Mensch am
// Schirm sieht — Zeilen mit Ist, Soll und Abweichung und eine Summe darunter, aber keine
// Zeiteinträge zum Selberaddieren.
public class SollIstEndpunktTests
{
    private const string BoardsRoute = "/api/boards";
    private const string KontributorenRoute = "/api/kontributoren";
    private const string Herkunftspfad = "Dokumentation/Planung/probe.md";
    private static readonly DateTimeOffset MorgensAchtUhr = new(2026, 9, 6, 8, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Wenn_der_Bestand_abgerufen_wird_dann_traegt_jede_Zeile_Nummer_Titel_Ist_Soll_und_Abweichung()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitEingefahrenerDatei(webApi);
        await TrageZeitNach(webApi, aufbau, "WBS-01", TimeSpan.FromHours(3));

        var auswertung = await LiesSollIst(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId);

        var zeile = auswertung.Zeilen.Single(eintrag => eintrag.Kartennummer == "WBS-01");
        Assert.Multiple(() =>
        {
            Assert.That(zeile.Titel, Is.EqualTo("[I0001] Board anlegen"));
            Assert.That(zeile.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(3)));
            Assert.That(zeile.Sollband, Is.EqualTo(new Zeitband(2.4m, 4.4m)));
            Assert.That(zeile.Abweichung, Is.EqualTo(new Abweichung(Abweichungslage.ImBand, null)));
            Assert.That(zeile.IstArchiviert, Is.False);
            Assert.That(zeile.KarteId, Is.GreaterThan(0));
        });
    }

    // Der Bestand ist Board × Kartenklasse — dasselbe Set, das `GET .../karten` liefert, in
    // Kartennummernfolge und ohne dass eine Karte herausfällt.
    [Test]
    public async Task Wenn_der_Bestand_abgerufen_wird_dann_stehen_alle_Karten_der_Klasse_in_Kartennummernfolge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitEingefahrenerDatei(webApi);

        var auswertung = await LiesSollIst(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId);

        Assert.That(auswertung.Zeilen.Select(zeile => zeile.Kartennummer), Is.EqualTo(new[] { "WBS-01", "WBS-02", "WBS-03" }));
    }

    // Rand 2 der Zusage „keine Karte fällt aus der Tabelle": ohne Zeiteintrag steht 0:00 da.
    [Test]
    public async Task Wenn_eine_Karte_keinen_Zeiteintrag_hat_dann_steht_sie_mit_null_Zeit_in_der_Auswertung()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitEingefahrenerDatei(webApi);

        var auswertung = await LiesSollIst(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId);

        var zeile = auswertung.Zeilen.Single(eintrag => eintrag.Kartennummer == "WBS-02");
        Assert.Multiple(() =>
        {
            Assert.That(zeile.ErfassteZeit, Is.EqualTo(TimeSpan.Zero));
            Assert.That(zeile.Sollband, Is.EqualTo(new Zeitband(2.0m, 2.0m)));
            Assert.That(zeile.Abweichung, Is.EqualTo(new Abweichung(Abweichungslage.UnterDemBand, null)));
        });
    }

    // Rand 3: ohne Soll weder Band noch Abweichung — nicht `im Band`, nicht `0`.
    [Test]
    public async Task Wenn_unter_einer_Karte_kein_Aufwand_steht_dann_traegt_ihre_Zeile_weder_Sollband_noch_Abweichung()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitEingefahrenerDatei(webApi);

        var auswertung = await LiesSollIst(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId);

        var zeile = auswertung.Zeilen.Single(eintrag => eintrag.Kartennummer == "WBS-03");
        Assert.Multiple(() =>
        {
            Assert.That(zeile.Sollband, Is.Null);
            Assert.That(zeile.Abweichung, Is.Null);
            Assert.That(auswertung.Summe.KartenOhneSoll, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Wenn_der_Bestand_abgerufen_wird_dann_traegt_die_Summenzeile_ein_Band_und_ihre_eigene_Abweichung()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitEingefahrenerDatei(webApi);
        await TrageZeitNach(webApi, aufbau, "WBS-01", TimeSpan.FromHours(3));
        await TrageZeitNach(webApi, aufbau, "WBS-02", TimeSpan.FromHours(5));

        var auswertung = await LiesSollIst(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Summe.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(8)));
            Assert.That(auswertung.Summe.Sollband, Is.EqualTo(new Zeitband(4.4m, 6.4m)));
            Assert.That(auswertung.Summe.Abweichung, Is.EqualTo(new Abweichung(Abweichungslage.UeberDemBand, 1.6m)));
            Assert.That(auswertung.Zeilen.Single(zeile => zeile.Kartennummer == "WBS-02").Abweichung,
                Is.EqualTo(new Abweichung(Abweichungslage.UeberDemBand, 3.0m)));
        });
    }

    // Personenstunden gegen Personenstunden: zwei Kontributoren zur selben Stunde an derselben
    // Karte haben zwei Stunden geleistet, nicht eine.
    [Test]
    public async Task Wenn_zwei_Zeiteintraege_sich_ueberlappen_dann_zaehlen_sie_doppelt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitEingefahrenerDatei(webApi);
        var karteId = await KarteIdVon(webApi, aufbau, "WBS-01");
        await TrageZeitNach(webApi, karteId, aufbau.KontributorId, MorgensAchtUhr, MorgensAchtUhr.AddHours(1));
        await TrageZeitNach(webApi, karteId, aufbau.ZweiterKontributorId, MorgensAchtUhr, MorgensAchtUhr.AddHours(1));

        var auswertung = await LiesSollIst(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId);

        Assert.That(auswertung.Zeilen.Single(zeile => zeile.Kartennummer == "WBS-01").ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(2)));
    }

    // Ein laufender Timer zählt nicht mit: gezählt wird, was abgeschlossen ist.
    [Test]
    public async Task Wenn_ein_Timer_noch_laeuft_dann_zaehlt_er_nicht_mit()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitEingefahrenerDatei(webApi);
        var karteId = await KarteIdVon(webApi, aufbau, "WBS-01");
        await TrageZeitNach(webApi, karteId, aufbau.KontributorId, MorgensAchtUhr, MorgensAchtUhr.AddHours(1));
        using var gestartet = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/zeiten/laufend", new ZeitmessungStartenAnfrage(aufbau.KontributorId));
        gestartet.EnsureSuccessStatusCode();

        var auswertung = await LiesSollIst(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId);

        Assert.That(auswertung.Zeilen.Single(zeile => zeile.Kartennummer == "WBS-01").ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(1)));
    }

    // Eine archivierte Karte steht markiert mit — ihre Zeit wurde geleistet.
    [Test]
    public async Task Wenn_eine_Karte_archiviert_ist_dann_steht_sie_markiert_in_der_Auswertung()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitEingefahrenerDatei(webApi);
        var karteId = await KarteIdVon(webApi, aufbau, "WBS-01");
        await TrageZeitNach(webApi, karteId, aufbau.KontributorId, MorgensAchtUhr, MorgensAchtUhr.AddHours(1));
        using var archiviert = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{aufbau.Board.BoardId}/karten/{karteId}/archivierung", new Archivierung(true));
        archiviert.EnsureSuccessStatusCode();

        var auswertung = await LiesSollIst(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId);

        var zeile = auswertung.Zeilen.Single(eintrag => eintrag.Kartennummer == "WBS-01");
        Assert.Multiple(() =>
        {
            Assert.That(zeile.IstArchiviert, Is.True);
            Assert.That(zeile.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(1)));
            Assert.That(auswertung.Summe.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(1)));
        });
    }

    // Ein Bestand ohne Karten ist kein Fehler: die leere Zeilenliste ist die Antwort.
    [Test]
    public async Task Wenn_die_Kartenklasse_keine_Karte_fuehrt_dann_kommt_200_mit_leerer_Zeilenliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        var auswertung = await LiesSollIst(webApi, aufbau.Board.BoardId, aufbau.KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(auswertung.Zeilen, Is.Empty);
            Assert.That(auswertung.Summe.ErfassteZeit, Is.EqualTo(TimeSpan.Zero));
            Assert.That(auswertung.Summe.Sollband, Is.Null);
            Assert.That(auswertung.Summe.Abweichung, Is.Null);
            Assert.That(auswertung.Summe.KartenOhneSoll, Is.Zero);
        });
    }

    [Test]
    public async Task Wenn_es_das_Board_nicht_gibt_dann_kommt_404_mit_Grund_und_Kompensationsaktion()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/999/kartenklassen/{aufbau.KartenklasseId}/soll-ist");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Soll-Ist mit unbekannter BoardId")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("board-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    [Test]
    public async Task Wenn_es_die_Kartenklasse_nicht_gibt_dann_kommt_404_mit_Grund_und_Kompensationsaktion()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{aufbau.Board.BoardId}/kartenklassen/999/soll-ist");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Soll-Ist mit unbekannter KartenklasseId")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kartenklasse-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain($"GET /api/boards/{aufbau.Board.BoardId}/kartenklassen"));
        });
    }

    // „Gibt es, nur nicht hier": ein eigener Code, weil die Kompensation eine andere ist.
    [Test]
    public async Task Wenn_die_Kartenklasse_einem_fremden_Board_gehoert_dann_kommt_404_mit_dem_eigenen_Code()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        var nachbar = await LegeBoardAn(webApi, "Beschaffung");
        var fremdeKartenklasse = await LegeKartenklasseAn(webApi, nachbar.BoardId);

        using var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{aufbau.Board.BoardId}/kartenklassen/{fremdeKartenklasse.KartenklasseId}/soll-ist");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Soll-Ist mit fremder KartenklasseId")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kartenklasse-fremd"));
            Assert.That(befund.Meldung, Does.Contain(nachbar.BoardId.ToString(CultureInfo.InvariantCulture)));
        });
    }

    // Die Antwort **ist** die Auswertung: sie trägt die Zeiteinträge nicht mit, aus denen sie
    // gerechnet ist — ein Agent bekommt den Vergleich, keine Summanden.
    [Test]
    public async Task Wenn_die_Antwort_gelesen_wird_dann_stehen_keine_Zeiteintraege_darin()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await AufbauMitEingefahrenerDatei(webApi);
        await TrageZeitNach(webApi, aufbau, "WBS-01", TimeSpan.FromHours(3));

        using var antwort = await webApi.Klient.GetAsync(SollIstRoute(aufbau.Board.BoardId, aufbau.KartenklasseId));

        var rumpf = await antwort.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(rumpf, Does.Not.Contain("zeiteintraege"));
            Assert.That(rumpf, Does.Not.Contain("beginn"));
            Assert.That(rumpf, Does.Contain("summe"));
        });
    }

    // **Die Sollzeit ist nicht von Hand änderbar**: der einzige Erzeuger ist der Import. Keine
    // Route nimmt sie entgegen — die Vision schließt Planen im Board aus, und ein Endpunkt, der
    // sie setzte, wäre genau das.
    [Test]
    public void Wenn_die_Routen_durchgegangen_werden_dann_setzt_keine_von_ihnen_eine_Sollzeit()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var schreibendeSollzeitrouten = webApi.Routen.Where(SetztEineSollzeit);

        Assert.That(schreibendeSollzeitrouten, Is.Empty);
    }

    private static bool SetztEineSollzeit(string route)
    {
        var dieRouteSchreibt = !route.StartsWith("GET ", StringComparison.Ordinal);
        var dieRouteSprichtVomSoll = route.Contains("soll", StringComparison.OrdinalIgnoreCase);
        return dieRouteSchreibt && dieRouteSprichtVomSoll;
    }

    private static async Task<SollIstAuswertung> LiesSollIst(TestWebApi webApi, long boardId, long kartenklasseId)
    {
        var auswertung = await webApi.Klient.GetFromJsonAsync<SollIstAuswertung>(SollIstRoute(boardId, kartenklasseId));
        Assert.That(auswertung, Is.Not.Null, "Die API hat keine Auswertung zurückgegeben.");
        return auswertung!;
    }

    private static string SollIstRoute(long boardId, long kartenklasseId)
    {
        return $"{BoardsRoute}/{boardId}/kartenklassen/{kartenklasseId}/soll-ist";
    }

    private static async Task TrageZeitNach(TestWebApi webApi, Probeaufbau aufbau, string kartennummer, TimeSpan dauer)
    {
        var karteId = await KarteIdVon(webApi, aufbau, kartennummer);
        await TrageZeitNach(webApi, karteId, aufbau.KontributorId, MorgensAchtUhr, MorgensAchtUhr + dauer);
    }

    private static async Task TrageZeitNach(TestWebApi webApi, long karteId, long kontributorId, DateTimeOffset beginn, DateTimeOffset ende)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/zeiten", new ZeiteintragNachtragenAnfrage(kontributorId, beginn, ende));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task<long> KarteIdVon(TestWebApi webApi, Probeaufbau aufbau, string kartennummer)
    {
        var karten = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Klassenkarte>>($"{BoardsRoute}/{aufbau.Board.BoardId}/kartenklassen/{aufbau.KartenklasseId}/karten");
        Assert.That(karten, Is.Not.Null);
        return karten!.Single(klassenkarte => klassenkarte.Karte.Kartennummer == kartennummer).Karte.KarteId;
    }

    private static async Task<Probeaufbau> AufbauMitEingefahrenerDatei(TestWebApi webApi)
    {
        var aufbau = await Aufbau(webApi);
        var rumpf = new MultipartFormDataContent();
        var datei = new ByteArrayContent(Encoding.UTF8.GetBytes(ProbedateiMitAufwaenden()));
        datei.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        rumpf.Add(datei, "datei", "probe.md");
        rumpf.Add(new StringContent(aufbau.KartenklasseId.ToString(CultureInfo.InvariantCulture)), "klasse");
        rumpf.Add(new StringContent(Herkunftspfad), "pfad");
        rumpf.Add(new StringContent(aufbau.KontributorId.ToString(CultureInfo.InvariantCulture)), "kontributor");
        rumpf.Add(new StringContent("false"), "trocken");
        using var antwort = await webApi.Klient.PostAsync($"{BoardsRoute}/{aufbau.Board.BoardId}/wbs-import", rumpf);
        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Der Import der Probedatei ist nicht durchgekommen.");
        return aufbau;
    }

    // Drei Interactions: zwei mit Aufwänden im Teilbaum (2,4–4,4 und 2,0–2,0), eine ganz ohne.
    private static string ProbedateiMitAufwaenden()
    {
        return string.Join(
            '\n',
            "---",
            "application: Probe",
            "sprache: de",
            "zuletzt: 2026-09-07",
            "---",
            string.Empty,
            "## Knoten",
            string.Empty,
            "| ID | Ebene | Eltern | Name | Status | Fertig-Kriterium | Eingabe → Ausgabe | Aufwand | Ausbaustufe | Braucht | Requirement | Notiz |",
            "|---|---|---|---|---|---|---|---|---|---|---|---|",
            "| A0001 | Application | — | Probe | gelb | alle Dialogs gruen | | | | | | |",
            "| D0001 | Dialog | A0001 | Boards führen | gelb | alle Interactions gruen | | | | | | |",
            "| I0001 | Interaction | D0001 | Board anlegen | rot | Ein neues Board entsteht | | | | | R00001 | |",
            "| B0001 | Bubble | I0001 | Standardspalten erzeugen | rot | Test gruen | — → Vorlage → 3 Spalten | 0,4 | | | | Operation |",
            "| B0002 | Bubble | I0001 | Board schreiben | rot | Test gruen | Entwurf → Repository → Board | 2-4 | | | | Provider |",
            "| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |",
            "| B0003 | Bubble | I0002 | Liste lesen | rot | Test gruen | — → Repository → Boards | 2 | | | | Provider |",
            "| I0003 | Interaction | D0001 | Board umbenennen | rot | Der Name ändert sich | | | | | R00003 | |");
    }

    private static async Task<Probeaufbau> Aufbau(TestWebApi webApi)
    {
        var board = await LegeBoardAn(webApi, "KanbanC — Umsetzung");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);
        var kontributor = await LegeKontributorAn(webApi, "Stefan");
        var zweiter = await LegeKontributorAn(webApi, "Claude");
        return new Probeaufbau(board, kartenklasse.KartenklasseId, kontributor.KontributorId, zweiter.KontributorId);
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

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage(name, Kontributorart.Mensch));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Kontributor>())!;
    }

    private sealed record Probeaufbau(Board Board, long KartenklasseId, long KontributorId, long ZweiterKontributorId);
}
