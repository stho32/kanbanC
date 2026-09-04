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
