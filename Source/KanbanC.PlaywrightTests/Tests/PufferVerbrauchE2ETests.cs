using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// **Ein** Lauf über beide Prozesse: eine kleine WBS mit Bändern und einer Punktschätzung
// einfahren, auf einer Karte Zeit über der Untergrenze erfassen, eine Karte in die
// Abschlussspalte ziehen, `/auswertungen` öffnen, `Puffer-Verbrauch` wählen und Kopfzahlen,
// Punktlage und Kartenzeile lesen — die Kurve wird an ihren Zahlen geprüft, nicht an einem Bild.
// Gewartet wird auf Zustände, nie auf Zeit.
[TestFixture]
public class PufferVerbrauchE2ETests : PageTest
{
    private static readonly DateTimeOffset MorgensAchtUhr = new(2026, 9, 6, 8, 0, 0, TimeSpan.Zero);

    // US-9: der letzte gesperrte Punkt ist ein Weg geworden.
    [Test]
    [Category("US-9")]
    public async Task Wenn_der_Schirm_offen_ist_dann_ist_Puffer_Verbrauch_waehlbar_und_kein_Punkt_mehr_gesperrt()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Auswertungspunkte).ToHaveCountAsync(5);
        await Expect(seite.GesperrteAuswertungen).ToHaveCountAsync(0);

        await seite.WaehlePuffer();

        await Expect(seite.Auswertungstitel).ToContainTextAsync("Puffer-Verbrauch");
        await Expect(seite.Zeitraumwahl).ToHaveCountAsync(0);
    }

    // Der Lauf dieses Slice: Kopfzahlen, Punktlage, Kartenzeile und der Aufruf im Fuß.
    // WBS-01 trägt 2,4–4,4 h und 3:00 erfasste Zeit und steht in der Abschlussspalte, WBS-02
    // trägt die Punktschätzung 2,0–2,0 ohne Zeit, WBS-03 gar kein Band.
    // Kettenpuffer 6,4 − 4,4 = 2,0 h · verbraucht 3,0 − 2,4 = 0,6 h · Anteil 30 % ·
    // Fortschritt 2,4 / 4,4 = 55 %.
    [Test]
    [Category("US-1")]
    public async Task Wenn_Zeit_ueber_der_Untergrenze_erfasst_ist_dann_zeigt_der_Schirm_Kopfzahlen_Kurve_und_Kartenzeilen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitBaendern(webApi);
        var karten = await webApi.LadeKartenDerSpalte(aufbau.Board.BoardId, aufbau.Board.Spalten[0].SpalteId);
        var mitSpanne = karten.Single(karte => karte.Kartennummer == "WBS-01");
        var ohneBand = karten.Single(karte => karte.Kartennummer == "WBS-03");
        await webApi.TrageZeitNach(mitSpanne.KarteId, aufbau.KontributorId, MorgensAchtUhr, MorgensAchtUhr.AddHours(3));
        await ZieheInDieAbschlussspalte(webApi, aufbau, mitSpanne);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehlePuffer();

        await seite.WaehleBoard(aufbau.Board.BoardId);

        // Genau eine Kartenklasse: der Bestand steht damit, ohne dass jemand zweimal wählt.
        await Expect(seite.Pufferflaeche).ToBeVisibleAsync();
        await Expect(seite.Pufferkopfzahlen).ToContainTextAsync("2,0 h");
        await Expect(seite.Pufferkopfzahlen).ToContainTextAsync("Kettenpuffer");
        await Expect(seite.Pufferkopfzahlen).ToContainTextAsync("0,6 h");
        await Expect(seite.Pufferkopfzahlen).ToContainTextAsync("30 %");
        await Expect(seite.Pufferkopfzahlen).ToContainTextAsync("55 %");
        await Expect(seite.Pufferkopfzahlen).ToContainTextAsync("1 von 3");

        // Die Kurve wird an cx und cy geprüft: 55 % Fortschritt auf 400 Breite, 30 % Verbrauch
        // auf 300 Höhe von unten.
        await Expect(seite.Fieberkurve).ToBeVisibleAsync();
        var punktlage = await seite.Punktlage();
        Assert.Multiple(() =>
        {
            Assert.That(punktlage.X, Is.EqualTo("220"));
            Assert.That(punktlage.Y, Is.EqualTo("210"));
        });
        await Expect(seite.Fieberkurvenpunktwert).ToContainTextAsync("30 %");
        await Expect(seite.Fieberkurvenpunktwert).ToContainTextAsync("grün");

        // Die Tabelle sagt, welche Karte den Puffer frisst — die Antwort, die die Kurve nicht gibt.
        await Expect(seite.Pufferzeilen).ToHaveCountAsync(3);
        await Expect(seite.Pufferzeile(mitSpanne.KarteId)).ToContainTextAsync("2,4–4,4 h");
        await Expect(seite.Pufferzeile(mitSpanne.KarteId)).ToContainTextAsync("3:00");
        await Expect(seite.Pufferzeile(mitSpanne.KarteId)).ToContainTextAsync("0,6 h");
        await Expect(seite.Pufferzeile(mitSpanne.KarteId)).ToContainTextAsync("erledigt");

        // Eine Karte ohne Band steht mit leerem Band und leerem Verbrauch da, nicht mit 0,0 —
        // und die Fußzeile nennt, wie viele es sind.
        await Expect(seite.Pufferzeile(ohneBand.KarteId)).ToContainTextAsync("—");
        await Expect(seite.PufferFusszeile).ToContainTextAsync("1 von 3 Karten");

        // Derselbe Weg für einen Agenten steht im Fuß der Fläche — ohne Abfrageparameter.
        await Expect(seite.Agentenaufruf).ToContainTextAsync($"GET /api/boards/{aufbau.Board.BoardId}/kartenklassen/{aufbau.KartenklasseId}/puffer");
    }

    // US-9 und die gelbe Zone in einem Lauf: der Bestand steht schon (Soll-Ist), der Umschalter
    // wechselt — **die Wahl wird nicht weggeworfen**, die Fläche steht sofort.
    // 3:24 auf WBS-01 sind 3,4 h und verbrauchen 1,0 h der 2,0 h Kettenpuffer: 50 % Anteil bei
    // 55 % Fortschritt liegt zwischen den Grenzen 36,7 % und 70,0 % — die **gelbe** Zone.
    [Test]
    [Category("US-9")]
    public async Task Wenn_bei_stehendem_Bestand_auf_Puffer_Verbrauch_gewechselt_wird_dann_bleibt_die_Wahl_und_der_Punkt_liegt_in_Gelb()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitBaendern(webApi);
        var karten = await webApi.LadeKartenDerSpalte(aufbau.Board.BoardId, aufbau.Board.Spalten[0].SpalteId);
        var mitSpanne = karten.Single(karte => karte.Kartennummer == "WBS-01");
        await webApi.TrageZeitNach(mitSpanne.KarteId, aufbau.KontributorId, MorgensAchtUhr, MorgensAchtUhr.AddMinutes(204));
        await ZieheInDieAbschlussspalte(webApi, aufbau, mitSpanne);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        // Erst der Bestand unter der Vorgabe-Auswertung, dann der Wechsel — ohne zweite Wahl.
        await seite.WaehleBoard(aufbau.Board.BoardId);
        await Expect(seite.Tabelle).ToBeVisibleAsync();

        await seite.WaehlePuffer();

        await Expect(seite.Pufferflaeche).ToBeVisibleAsync();
        await Expect(seite.Boardwahl).ToHaveValueAsync(aufbau.Board.BoardId.ToString());
        await Expect(seite.Pufferkopfzahlen).ToContainTextAsync("50 %");
        await Expect(seite.Fieberkurvenpunktwert).ToContainTextAsync("gelb");

        // Beide Achsen tragen ihre Beschriftung — waagerecht der Fortschritt, senkrecht der
        // Puffer-Verbrauch.
        await Expect(seite.FieberkurvenAchsentitel).ToHaveCountAsync(2);
        await Expect(seite.Fieberkurve).ToContainTextAsync("Fortschritt");
        await Expect(seite.Fieberkurve).ToContainTextAsync("Puffer-Verbrauch");
    }

    // Rand 4: ein Verbrauch über 100 % wird am oberen Rand gezeigt und beziffert — nicht auf
    // 100 % zurückgezogen. 8:00 auf WBS-01 verbrauchen 5,6 h von 2,0 h Kettenpuffer.
    [Test]
    [Category("US-8")]
    public async Task Wenn_mehr_als_der_ganze_Puffer_verbraucht_ist_dann_sitzt_der_Punkt_am_oberen_Rand_und_traegt_seinen_Wert()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitBaendern(webApi);
        var karten = await webApi.LadeKartenDerSpalte(aufbau.Board.BoardId, aufbau.Board.Spalten[0].SpalteId);
        var mitSpanne = karten.Single(karte => karte.Kartennummer == "WBS-01");
        await webApi.TrageZeitNach(mitSpanne.KarteId, aufbau.KontributorId, MorgensAchtUhr, MorgensAchtUhr.AddHours(8));
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehlePuffer();

        await seite.WaehleBoard(aufbau.Board.BoardId);

        await Expect(seite.Pufferkopfzahlen).ToContainTextAsync("280 %");
        var punktlage = await seite.Punktlage();
        Assert.Multiple(() =>
        {
            Assert.That(punktlage.X, Is.EqualTo("0"));
            Assert.That(punktlage.Y, Is.EqualTo("0"));
        });
        await Expect(seite.Fieberkurvenpunktwert).ToContainTextAsync("280 %");
        await Expect(seite.Fieberkurvenpunktwert).ToContainTextAsync("rot");
    }

    // Rand 3: eine Kette aus lauter Punktschätzungen sagt es als Satz statt als Kurve — die
    // Stundenzahlen stehen trotzdem da.
    [Test]
    [Category("US-6")]
    public async Task Wenn_der_Bestand_nur_Punktschaetzungen_traegt_dann_steht_ein_Satz_statt_einer_Kurve()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitPunktschaetzungen(webApi);
        var karten = await webApi.LadeKartenDerSpalte(aufbau.Board.BoardId, aufbau.Board.Spalten[0].SpalteId);
        var erste = karten.Single(karte => karte.Kartennummer == "WBS-01");
        await webApi.TrageZeitNach(erste.KarteId, aufbau.KontributorId, MorgensAchtUhr, MorgensAchtUhr.AddHours(3));
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehlePuffer();

        await seite.WaehleBoard(aufbau.Board.BoardId);

        await Expect(seite.OhnePufferHinweis).ToContainTextAsync("nur Punktschätzungen");
        await Expect(seite.Fieberkurve).ToHaveCountAsync(0);
        await Expect(seite.Pufferkopfzahlen).ToContainTextAsync("0,0 h");
        await Expect(seite.Pufferkopfzahlen).ToContainTextAsync("1,0 h");
        await Expect(seite.Pufferzeilen).ToHaveCountAsync(2);
    }

    // Rand 2: ohne jedes Sollband steht die Kompensationsaktion da statt einer Kurve ohne
    // Achsenwerte.
    [Test]
    [Category("US-7")]
    public async Task Wenn_keine_Karte_ein_Sollband_traegt_dann_steht_die_Kompensationsaktion_statt_einer_Kurve()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandOhneAufwaende(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehlePuffer();

        await seite.WaehleBoard(aufbau.Board.BoardId);

        await Expect(seite.OhneBandHinweis).ToContainTextAsync("erneut ein");
        await Expect(seite.Fieberkurve).ToHaveCountAsync(0);
        await Expect(seite.Pufferzeilen).ToHaveCountAsync(2);
    }

    // Rand 1: ein Bestand ohne Karten zeigt eine lesbare Leermeldung statt einer leeren Fläche.
    [Test]
    [Category("US-7")]
    public async Task Wenn_der_Kartenbestand_keine_Karte_fuehrt_dann_steht_eine_lesbare_Leermeldung_statt_einer_leeren_Flaeche()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Frisch");
        await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehlePuffer();

        await seite.WaehleBoard(board.BoardId);

        await Expect(seite.Leermeldung).ToContainTextAsync("führt keine Karte");
        await Expect(seite.Pufferflaeche).ToHaveCountAsync(0);
    }

    // US-4: eine **archivierte** Karte steht markiert in der Tabelle und wird nicht ausgelassen —
    // ihre Zeit wurde geleistet und bleibt in Kettenpuffer und Verbrauch.
    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_Karte_des_Bestands_archiviert_ist_dann_steht_sie_markiert_in_der_Tabelle_und_bleibt_in_der_Rechnung()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitBaendern(webApi);
        var karten = await webApi.LadeKartenDerSpalte(aufbau.Board.BoardId, aufbau.Board.Spalten[0].SpalteId);
        var archivierte = karten.Single(karte => karte.Kartennummer == "WBS-01");
        await webApi.TrageZeitNach(archivierte.KarteId, aufbau.KontributorId, MorgensAchtUhr, MorgensAchtUhr.AddHours(3));
        await webApi.SchalteKartenarchivierung(aufbau.Board.BoardId, archivierte.KarteId, istArchiviert: true);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehlePuffer();

        await seite.WaehleBoard(aufbau.Board.BoardId);

        await Expect(seite.Pufferzeilen).ToHaveCountAsync(3);
        await Expect(seite.Pufferzeile(archivierte.KarteId)).ToContainTextAsync("archiviert");
        await Expect(seite.Pufferzeile(archivierte.KarteId)).ToContainTextAsync("0,6 h");
        await Expect(seite.Pufferkopfzahlen).ToContainTextAsync("0,6 h");
    }

    // Rand 6: ein Board ohne Kartenklasse hat keinen Kartenbestand — derselbe Hinweis wie bei den
    // übrigen Auswertungen, und kein Aufruf.
    [Test]
    [Category("US-7")]
    public async Task Wenn_das_Board_keine_Kartenklasse_fuehrt_dann_sagt_der_Schirm_warum_es_nichts_auszuwerten_gibt()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Beschaffung");
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehlePuffer();

        await seite.WaehleBoard(board.BoardId);

        await Expect(seite.OhneKartenklasseHinweis).ToContainTextAsync("keine Kartenklasse");
        await Expect(seite.Pufferflaeche).ToHaveCountAsync(0);
    }

    // Rand 7: fällt die WebApi aus, steht eine lesbare Meldung statt einer Ausnahmeseite — und
    // der Umschalter bleibt stehen, weil er ohne Abruf auskommt.
    [Test]
    [Category("US-7")]
    public async Task Wenn_die_WebApi_nicht_erreichbar_ist_dann_steht_eine_lesbare_Meldung_und_der_Umschalter_bleibt_stehen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.Boardwahl).ToBeVisibleAsync();

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.Oeffne();
        await seite.WaehlePuffer();

        await Expect(seite.Fehlermeldung).ToContainTextAsync("Die WebApi ist nicht erreichbar.");
        await Expect(seite.Auswertungspunkte).ToHaveCountAsync(5);
    }

    private static async Task ZieheInDieAbschlussspalte(WebApiKlient webApi, Bestandsaufbau aufbau, Karte karte)
    {
        await webApi.VerschiebeKarte(aufbau.Board.BoardId, karte.KarteId, new Kartenlage(aufbau.Board.Spalten[^1].SpalteId, 1));
    }

    private static async Task<Bestandsaufbau> BestandMitBaendern(WebApiKlient webApi)
    {
        return await FahreEin(webApi, WbsMitBaendern());
    }

    private static async Task<Bestandsaufbau> BestandMitPunktschaetzungen(WebApiKlient webApi)
    {
        return await FahreEin(webApi, WbsMitPunktschaetzungen());
    }

    private static async Task<Bestandsaufbau> BestandOhneAufwaende(WebApiKlient webApi)
    {
        return await FahreEin(webApi, WbsOhneAufwaende());
    }

    private static async Task<Bestandsaufbau> FahreEin(WebApiKlient webApi, string wbs)
    {
        var board = await webApi.LegeBoardAn("KanbanC — Umsetzung");
        var kartenklasse = await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        await webApi.ImportiereWbs(board.BoardId, "probe.md", wbs, kartenklasse.KartenklasseId, stefan.KontributorId, "Dokumentation/Planung/probe.md");
        return new Bestandsaufbau(board, kartenklasse.KartenklasseId, stefan.KontributorId);
    }

    private sealed record Bestandsaufbau(Board Board, long KartenklasseId, long KontributorId);

    // I0001 trägt 0,4 + 2-4 (2,4–4,4 h), I0002 die Punktschätzung 2 (2,0–2,0 h), I0003 gar keine.
    private static string WbsMitBaendern()
    {
        return Wbs(
            "| I0001 | Interaction | D0001 | Board anlegen | rot | Ein neues Board entsteht | | | | | R00001 | |",
            "| B0001 | Bubble | I0001 | Standardspalten erzeugen | rot | Test gruen | | 0,4 | | | | Operation |",
            "| B0002 | Bubble | I0001 | Board schreiben | rot | Test gruen | | 2-4 | | | | Provider |",
            "| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |",
            "| B0003 | Bubble | I0002 | Liste lesen | rot | Test gruen | | 2 | | | | Provider |",
            "| I0003 | Interaction | D0001 | Board umbenennen | rot | Der Name ändert sich | | | | | R00003 | |");
    }

    // Zwei Karten, beide mit Punktschätzung: Σ Bis − Σ Von = 0 — die Kette hat keinen Puffer.
    private static string WbsMitPunktschaetzungen()
    {
        return Wbs(
            "| I0001 | Interaction | D0001 | Board anlegen | rot | Ein neues Board entsteht | | | | | R00001 | |",
            "| B0001 | Bubble | I0001 | Board schreiben | rot | Test gruen | | 2 | | | | Provider |",
            "| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |",
            "| B0002 | Bubble | I0002 | Liste lesen | rot | Test gruen | | 0,4 | | | | Provider |");
    }

    private static string WbsOhneAufwaende()
    {
        return Wbs(
            "| I0001 | Interaction | D0001 | Board anlegen | rot | Ein neues Board entsteht | | | | | R00001 | |",
            "| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |");
    }

    private static string Wbs(params string[] knoten)
    {
        var kopf = new[]
        {
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
        };

        return string.Join('\n', kopf.Concat(knoten));
    }
}
