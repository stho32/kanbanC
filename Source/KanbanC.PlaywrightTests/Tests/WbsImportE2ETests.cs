using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// **Ein** Lauf über die drei Schritte: Datei ablegen, Vorschau sehen, Schnittebene wechseln und
// die Zahl sich ändern sehen, schreiben, Karten auf dem Board. Die Testdatei ist eine kleine WBS —
// die echte Planungsdatei läuft in der Probe des Lesers, nicht durch den Browser.
// Gewartet wird auf Zustände, nie auf Zeit.
[TestFixture]
public class WbsImportE2ETests : PageTest
{
    // US-6: ohne Kartenklasse steht an der Stelle der Kachel der Weg dorthin — die Sperre ist
    // sichtbar, wo sie aufzulösen ist.
    [Test]
    [Category("US-6")]
    public async Task Wenn_das_Board_keine_Kartenklasse_hat_dann_steht_im_Layout_Modus_der_Weg_zur_Klassenpflege_statt_des_Knopfes()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.OeffneImLayoutModus(board.BoardId);

        await Expect(seite.Importkachel).ToBeVisibleAsync();
        await Expect(seite.ImportOhneKlasse).ToContainTextAsync("Ohne Kartenklasse geht es nicht");
        await Expect(seite.ImportKnopf).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_das_Board_eine_Kartenklasse_hat_dann_fuehrt_die_Kachel_auf_den_Importschirm()
    {
        var aufbau = await BoardMitKlasseUndIdentitaet();
        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.OeffneImLayoutModus(aufbau.BoardId);

        await Expect(seite.ImportKnopf).ToBeVisibleAsync();
        await seite.ImportKnopf.ClickAsync();

        var importSeite = new ImportSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await Expect(importSeite.SchrittDatei).ToBeVisibleAsync();
        await Expect(importSeite.Boardname).ToHaveTextAsync("Entwicklung");
    }

    // Der Sperrgrund aus R00024, unverändert wiederverwendet: ohne gewählte Identität nimmt die
    // Fläche keine Datei an — wer importiert, ist der Urheber der entstehenden Karten.
    [Test]
    [Category("US-6")]
    public async Task Wenn_keine_Identitaet_gewaehlt_ist_dann_ist_die_Ablegeflaeche_gesperrt_und_sagt_warum()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var seite = new ImportSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne(board.BoardId);

        await Expect(seite.Dateifeld).ToBeDisabledAsync();
        await Expect(seite.Ablegehinweis).ToContainTextAsync("Wähle oben in der Kopfzeile, wer du bist");
    }

    // Der ganze Lauf in einem Zug — der eine E2E-Test dieses Slice.
    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_WBS_abgelegt_wird_dann_zeigt_Schritt_2_die_Wirkung_und_Schritt_3_legt_die_Karten_an()
    {
        var aufbau = await BoardMitKlasseUndIdentitaet();
        var seite = new ImportSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(aufbau.BoardId);
        await Expect(seite.Agentenaufruf).ToContainTextAsync($"POST /api/boards/{aufbau.BoardId}/wbs-import");

        await seite.LegeDateiAb("kanbanc.md", KleineWbs());

        // Schritt 2: die Wirkung steht im Bild, bevor jemand sie erzeugt.
        await Expect(seite.SchrittVorschau).ToBeVisibleAsync();
        await Expect(seite.Angelegt).ToHaveTextAsync("2 angelegt");
        await Expect(seite.Baumzeilen).ToHaveCountAsync(6);
        await Expect(seite.ZweiterLaufHinweis).ToContainTextAsync("erneut");
        await Expect(seite.Schnittebenenstellung("Interaction")).ToContainTextAsync("2");
        await Expect(seite.Schnittebenenstellung("Dialog")).ToContainTextAsync("1");

        // Der Regler ändert die Zahl, ohne dass etwas entsteht.
        await seite.Schnittebenenstellung("Dialog").ClickAsync();
        await Expect(seite.Angelegt).ToHaveTextAsync("1 angelegt");
        await seite.Schnittebenenstellung("Interaction").ClickAsync();
        await Expect(seite.Angelegt).ToHaveTextAsync("2 angelegt");

        // Bis hierher steht auf dem Board keine Karte.
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var vorDemSchreiben = await webApi.LadeBoard(aufbau.BoardId);
        Assert.That(vorDemSchreiben.Spalten.Sum(spalte => spalte.Kartenzahl), Is.Zero, "Die Vorschau hat geschrieben.");

        await Expect(seite.Schreibknopf).ToHaveTextAsync("2 Karten anlegen");
        await seite.Schreibknopf.ClickAsync();

        // Schritt 3: **eine Zeile** und der Weg zum Board.
        await Expect(seite.Ergebniszeile).ToHaveTextAsync("2 Karten angelegt");
        await Expect(seite.ZumBoard).ToBeVisibleAsync();
        await seite.ZumBoard.ClickAsync();

        var boardSeite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await Expect(boardSeite.Karten).ToHaveCountAsync(2);
        await Expect(boardSeite.Kartentitel.Nth(0)).ToHaveTextAsync("[I0002] Boards auflisten");
        await Expect(boardSeite.Kartennummern.Nth(0)).ToHaveTextAsync("WBS-02");
    }

    // Rand A: was keine WBS ist, wird mit Grund und Kompensation zurückgewiesen — nie mit
    // „ungültiges Format".
    [Test]
    [Category("US-2")]
    public async Task Wenn_eine_Datei_ohne_Frontmatter_abgelegt_wird_dann_sagt_der_Schirm_was_fehlt_und_was_zu_tun_ist()
    {
        var aufbau = await BoardMitKlasseUndIdentitaet();
        var seite = new ImportSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(aufbau.BoardId);

        await seite.LegeDateiAb("Protokoll.md", "# Protokoll\n\nEin Text ohne Knotentabelle.");

        await Expect(seite.Zurueckweisung).ToBeVisibleAsync();
        await Expect(seite.Zurueckweisung).ToContainTextAsync("Protokoll.md");
        await Expect(seite.Zurueckweisung).ToContainTextAsync("/planung anlegen");
        await Expect(seite.SchrittDatei).ToBeVisibleAsync();
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
    }

    // Rand C: fällt die WebApi während des Imports aus, erscheint die bekannte Ausfallmeldung —
    // keine Ausnahmeseite.
    [Test]
    [Category("US-10")]
    public async Task Wenn_die_WebApi_waehrend_des_Imports_ausfaellt_dann_erscheint_die_Ausfallmeldung()
    {
        var aufbau = await BoardMitKlasseUndIdentitaet();
        var seite = new ImportSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(aufbau.BoardId);

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.LegeDateiAb("kanbanc.md", KleineWbs());

        await Expect(seite.Fehlermeldung).ToBeVisibleAsync();
        await Expect(seite.Fehlermeldung).ToContainTextAsync("Die WebApi ist nicht erreichbar.");
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
    }

    // US-9: ein Lauf, eine Meldung, ein Neuladen — und **keine Einflugmarke je Karte**. Der
    // Live-Kanal gilt damit auch fuer einen Weg, der nicht ueber eine Kartenbewegung laeuft.
    [Test]
    [Category("US-9")]
    public async Task Wenn_ein_Agent_importiert_dann_zieht_die_offene_Sicht_ohne_Zutun_nach_und_bekommt_keine_Einflugmarke()
    {
        var aufbau = await BoardMitKlasseUndIdentitaet();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        await Expect(zusehendes.Karten).ToHaveCountAsync(0);

        await webApi.ImportiereWbs(aufbau.BoardId, "kanbanc.md", KleineWbs(), aufbau.KartenklasseId, aufbau.KontributorId, "Dokumentation/Planung/kanbanc.md");

        await Expect(zusehendes.Karten).ToHaveCountAsync(2);
        await Expect(zusehendes.Einflugmarken).ToHaveCountAsync(0);
    }

    // Eine offene Sicht auf ein **anderes** Board laedt nicht: das Ereignis nennt sein Board.
    [Test]
    [Category("US-9")]
    public async Task Wenn_auf_ein_anderes_Board_importiert_wird_dann_bleibt_die_offene_Sicht_unberuehrt()
    {
        var aufbau = await BoardMitKlasseUndIdentitaet();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anderes = await webApi.LegeBoardAn("Anderes");
        var andereKlasse = await webApi.LegeKartenklasseAn(anderes.BoardId, "WBS", "WBS-");
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        await Expect(zusehendes.Karten).ToHaveCountAsync(0);

        await webApi.ImportiereWbs(anderes.BoardId, "kanbanc.md", KleineWbs(), andereKlasse.KartenklasseId, aufbau.KontributorId, "kanbanc.md");
        await webApi.ImportiereWbs(aufbau.BoardId, "kanbanc.md", KleineWbs(), aufbau.KartenklasseId, aufbau.KontributorId, "kanbanc.md");

        // Erst der Lauf auf **diesem** Board bewegt die Sicht — der auf dem anderen hat sie nicht
        // angefasst, sonst stuenden hier schon vorher Karten.
        await Expect(zusehendes.Karten).ToHaveCountAsync(2);
    }

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
            "| I0001 | Interaction | D0001 | Board anlegen | gruen | Ein neues Board entsteht | | | | | R00001 | |",
            "| F0001 | Feature | I0001 | Board anlegen und abrufen | gruen | AK Board anlegen | | | | | R00001 | |",
            "| B0001 | Bubble | F0001 | Standardspalten erzeugen | rot | Test gruen | | 2 | | | | Operation |",
            "| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |");
    }

    private async Task<Importaufbau> BoardMitKlasseUndIdentitaet()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var kartenklasse = await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var kontributor = await webApi.LegeKontributorAn("Stefan", KanbanC.Contracts.Kontributoren.Kontributorart.Mensch);
        var boardSeite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await boardSeite.Oeffne(board.BoardId);
        var rahmen = new Rahmen(Page);
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(kontributor.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync(kontributor.Name);
        return new Importaufbau(board.BoardId, kartenklasse.KartenklasseId, kontributor.KontributorId);
    }

    private sealed record Importaufbau(long BoardId, long KartenklasseId, long KontributorId);
}
