using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class BoardArchivierenE2ETests : PageTest
{
    [Test]
    [Category("US-4")]
    public async Task Wenn_ein_Board_ueber_das_Menue_archiviert_wird_dann_verschwindet_es_ohne_Reload_aus_der_Standardliste()
    {
        var seite = await UebersichtMitDreiBoards();
        await seite.OeffneMenue(2);

        await seite.Menuepunkt(2, "archivieren").ClickAsync();

        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await Expect(seite.Boardzeile(2)).ToHaveCountAsync(0);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var archivierte = await webApi.LadeAlleBoards(archiviert: true);
        Assert.That(archivierte.Select(board => board.BoardId), Is.EqualTo(new long[] { 2 }));
        Assert.That((await webApi.LadeBoard(2)).IstArchiviert, Is.True);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_die_Seite_nach_dem_Archivieren_neu_geladen_wird_dann_zeigt_sie_weiterhin_zwei_Boards()
    {
        var seite = await UebersichtMitDreiBoards();
        await seite.OeffneMenue(2);
        await seite.Menuepunkt(2, "archivieren").ClickAsync();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);

        await seite.Oeffne();

        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await Expect(seite.Boardzeile(2)).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_auf_archivierte_gewechselt_wird_dann_steht_dort_das_abgelegte_Board_als_archiviert_erkennbar()
    {
        var seite = await UebersichtMitDreiBoards();
        await seite.OeffneMenue(2);
        await seite.Menuepunkt(2, "archivieren").ClickAsync();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);

        await seite.ZeigeArchivierte();

        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        await Expect(seite.Boardzeile(2)).ToBeVisibleAsync();
        await Expect(seite.ArchivierteKacheln).ToHaveCountAsync(1);
        await Expect(seite.Boardzeile(2)).ToContainTextAsync("archiviert");
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_zwischen_den_Ansichten_gewechselt_wird_dann_zeigt_jede_ihre_eigene_Menge_und_keine_Kachel_der_aktiven_ist_archiviert_markiert()
    {
        var seite = await UebersichtMitDreiBoards();
        await seite.OeffneMenue(2);
        await seite.Menuepunkt(2, "archivieren").ClickAsync();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await seite.ZeigeArchivierte();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);

        await seite.ZeigeAktive();

        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await Expect(seite.ArchivierteKacheln).ToHaveCountAsync(0);
        await Expect(seite.Boardzeile(2)).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_die_Seite_neu_geladen_wird_dann_steht_die_Wahl_wieder_auf_aktive()
    {
        var seite = await UebersichtMitDreiBoards();
        await seite.OeffneMenue(2);
        await seite.Menuepunkt(2, "archivieren").ClickAsync();
        await seite.ZeigeArchivierte();
        await Expect(seite.FilterArchivierte).ToBeCheckedAsync();

        await seite.Oeffne();

        await Expect(seite.FilterAktive).ToBeCheckedAsync();
        await Expect(seite.FilterArchivierte).Not.ToBeCheckedAsync();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
    }

    [Test]
    [Category("US-7")]
    public async Task Wenn_ein_Board_ueber_die_API_archiviert_wurde_dann_fehlt_es_in_der_danach_geoeffneten_Standardliste_und_steht_unter_archivierte()
    {
        var seite = await UebersichtMitDreiBoards();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);

        await webApi.SchalteArchivierung(2, istArchiviert: true);
        await seite.Oeffne();

        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await Expect(seite.Boardzeile(2)).ToHaveCountAsync(0);

        await seite.ZeigeArchivierte();

        await Expect(seite.Boardzeile(2)).ToBeVisibleAsync();
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_ein_archiviertes_Board_zurueckgeholt_wird_dann_verschwindet_es_aus_der_archivierten_Ansicht_und_steht_wieder_an_seiner_alphabetischen_Stelle()
    {
        var seite = await UebersichtMitDreiBoards();
        await seite.OeffneMenue(2);
        await seite.Menuepunkt(2, "archivieren").ClickAsync();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await seite.ZeigeArchivierte();
        await Expect(seite.Zurueckholen(2)).ToBeVisibleAsync();

        await seite.Zurueckholen(2).ClickAsync();

        await Expect(Page).ToHaveURLAsync($"{Testumgebung.Aktuelle.BlazorAdresse}/boards");
        await Expect(seite.Boardzeilen).ToHaveCountAsync(0);
        await Expect(seite.HinweisKeineBoards).ToBeVisibleAsync();

        await seite.ZeigeAktive();

        await Expect(seite.Boardzeilen).ToHaveCountAsync(3);
        var projektboards = seite.KachelnImBand(seite.BandProjektboards);
        await Expect(projektboards).ToHaveCountAsync(1);
        await Expect(projektboards.Nth(0)).ToContainTextAsync("KanbanC — Release 1");
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var zurueckgeholt = await webApi.LadeBoard(2);
        Assert.That(zurueckgeholt.IstArchiviert, Is.False);
        Assert.That(zurueckgeholt.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_die_aktive_Ansicht_gezeigt_wird_dann_traegt_keine_Kachel_zurueckholen_und_das_Menue_bietet_Archivieren_an()
    {
        var seite = await UebersichtMitDreiBoards();

        await Expect(seite.Zurueckholen(1)).ToHaveCountAsync(0);
        await seite.OeffneMenue(1);
        await Expect(seite.Menuepunkt(1, "archivieren")).ToBeVisibleAsync();

        await seite.SchalteMenue(1);
        await seite.OeffneMenue(2);
        await seite.Menuepunkt(2, "archivieren").ClickAsync();
        await seite.ZeigeArchivierte();

        await Expect(seite.Zurueckholen(2)).ToBeVisibleAsync();
        await seite.OeffneMenue(2);
        await Expect(seite.Menuepunkt(2, "archivieren")).ToHaveCountAsync(0);
        await Expect(seite.Menuepunkt(2, "umbenennen")).ToBeVisibleAsync();
    }

    private async Task<BoardsSeite> UebersichtMitDreiBoards()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular("Entwicklung", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        await seite.FuelleFormular("KanbanC — Release 1", "Projekt", "2026-01-01", "2026-06-30");
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await seite.FuelleFormular("Vertrieb", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(3);
        return seite;
    }
}
