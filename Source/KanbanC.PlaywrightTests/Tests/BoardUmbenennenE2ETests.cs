using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class BoardUmbenennenE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Board_in_der_Kachel_umbenannt_wird_dann_traegt_die_Liste_den_neuen_Namen_und_ein_Reload_zeigt_ihn_wieder()
    {
        var seite = await UebersichtMitBoard("KanbanC — Release 1");
        await Expect(seite.Boardverweis(1)).ToHaveTextAsync("KanbanC — Release 1");

        await seite.BenenneUm(1, "KanbanC — Release 2");

        await Expect(seite.Boardverweis(1)).ToHaveTextAsync("KanbanC — Release 2");

        await seite.Oeffne();

        await Expect(seite.Boardverweis(1)).ToHaveTextAsync("KanbanC — Release 2");
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LadeBoard(1);
        Assert.That(board.Name, Is.EqualTo("KanbanC — Release 2"));
        Assert.That(board.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_das_Namensfeld_geoeffnet_wird_dann_steht_der_alte_Name_darin_und_daneben_Speichern_und_Abbrechen()
    {
        var seite = await UebersichtMitBoard("KanbanC — Release 1");

        await seite.OeffneNamensfeld(1);

        await Expect(seite.Namenseingabe(1)).ToHaveValueAsync("KanbanC — Release 1");
        await Expect(seite.Boardzeile(1).Locator(".board-kachel-speichern")).ToBeVisibleAsync();
        await Expect(seite.Boardzeile(1).Locator(".board-kachel-abbrechen")).ToBeVisibleAsync();
        await Expect(seite.Boardverweis(1)).ToHaveCountAsync(0);
    }

    private async Task<BoardsSeite> UebersichtMitBoard(string name)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular(name, "Projekt", "2026-09-01", "2026-12-31");
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        return seite;
    }
}
