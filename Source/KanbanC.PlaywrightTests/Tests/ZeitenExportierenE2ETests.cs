using System.Text;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// **Ein** Lauf über beide Prozesse: eine kleine WBS einfahren, eine Zeit nachtragen,
// `/auswertungen` öffnen, `Zeiten exportieren` wählen, die Zählzeile lesen und **die Datei wirklich
// herunterladen** — geprüft wird die Datei, nicht ein Bild von ihr.
// Gewartet wird auf Zustände, nie auf Zeit.
[TestFixture]
public class ZeitenExportierenE2ETests : PageTest
{
    private static readonly DateTimeOffset Beginn = new(2026, 9, 6, 14, 2, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Ende = new(2026, 9, 6, 14, 50, 0, TimeSpan.Zero);
    private static readonly DateOnly Sechster = new(2026, 9, 6);

    // US-1 und US-8: der Verweis ist ein `<a href>` **auf die WebApi**, und ein Klick löst einen
    // echten Browser-Download mit dem gerechneten Namen und den erwarteten Bytes aus.
    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Verweis_geklickt_wird_dann_kommt_die_Datei_mit_ihrem_Namen_und_den_Zeilen_des_Bestands()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitEinerZeit(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleZeitexport();

        await seite.WaehleBoard(aufbau.BoardId);

        await Expect(seite.Zaehlzeile).ToContainTextAsync("1");
        await Expect(seite.Zaehlzeile).ToContainTextAsync("Einträge");
        await Expect(seite.Zaehlzeile).ToContainTextAsync("Kontributoren");
        await Expect(seite.Zeitexportdateiname).ToContainTextAsync("-zeiten-2026-09-06_2026-09-06.csv");

        var verweis = await seite.Zeitexportverweis.GetAttributeAsync("href");
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await seite.Zeitexportverweis.ClickAsync();
        });

        Assert.That(verweis, Does.StartWith(Testumgebung.Aktuelle.WebApiAdresse), "Der Verweis zeigt nicht auf die WebApi.");
        Assert.That(verweis, Does.Not.StartWith(Testumgebung.Aktuelle.BlazorAdresse));
        Assert.That(download.SuggestedFilename, Does.EndWith("-zeiten-2026-09-06_2026-09-06.csv"));
        var pfad = await download.PathAsync();
        var bytes = await File.ReadAllBytesAsync(pfad!);
        var satz = Encoding.UTF8.GetString(bytes[3..]);
        Assert.Multiple(() =>
        {
            Assert.That(bytes[..3], Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.That(satz, Does.StartWith("Kartennummer;Kartentitel;Kontributor;Art;Beginn;Ende;Dauer\r\n"));
            Assert.That(satz, Does.Contain("WBS-01;[I0001] Board anlegen;Stefan;Mensch;"));
            Assert.That(satz, Does.Contain(";0:48\r\n"));
        });
    }

    // US-3: läuft mindestens ein Eintrag, sagt die Zählzeile es dazu — dieselbe Trennung, die der
    // Zeitenblock der Kartenseite schon macht. Die laufende Zeile steht **nur** dann da.
    [Test]
    [Category("US-3")]
    public async Task Wenn_ein_Eintrag_laeuft_dann_nennt_die_Zaehlzeile_ihn_getrennt()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitEinerZeit(webApi);
        var karten = await webApi.LadeKartenDerSpalte(aufbau.BoardId, aufbau.SpalteId);
        var erste = karten.Single(karte => karte.Kartennummer == "WBS-01");
        await webApi.StarteZeitmessung(erste.KarteId, aufbau.StefanId);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleZeitexport();

        await seite.WaehleBoard(aufbau.BoardId);

        await Expect(seite.Zaehlzeile).ToContainTextAsync("2");
        await Expect(seite.LaufendeZeile).ToContainTextAsync("davon 1 laufend");
        await Expect(seite.LaufendeZeile).ToContainTextAsync("ohne Ende und ohne Dauer in der Datei");
    }

    // Und ohne laufenden Eintrag steht die Zeile nicht da — sonst wäre sie eine Auskunft über
    // nichts.
    [Test]
    [Category("US-3")]
    public async Task Wenn_kein_Eintrag_laeuft_dann_steht_die_laufende_Zeile_nicht_da()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitEinerZeit(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleZeitexport();

        await seite.WaehleBoard(aufbau.BoardId);

        await Expect(seite.Zeitexportverweis).ToBeVisibleAsync();
        await Expect(seite.LaufendeZeile).ToHaveCountAsync(0);
    }

    // US-4: eine Änderung an einer Grenze lässt Zahlen, Dateiname und Verweis folgen — und der
    // Fuß nennt den Aufruf der gewählten Auswertung.
    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_Zeitraumgrenze_gesetzt_wird_dann_folgen_Zahlen_Dateiname_und_Fuss()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitEinerZeit(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleZeitexport();
        await seite.WaehleBoard(aufbau.BoardId);
        await Expect(seite.Zeitexportverweis).ToBeVisibleAsync();

        await seite.WaehleVon(Sechster);

        await Expect(seite.Zeitexportdateiname).ToContainTextAsync("-zeiten-2026-09-06_2026-09-06.csv");
        await Expect(seite.Agentenaufruf).ToContainTextAsync($"GET /api/boards/{aufbau.BoardId}/kartenklassen/{aufbau.KartenklasseId}/zeitexport.csv?von=2026-09-06");
        await Expect(seite.Zeitexportverweis).ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex("von=2026-09-06"));
    }

    // US-6: eine verdrehte Spanne zeigt der Schirm als **Zurückweisung der API** — er prüft nicht
    // selbst nach, sonst stünde dieselbe Regel an zwei Stellen.
    [Test]
    [Category("US-6")]
    public async Task Wenn_bis_vor_von_liegt_dann_steht_die_Zurueckweisung_der_API_am_Schirm()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitEinerZeit(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleZeitexport();
        await seite.WaehleBoard(aufbau.BoardId);

        await seite.WaehleVon(Sechster);
        await Expect(seite.Zeitexportverweis).ToBeVisibleAsync();

        await seite.WaehleBis(new DateOnly(2026, 9, 1));

        await Expect(seite.Zurueckweisung).ToContainTextAsync("tauschen");
        await Expect(seite.Zeitexportverweis).ToHaveCountAsync(0);
    }

    // US-7, Rand 2: Karten ja, Zeiten nein — Meldung mit Kompensationsaktion statt eines Verweises
    // auf eine leere Datei.
    [Test]
    [Category("US-7")]
    public async Task Wenn_der_Bestand_keinen_Zeiteintrag_fuehrt_dann_steht_die_Kompensationsaktion_statt_eines_Verweises()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandOhneZeit(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleZeitexport();

        await seite.WaehleBoard(aufbau.BoardId);

        await Expect(seite.OhneZeitenHinweis).ToContainTextAsync("Starte einen Timer auf einer Karte oder trage eine Zeit nach");
        await Expect(seite.Zeitexportverweis).ToHaveCountAsync(0);
    }

    // US-7, Rand 3: leerer Ausschnitt trotz vorhandener Zeiten — dieselbe Form, aber mit der
    // gewählten Spanne, damit der Unterschied zu Rand 2 lesbar ist.
    [Test]
    [Category("US-7")]
    public async Task Wenn_der_gewaehlte_Ausschnitt_leer_bleibt_dann_nennt_die_Meldung_die_gewaehlte_Spanne()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitEinerZeit(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleZeitexport();
        await seite.WaehleBoard(aufbau.BoardId);
        await Expect(seite.Zeitexportverweis).ToBeVisibleAsync();

        await seite.WaehleVon(new DateOnly(2026, 9, 8));

        await Expect(seite.LeererAusschnittHinweis).ToContainTextAsync("2026-09-08");
        await Expect(seite.Zeitexportverweis).ToHaveCountAsync(0);
        await Expect(seite.OhneZeitenHinweis).ToHaveCountAsync(0);
    }

    // US-7, Rand 1: ein Board ohne Kartenklasse trägt denselben Hinweis wie bei den anderen
    // Auswertungen.
    [Test]
    [Category("US-7")]
    public async Task Wenn_das_Board_keine_Kartenklasse_fuehrt_dann_steht_der_vorhandene_Hinweis()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Beschaffung");
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleZeitexport();

        await seite.WaehleBoard(board.BoardId);

        await Expect(seite.OhneKartenklasseHinweis).ToContainTextAsync("keine Kartenklasse");
    }

    // US-7, Rand 4: fällt die WebApi aus, steht eine lesbare Meldung, und der Umschalter bleibt
    // stehen — er kommt ohne Abruf aus.
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
        await seite.WaehleZeitexport();

        await Expect(seite.Fehlermeldung).ToContainTextAsync("Die WebApi ist nicht erreichbar.");
        await Expect(seite.Auswertungspunkte).ToHaveCountAsync(5);
    }

    // Der Umschalter führt fünf Auswertungen; seit dem Puffer-Verbrauch sind alle fünf davon Wege.
    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Schirm_offen_ist_dann_ist_Zeiten_exportieren_waehlbar_und_kein_Punkt_mehr_gesperrt()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Auswertungspunkte).ToHaveCountAsync(5);
        await Expect(seite.GesperrteAuswertungen).ToHaveCountAsync(0);

        await seite.WaehleZeitexport();

        await Expect(seite.Auswertungstitel).ToContainTextAsync("Zeiten exportieren");
    }

    // Board- und Kartenklassenwahl bleiben gemeinsam: ein Wechsel der Auswertung wirft sie nicht
    // weg.
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Auswertung_gewechselt_wird_dann_bleibt_der_gewaehlte_Bestand_stehen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await BestandMitEinerZeit(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleBoard(aufbau.BoardId);
        await Expect(seite.Tabelle).ToBeVisibleAsync();

        await seite.WaehleZeitexport();

        await Expect(seite.Boardwahl).ToHaveValueAsync(aufbau.BoardId.ToString());
        await Expect(seite.Zeitexportflaeche).ToBeVisibleAsync();
    }

    private static async Task<Bestandsaufbau> BestandMitEinerZeit(WebApiKlient webApi)
    {
        var aufbau = await BestandOhneZeit(webApi);
        var karten = await webApi.LadeKartenDerSpalte(aufbau.BoardId, aufbau.SpalteId);
        var erste = karten.Single(karte => karte.Kartennummer == "WBS-01");
        await webApi.TrageZeitNach(erste.KarteId, aufbau.StefanId, Beginn, Ende);
        return aufbau;
    }

    private static async Task<Bestandsaufbau> BestandOhneZeit(WebApiKlient webApi)
    {
        var board = await webApi.LegeBoardAn("KanbanC — Release 2");
        var kartenklasse = await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        await webApi.ImportiereWbs(board.BoardId, "probe.md", KleineWbs(), kartenklasse.KartenklasseId, stefan.KontributorId, "Dokumentation/Planung/probe.md");
        return new Bestandsaufbau(board.BoardId, kartenklasse.KartenklasseId, board.Spalten[0].SpalteId, stefan.KontributorId);
    }

    private sealed record Bestandsaufbau(long BoardId, long KartenklasseId, long SpalteId, long StefanId);

    // Zwei Interactions, beide rot — sie landen in der ersten Bahn.
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
            "| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |");
    }
}
