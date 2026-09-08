using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// **Der Lauf geht durch beide Slices**: die Datei wird über den Menüpunkt „Exportieren"
// heruntergeladen und über die Ablegefläche der Boardliste wieder abgelegt. Nur so ist
// bewiesen, dass Ausleitung und Einlesung dasselbe Format meinen.
[TestFixture]
public class BoardImportierenE2ETests : PageTest
{
    private static readonly DateTimeOffset Beginn = new(2026, 9, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Ende = new(2026, 9, 6, 10, 30, 0, TimeSpan.Zero);

    [Test]
    [Category("US-11")]
    public async Task Wenn_die_Boardliste_geoeffnet_wird_dann_steht_der_Importknopf_im_Seitenkopf_neben_dem_Anlegeknopf()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Importknopf).ToHaveTextAsync("Board importieren");
        await Expect(seite.Importflaeche).ToHaveCountAsync(0);
    }

    // US-13: ein Board mit Karte, Kartenklasse, Anhang und Zeiteintrag ausleiten, die geladene
    // Datei ablegen, die Vorschau lesen, anlegen — und danach dreierlei prüfen: das neue Board
    // steht in der Liste, das alte traegt unveraendert seine Karten, und die Kartennummer der
    // importierten Karte ist dieselbe wie im Original.
    [Test]
    [Category("US-13")]
    public async Task Wenn_die_exportierte_Datei_wieder_abgelegt_wird_dann_entsteht_ein_neues_Board_neben_dem_alten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("KanbanC — Release 2");
        var kartenklasse = await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "[I0039] Board importieren");
        await webApi.OrdneKartenklasseZu(karte.KarteId, kartenklasse.KartenklasseId);
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        await webApi.HaengeAnhangAn(karte.KarteId, "bericht.pdf", [1, 2, 3], stefan.KontributorId);
        await webApi.TrageZeitNach(karte.KarteId, stefan.KontributorId, Beginn, Ende);

        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.OeffneMenue(board.BoardId);
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await seite.Exportverweis(board.BoardId).ClickAsync();
        });
        var dateipfad = await download.PathAsync();

        await seite.OeffneImport();
        await seite.LegeBoarddateiAb(dateipfad!);

        await Expect(seite.Importboardname).ToHaveTextAsync("KanbanC — Release 2");
        await Expect(seite.Importzahlen).ToHaveCountAsync(10);
        await Expect(seite.ImportDoppelteNamen).ToContainTextAsync("Stefan");
        await Expect(seite.Importanhanghinweis).ToContainTextAsync("ohne Inhalt");
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);

        await seite.BestaetigeImport();

        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        var neueBoardId = await NeueBoardId(seite);
        var altesBoard = await webApi.LadeRohdatenkarten(board.BoardId);
        var neuesBoard = await webApi.LadeRohdatenkarten(neueBoardId);
        var alteZeiten = await webApi.LadeRohdatenzeiten(board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(neueBoardId, Is.Not.EqualTo(board.BoardId), "Das neue Board hat die Nummer des alten bekommen.");
            Assert.That(altesBoard, Has.Count.EqualTo(1), "Das alte Board hat Karten verloren oder bekommen.");
            Assert.That(altesBoard[0].Karte.Kartennummer, Is.EqualTo("WBS-01"));
            Assert.That(altesBoard[0].Anhaenge, Has.Count.EqualTo(1), "Das alte Board hat seinen Anhang verloren.");
            Assert.That(alteZeiten, Has.Count.EqualTo(1), "Das alte Board hat seinen Zeiteintrag verloren.");
            Assert.That(neuesBoard, Has.Count.EqualTo(1));
            Assert.That(neuesBoard[0].Karte.Kartennummer, Is.EqualTo("WBS-01"), "Die Kartennummer ist nicht mitgereist.");
            Assert.That(neuesBoard[0].Karte.KarteId, Is.Not.EqualTo(altesBoard[0].Karte.KarteId));
        });
    }

    // Die Nummer kommt aus dem Verweis des Berichts, nicht aus einem Literal: sonst haenge der
    // Test am Autoinkrement einer leeren Datenbank.
    private async Task<long> NeueBoardId(BoardsSeite seite)
    {
        var verweis = await seite.Importverweis.GetAttributeAsync("href");
        Assert.That(verweis, Does.StartWith("/boards/"), "Der Bericht fuehrt keinen Verweis auf das neue Board.");
        return long.Parse(verweis!["/boards/".Length..], System.Globalization.CultureInfo.InvariantCulture);
    }

    // **Die Vorschau schreibt nichts**: solange niemand „Board anlegen" klickt, bleibt die Liste,
    // wie sie war — und im ganzen Bestand gäbe es keinen Weg, ein Board wieder zu entfernen.
    [Test]
    [Category("US-1")]
    public async Task Wenn_nur_die_Vorschau_gelesen_wird_dann_steht_kein_neues_Board_in_der_Liste()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("KanbanC — Release 2");
        await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "[I0039] Board importieren");
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.OeffneMenue(board.BoardId);
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await seite.Exportverweis(board.BoardId).ClickAsync();
        });

        await seite.OeffneImport();
        await seite.LegeBoarddateiAb((await download.PathAsync())!);

        await Expect(seite.Importvorschau).ToBeVisibleAsync();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        Assert.That(await webApi.LadeAlleBoards(archiviert: false), Has.Count.EqualTo(1), "Die Vorschau hat geschrieben.");
    }

    // Eine Datei, die kein Board wiederherstellen kann, wird lesbar zurückgewiesen — mit Grund
    // und Kompensationsaktion, nicht als Ausnahmeseite.
    [Test]
    [Category("US-8")]
    public async Task Wenn_eine_fremde_Datei_abgelegt_wird_dann_erscheint_eine_lesbare_Zurueckweisung()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        // stil-check: C03 die abgelegte Datei ist hier der Prüfgegenstand, nicht eine Laufzeitabhängigkeit
        var fremdedatei = Path.Combine(Path.GetTempPath(), $"kanbanc-fremd-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(fremdedatei, "Das ist ein Protokoll und kein Board.");
        try
        {
            var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
            await seite.Oeffne();
            await seite.OeffneImport();

            await seite.Importdateifeld.SetInputFilesAsync(fremdedatei);

            await Expect(seite.Importzurueckweisung).ToBeVisibleAsync();
            await Expect(seite.Importzurueckweisung).ToContainTextAsync("wurde nicht eingelesen");
            var text = await seite.Importzurueckweisung.InnerTextAsync();
            Assert.That(text, Does.Contain("ausleiten"), "Die Zurueckweisung nennt keine Kompensationsaktion.");
            await Expect(seite.Importvorschau).ToHaveCountAsync(0);
            await Expect(seite.Importablegeflaeche).ToBeVisibleAsync();
        }
        finally
        {
            File.Delete(fremdedatei);
        }
    }
}
