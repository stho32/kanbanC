using System.Text.Json;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class BoardkachelMenueE2ETests : PageTest
{
    private static readonly DateTimeOffset Beginn = new(2026, 9, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Ende = new(2026, 9, 6, 10, 30, 0, TimeSpan.Zero);

    [Test]
    [Category("US-1")]
    public async Task Wenn_auf_das_Menuezeichen_geklickt_wird_dann_oeffnet_das_Menue_und_ein_zweiter_Klick_schliesst_es_wieder()
    {
        var seite = await UebersichtMitEinemBoard("KanbanC — Release 1");
        await Expect(seite.Menueliste(1)).ToHaveCountAsync(0);

        await seite.SchalteMenue(1);

        await Expect(seite.Menueliste(1)).ToBeVisibleAsync();
        await Expect(seite.Menuepunkt(1, "umbenennen")).ToHaveTextAsync("Umbenennen");

        await seite.SchalteMenue(1);

        await Expect(seite.Menueliste(1)).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Menuepunkt_angeklickt_wird_dann_bleibt_die_Uebersicht_stehen_waehrend_die_uebrige_Kachel_weiterhin_ins_Board_fuehrt()
    {
        var seite = await UebersichtMitEinemBoard("KanbanC — Release 1");
        await seite.OeffneMenue(1);

        await seite.Menuepunkt(1, "umbenennen").ClickAsync();

        // Erst abwarten, dass der Menuepunkt gewirkt hat — sonst waere die Adresspruefung schon
        // gruen, bevor eine faelschlich ausgeloeste Navigation ueberhaupt stattgefunden haette.
        await Expect(seite.Namenseingabe(1)).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync($"{Testumgebung.Aktuelle.BlazorAdresse}/boards");

        await seite.BrichUmbenennenAb(1);
        await seite.OeffneBoard(1);

        await Expect(Page).ToHaveURLAsync($"{Testumgebung.Aktuelle.BlazorAdresse}/boards/1");
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_zwei_Boards_in_der_Liste_stehen_dann_oeffnet_das_Menuezeichen_nur_das_Menue_seiner_eigenen_Kachel()
    {
        var seite = await UebersichtMitEinemBoard("Entwicklung");
        await seite.FuelleFormular("Vertrieb", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);

        await seite.OeffneMenue(1);

        await Expect(seite.Menueliste(1)).ToBeVisibleAsync();
        await Expect(seite.Menueliste(2)).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Board_vor_einer_Kachel_mit_offenem_Namensfeld_verschwindet_dann_bleibt_das_Feld_bei_seiner_eigenen_Kachel()
    {
        var seite = await UebersichtMitEinemBoard("Alpha");
        await seite.FuelleFormular("Beta", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await seite.FuelleFormular("Gamma", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(3);
        await seite.OeffneNamensfeld(2);
        await seite.Namenseingabe(2).FillAsync("Beta neu");

        await seite.OeffneMenue(1);
        await seite.Menuepunkt(1, "archivieren").ClickAsync();

        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await Expect(seite.Namenseingabe(2)).ToBeVisibleAsync();
        await Expect(seite.Namenseingabe(2)).ToHaveValueAsync("Beta neu");
        await Expect(seite.Namenseingabe(3)).ToHaveCountAsync(0);
        await Expect(seite.Boardverweis(3)).ToHaveTextAsync("Gamma");
    }

    // US-7: der dritte Punkt ist ein **Verweis** auf die WebApi, der Klick loest einen echten
    // Download aus, die Boardliste bleibt stehen — und die geladene Datei traegt genau das Board,
    // das der Test aufgebaut hat. Ein Download mit richtigem Namen und falschem Inhalt waere
    // schlimmer als keiner.
    [Test]
    [Category("US-7")]
    public async Task Wenn_Exportieren_geklickt_wird_dann_laedt_der_Browser_die_Datei_des_aufgebauten_Boards_und_die_Seite_bleibt_stehen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("KanbanC — Release 2");
        var kartenklasse = await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "[I0038] Board exportieren");
        await webApi.OrdneKartenklasseZu(karte.KarteId, kartenklasse.KartenklasseId);
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        await webApi.TrageZeitNach(karte.KarteId, stefan.KontributorId, Beginn, Ende);
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.OeffneMenue(board.BoardId);

        var verweis = await seite.Exportverweis(board.BoardId).GetAttributeAsync("href");
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await seite.Exportverweis(board.BoardId).ClickAsync();
        });

        Assert.That(verweis, Does.StartWith(Testumgebung.Aktuelle.WebApiAdresse), "Der Verweis zeigt nicht auf die WebApi.");
        Assert.That(verweis, Does.EndWith($"/api/boards/{board.BoardId}/export.json"));
        Assert.That(download.SuggestedFilename, Does.StartWith("kanbanc-release-2-"));
        Assert.That(download.SuggestedFilename, Does.EndWith(".kanbanc.json"));

        var pfad = await download.PathAsync();
        using var datei = JsonDocument.Parse(await File.ReadAllBytesAsync(pfad!)); // stil-check: C03 die geladene Datei ist hier der Prüfgegenstand
        var wurzel = datei.RootElement;
        var gelesenekarte = wurzel.GetProperty("karten").EnumerateArray().Single();
        var zeiteintrag = wurzel.GetProperty("zeiteintraege").EnumerateArray().Single();
        Assert.Multiple(() =>
        {
            Assert.That(wurzel.GetProperty("board").GetProperty("name").GetString(), Is.EqualTo("KanbanC — Release 2"));
            Assert.That(gelesenekarte.GetProperty("karte").GetProperty("karte").GetProperty("titel").GetString(), Is.EqualTo("[I0038] Board exportieren"));
            Assert.That(gelesenekarte.GetProperty("karte").GetProperty("karte").GetProperty("kartennummer").GetString(), Is.EqualTo("WBS-01"));
            Assert.That(gelesenekarte.GetProperty("zaehlerstand").GetInt32(), Is.EqualTo(1));
            Assert.That(zeiteintrag.GetProperty("kontributor").GetProperty("name").GetString(), Is.EqualTo("Stefan"));
            Assert.That(wurzel.GetProperty("kopf").GetProperty("anhanghinweis").GetString(), Does.Contain("Bytes"));
        });

        await Expect(Page).ToHaveURLAsync($"{Testumgebung.Aktuelle.BlazorAdresse}/boards");
        await Expect(seite.Menueliste(board.BoardId)).ToHaveCountAsync(0);
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
    }

    // US-7: drei Punkte an der aktiven Kachel — Umbenennen, Archivieren, Exportieren.
    [Test]
    [Category("US-7")]
    public async Task Wenn_das_Menue_einer_aktiven_Kachel_offen_ist_dann_fuehrt_es_drei_Punkte()
    {
        var seite = await UebersichtMitEinemBoard("KanbanC — Release 1");

        await seite.OeffneMenue(1);

        await Expect(seite.Menuepunkte(1)).ToHaveCountAsync(3);
        await Expect(seite.Exportverweis(1)).ToHaveTextAsync("Exportieren");
    }

    // US-8: gerade das abgelegte Board ist der wahrscheinlichste Anlass — der Punkt steht dort,
    // wo „Archivieren" fehlt.
    [Test]
    [Category("US-8")]
    public async Task Wenn_das_Menue_einer_archivierten_Kachel_offen_ist_dann_steht_Exportieren_darin()
    {
        var seite = await UebersichtMitEinemBoard("KanbanC — Release 1");
        await seite.OeffneMenue(1);
        await seite.Menuepunkt(1, "archivieren").ClickAsync();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(0);
        await seite.ZeigeArchivierte();
        await Expect(seite.Boardzeile(1)).ToBeVisibleAsync();

        await seite.OeffneMenue(1);

        await Expect(seite.Menuepunkte(1)).ToHaveCountAsync(2);
        await Expect(seite.Menuepunkt(1, "archivieren")).ToHaveCountAsync(0);
        await Expect(seite.Exportverweis(1)).ToHaveTextAsync("Exportieren");
        await Expect(seite.Zurueckholen(1)).ToBeVisibleAsync();

        // „Und ein Klick lädt die Datei genauso": die Anwesenheit des Punktes allein wäre die
        // halbe Zusage.
        var download = await Page.RunAndWaitForDownloadAsync(async () =>
        {
            await seite.Exportverweis(1).ClickAsync();
        });

        Assert.That(download.SuggestedFilename, Does.EndWith(".kanbanc.json"));
        await Expect(seite.Boardzeile(1)).ToBeVisibleAsync();
    }

    private async Task<BoardsSeite> UebersichtMitEinemBoard(string name)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular(name, "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        return seite;
    }
}
