using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// **Ein** Lauf über beide Prozesse: eine kleine WBS mit Aufwänden einfahren, auf einer Karte eine
// Zeit nachtragen, `/auswertungen` über die Kopfzeile öffnen, den Bestand wählen und Ist, Soll und
// Abweichung lesen. Die echte Planungsdatei läuft in der Probe des Imports, nicht durch den
// Browser.
// Gewartet wird auf Zustände, nie auf Zeit.
[TestFixture]
public class SollIstVergleichE2ETests : PageTest
{
    private static readonly DateTimeOffset MorgensAchtUhr = new(2026, 9, 6, 8, 0, 0, TimeSpan.Zero);

    // Der Lauf dieses Slice.
    [Test]
    [Category("US-3")]
    public async Task Wenn_eine_WBS_mit_Aufwaenden_eingefahren_ist_dann_zeigt_der_Schirm_Ist_Soll_und_Abweichung_je_Karte()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("KanbanC — Umsetzung");
        var kartenklasse = await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        await webApi.ImportiereWbs(board.BoardId, "probe.md", WbsMitAufwaenden(), kartenklasse.KartenklasseId, stefan.KontributorId, "Dokumentation/Planung/probe.md");
        var karten = await webApi.LadeKartenDerSpalte(board.BoardId, board.Spalten[0].SpalteId);
        var ersteKarte = karten.Single(karte => karte.Kartennummer == "WBS-01");
        await webApi.TrageZeitNach(ersteKarte.KarteId, stefan.KontributorId, MorgensAchtUhr, MorgensAchtUhr.AddHours(3));

        // Über die Kopfzeile hinein — der Punkt „Auswertungen" ist mit R00036 ein Weg geworden.
        var boards = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await boards.Oeffne();
        await rahmen.PunktAuswertungen.ClickAsync();
        await Expect(seite.Auswertungsliste).ToBeVisibleAsync();
        await Expect(rahmen.Seitentitel).ToHaveTextAsync("Auswertungen");

        await seite.WaehleBoard(board.BoardId);

        // Genau eine Kartenklasse: der Bestand steht damit, ohne dass jemand zweimal wählt.
        await Expect(seite.Tabelle).ToBeVisibleAsync();
        await Expect(seite.Zeilen).ToHaveCountAsync(3);
        await Expect(seite.Zeile(ersteKarte.KarteId)).ToContainTextAsync("3:00");
        await Expect(seite.Zeile(ersteKarte.KarteId)).ToContainTextAsync("2,4–4,4 h");
        await Expect(seite.Zeile(ersteKarte.KarteId)).ToContainTextAsync("im Band");
        await Expect(seite.Summenzeile).ToContainTextAsync("3:00");
        await Expect(seite.Summenzeile).ToContainTextAsync("4,4–6,4 h");
        await Expect(seite.Summenzeile).ToContainTextAsync("unter dem Band");

        // Die Ränder: eine Karte ohne Zeiteintrag steht mit 0:00 da, eine ohne Soll mit dem
        // Gedankenstrich, und die Fußzeile nennt, wie viele es sind.
        var ohneZeit = karten.Single(karte => karte.Kartennummer == "WBS-02");
        var ohneSoll = karten.Single(karte => karte.Kartennummer == "WBS-03");
        await Expect(seite.Zeile(ohneZeit.KarteId)).ToContainTextAsync("0:00");
        await Expect(seite.Zeile(ohneSoll.KarteId)).ToContainTextAsync("—");
        await Expect(seite.Fusszeile).ToContainTextAsync("1 von 3 Karten");

        // Derselbe Weg für einen Agenten steht im Fuß der Fläche.
        await Expect(seite.Agentenaufruf).ToContainTextAsync($"GET /api/boards/{board.BoardId}/kartenklassen/{kartenklasse.KartenklasseId}/soll-ist");
    }

    // Eine archivierte Karte fällt nicht aus der Tabelle, sie steht **markiert** darin: ihre Zeit
    // wurde geleistet und bleibt in der Summe.
    [Test]
    [Category("US-6")]
    public async Task Wenn_eine_Karte_des_Bestands_archiviert_ist_dann_steht_sie_markiert_in_der_Tabelle()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitAufwaenden(webApi);
        var karten = await webApi.LadeKartenDerSpalte(aufbau.Board.BoardId, aufbau.Board.Spalten[0].SpalteId);
        var archivierte = karten.Single(karte => karte.Kartennummer == "WBS-01");
        await webApi.TrageZeitNach(archivierte.KarteId, aufbau.KontributorId, MorgensAchtUhr, MorgensAchtUhr.AddHours(3));
        await webApi.SchalteKartenarchivierung(aufbau.Board.BoardId, archivierte.KarteId, istArchiviert: true);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.WaehleBoard(aufbau.Board.BoardId);

        await Expect(seite.Zeilen).ToHaveCountAsync(3);
        await Expect(seite.Archivmarken).ToHaveCountAsync(1);
        await Expect(seite.Zeile(archivierte.KarteId)).ToContainTextAsync("archiviert");
        await Expect(seite.Summenzeile).ToContainTextAsync("3:00");
    }

    // Führt das Board mehrere Kartenklassen, entscheidet die zweite Wahl den Bestand — und die
    // Tabelle wechselt mit ihr.
    [Test]
    [Category("US-3")]
    public async Task Wenn_das_Board_zwei_Kartenklassen_fuehrt_dann_entscheidet_die_Wahl_der_Klasse_welcher_Bestand_dasteht()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitAufwaenden(webApi);
        var zweiteKlasse = await webApi.LegeKartenklasseAn(aufbau.Board.BoardId, "Bugmeldungen", "BUG-");
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.WaehleBoard(aufbau.Board.BoardId);

        // Bei zwei Klassen wird nicht vorgewählt: erst die Wahl macht den Bestand.
        await Expect(seite.OhneBestandHinweis).ToBeVisibleAsync();

        await seite.WaehleKartenklasse(zweiteKlasse.KartenklasseId);
        await Expect(seite.Leermeldung).ToContainTextAsync("führt keine Karte");

        await seite.WaehleKartenklasse(aufbau.KartenklasseId);
        await Expect(seite.Zeilen).ToHaveCountAsync(3);
    }

    private static async Task<Bestandsaufbau> BestandMitAufwaenden(WebApiKlient webApi)
    {
        var board = await webApi.LegeBoardAn("KanbanC — Umsetzung");
        var kartenklasse = await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        await webApi.ImportiereWbs(board.BoardId, "probe.md", WbsMitAufwaenden(), kartenklasse.KartenklasseId, stefan.KontributorId, "Dokumentation/Planung/probe.md");
        return new Bestandsaufbau(board, kartenklasse.KartenklasseId, stefan.KontributorId);
    }

    private sealed record Bestandsaufbau(Board Board, long KartenklasseId, long KontributorId);

    // Rand 1: ein Bestand ohne Karten zeigt eine lesbare Meldung statt einer leeren Tabelle.
    [Test]
    [Category("US-6")]
    public async Task Wenn_der_Kartenbestand_keine_Karte_fuehrt_dann_steht_eine_lesbare_Leermeldung_statt_einer_leeren_Tabelle()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Frisch");
        await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.WaehleBoard(board.BoardId);

        await Expect(seite.Leermeldung).ToContainTextAsync("führt keine Karte");
        await Expect(seite.Tabelle).ToHaveCountAsync(0);
    }

    // Ein Board ohne Kartenklasse hat keinen Kartenbestand — derselbe Satz wie die Sperre des
    // Imports, weil es dieselbe Voraussetzung ist.
    [Test]
    [Category("US-6")]
    public async Task Wenn_das_Board_keine_Kartenklasse_fuehrt_dann_sagt_der_Schirm_warum_es_nichts_auszuwerten_gibt()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Beschaffung");
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.WaehleBoard(board.BoardId);

        await Expect(seite.OhneKartenklasseHinweis).ToContainTextAsync("keine Kartenklasse");
        await Expect(seite.Tabelle).ToHaveCountAsync(0);
    }

    // Der Umschalter links kommt ohne Abruf aus: die vier noch nicht gebauten Auswertungen stehen
    // sichtbar und ohne Weg daneben.
    [Test]
    [Category("US-3")]
    public async Task Wenn_der_Schirm_offen_ist_dann_steht_Soll_Ist_waehlbar_und_die_uebrigen_vier_gesperrt_daneben()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Auswertungspunkte).ToHaveCountAsync(5);
        await Expect(seite.GesperrteAuswertungen).ToHaveCountAsync(4);
        await Expect(seite.OhneBestandHinweis).ToBeVisibleAsync();
    }

    // Rand 4: fällt die WebApi aus, steht eine lesbare Meldung statt einer Ausnahmeseite — und der
    // Umschalter links bleibt stehen, weil er ohne Abruf auskommt.
    [Test]
    [Category("US-6")]
    public async Task Wenn_die_WebApi_nicht_erreichbar_ist_dann_steht_eine_lesbare_Meldung_und_der_Umschalter_bleibt_stehen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.Boardwahl).ToBeVisibleAsync();

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.Oeffne();

        await Expect(seite.Fehlermeldung).ToContainTextAsync("Die WebApi ist nicht erreichbar.");
        await Expect(seite.Auswertungspunkte).ToHaveCountAsync(5);
    }

    // Eine kleine WBS mit Aufwänden: I0001 trägt 0,4 + 2-4 (2,4–4,4 h), I0002 trägt 2 (2,0–2,0 h),
    // I0003 gar keinen.
    private static string WbsMitAufwaenden()
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
            "| B0001 | Bubble | I0001 | Standardspalten erzeugen | rot | Test gruen | | 0,4 | | | | Operation |",
            "| B0002 | Bubble | I0001 | Board schreiben | rot | Test gruen | | 2-4 | | | | Provider |",
            "| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |",
            "| B0003 | Bubble | I0002 | Liste lesen | rot | Test gruen | | 2 | | | | Provider |",
            "| I0003 | Interaction | D0001 | Board umbenennen | rot | Der Name ändert sich | | | | | R00003 | |");
    }
}
