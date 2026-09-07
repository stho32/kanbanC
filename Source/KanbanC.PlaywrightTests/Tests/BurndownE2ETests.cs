using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// **Ein** Lauf über beide Prozesse: eine kleine WBS einfahren, Karten in die Abschlussspalte
// ziehen, `/auswertungen` öffnen, `Burndown` wählen und Kopfzahlen, Kurvenpunkte und Tageszeilen
// lesen — die Kurve wird an ihren Zahlen geprüft, nicht an einem Bild.
// Gewartet wird auf Zustände, nie auf Zeit.
[TestFixture]
public class BurndownE2ETests : PageTest
{
    // Der Umschalter führt fünf Auswertungen; seit diesem Slice sind zwei davon Wege.
    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Schirm_offen_ist_dann_ist_Burndown_waehlbar_und_die_uebrigen_drei_bleiben_gesperrt()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Auswertungspunkte).ToHaveCountAsync(5);
        await Expect(seite.GesperrteAuswertungen).ToHaveCountAsync(3);
        await Expect(seite.Auswertungstitel).ToContainTextAsync("Soll-Ist");

        await seite.WaehleBurndown();

        await Expect(seite.Auswertungstitel).ToContainTextAsync("Burndown");
    }

    // Der Lauf dieses Slice: Kopfzahlen, Kurve, Tagestabelle und der Zeitraum als Bedienelement.
    [Test]
    [Category("US-1")]
    public async Task Wenn_Karten_in_der_Abschlussspalte_stehen_dann_zeigt_der_Burndown_Kopfzahlen_Kurve_und_Tageszeilen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitDreiKarten(webApi);
        var karten = await webApi.LadeKartenDerSpalte(aufbau.Board.BoardId, aufbau.Board.Spalten[0].SpalteId);
        await ZieheInDieAbschlussspalte(webApi, aufbau, karten.Single(karte => karte.Kartennummer == "WBS-01"));
        await ZieheInDieAbschlussspalte(webApi, aufbau, karten.Single(karte => karte.Kartennummer == "WBS-02"));
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleBurndown();

        await seite.WaehleBoard(aufbau.Board.BoardId);

        // Genau eine Kartenklasse: der Bestand steht damit, ohne dass jemand zweimal wählt.
        await Expect(seite.Kurve).ToBeVisibleAsync();
        await Expect(seite.Kopfzahlen).ToContainTextAsync("1");
        await Expect(seite.Kopfzahlen).ToContainTextAsync("offen");
        await Expect(seite.Kopfzahlen).ToContainTextAsync("erledigt");
        await Expect(seite.Kopfzahlen).ToContainTextAsync("im Bestand");

        // Alles wurde heute erledigt: die Achse trägt den einen Tag heute, und die Tagestabelle
        // nennt beide Karten.
        Assert.That(await seite.Kurvenpunkte(), Has.Count.EqualTo(1));
        await Expect(seite.Tageszeilen).ToHaveCountAsync(1);
        await Expect(seite.Tagestabelle).ToContainTextAsync("WBS-01");
        await Expect(seite.Tagestabelle).ToContainTextAsync("WBS-02");
        await Expect(seite.LetzterKurvenwert).ToHaveTextAsync("1");

        // Der Zeitraum ist ein Bedienelement: ein früherer Beginn verlängert die Achse, und die
        // Kurve bekommt davor ihr flaches Stück.
        await seite.WaehleBeginn(Heute().AddDays(-4));
        await Expect(seite.Kurve).ToBeVisibleAsync();
        await Expect(seite.Kurvenlinie).ToHaveAttributeAsync("points", "0,0 150,0 300,0 450,0 600,106.7");

        // Die Kurve zeigt alle Tage, die Tabelle nur die mit Abschluss — beide aus derselben Reihe.
        await Expect(seite.Tageszeilen).ToHaveCountAsync(1);

        // Der Fuß zeigt den Aufruf der gewählten Auswertung, nicht mehr fest den einen.
        await Expect(seite.Agentenaufruf).ToContainTextAsync($"GET /api/boards/{aufbau.Board.BoardId}/kartenklassen/{aufbau.KartenklasseId}/burndown?seit=");
    }

    // Rand 2: ohne jedes Erledigungsdatum steht die Kompensationsaktion da statt einer Kurve aus
    // einem Punkt. Rand: die Fußzeile nennt die Karten, die die Kurve nie verlassen.
    [Test]
    [Category("US-6")]
    public async Task Wenn_keine_Karte_erledigt_ist_dann_steht_die_Kompensationsaktion_statt_einer_Kurve()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitDreiKarten(webApi);
        var karten = await webApi.LadeKartenDerSpalte(aufbau.Board.BoardId, aufbau.Board.Spalten[0].SpalteId);
        await webApi.SchalteKartenarchivierung(aufbau.Board.BoardId, karten[0].KarteId, istArchiviert: true);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleBurndown();

        await seite.WaehleBoard(aufbau.Board.BoardId);

        await Expect(seite.OhneErledigungHinweis).ToContainTextAsync("Abschlussspalte");
        await Expect(seite.Kurve).ToHaveCountAsync(0);
        await Expect(seite.BurndownFusszeile).ToContainTextAsync("1 von 3 Karten");
    }

    // Rand 3: sind alle Karten erledigt, endet die Kurve auf 0 — kein Sonderfall, sondern das
    // Ergebnis.
    [Test]
    [Category("US-6")]
    public async Task Wenn_alle_Karten_erledigt_sind_dann_endet_die_Kurve_auf_null()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitDreiKarten(webApi);
        var karten = await webApi.LadeKartenDerSpalte(aufbau.Board.BoardId, aufbau.Board.Spalten[0].SpalteId);
        foreach (var karte in karten)
        {
            await ZieheInDieAbschlussspalte(webApi, aufbau, karte);
        }

        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleBurndown();
        await seite.WaehleBoard(aufbau.Board.BoardId);

        await Expect(seite.LetzterKurvenwert).ToHaveTextAsync("0");
        await Expect(seite.Kopfzahlen).ToContainTextAsync("3");
    }

    // Rand 1: ein Bestand ohne Karten zeigt eine lesbare Leermeldung statt einer leeren Fläche.
    [Test]
    [Category("US-6")]
    public async Task Wenn_der_Kartenbestand_keine_Karte_fuehrt_dann_steht_eine_lesbare_Leermeldung_statt_einer_leeren_Flaeche()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Frisch");
        await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleBurndown();

        await seite.WaehleBoard(board.BoardId);

        await Expect(seite.Leermeldung).ToContainTextAsync("führt keine Karte");
        await Expect(seite.Kurve).ToHaveCountAsync(0);
    }

    // Rand 4: fällt die WebApi aus, steht eine lesbare Meldung statt einer Ausnahmeseite — und der
    // Umschalter bleibt stehen, weil er ohne Abruf auskommt.
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
        await seite.WaehleBurndown();

        await Expect(seite.Fehlermeldung).ToContainTextAsync("Die WebApi ist nicht erreichbar.");
        await Expect(seite.Auswertungspunkte).ToHaveCountAsync(5);
    }

    private static async Task ZieheInDieAbschlussspalte(WebApiKlient webApi, Bestandsaufbau aufbau, Karte karte)
    {
        await webApi.VerschiebeKarte(aufbau.Board.BoardId, karte.KarteId, new Kartenlage(aufbau.Board.Spalten[^1].SpalteId, 1));
    }

    private static async Task<Bestandsaufbau> BestandMitDreiKarten(WebApiKlient webApi)
    {
        var board = await webApi.LegeBoardAn("KanbanC — Umsetzung");
        var kartenklasse = await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        await webApi.ImportiereWbs(board.BoardId, "probe.md", KleineWbs(), kartenklasse.KartenklasseId, stefan.KontributorId, "Dokumentation/Planung/probe.md");
        return new Bestandsaufbau(board, kartenklasse.KartenklasseId);
    }

    private static DateOnly Heute()
    {
        return DateOnly.FromDateTime(DateTime.Today); // stil-check: C03 dieselbe Uhr wie die WebApi, deren Achsenende der Test prüft
    }

    private sealed record Bestandsaufbau(Board Board, long KartenklasseId);

    // Drei Interactions, alle rot — sie landen in der ersten Bahn und tragen kein Erledigungsdatum.
    private static string KleineWbs()
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
            "| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |",
            "| I0003 | Interaction | D0001 | Board umbenennen | rot | Der Name ändert sich | | | | | R00003 | |");
    }
}
