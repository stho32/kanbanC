using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class BoardOeffnenE2ETests : PageTest
{
    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Board_ueber_seine_Adresse_aufgerufen_wird_dann_nennt_die_Kopfzeile_Name_Art_und_beide_Termine()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("KanbanC 1.0", "Projekt", "2026-09-01", "2026-12-31");
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(1);

        await Expect(board.Name).ToHaveTextAsync("KanbanC 1.0");
        await Expect(board.Art).ToHaveTextAsync("Projekt");
        await Expect(board.Starttermin).ToHaveTextAsync("2026-09-01");
        await Expect(board.Zieltermin).ToHaveTextAsync("2026-12-31");
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Board_ohne_Termine_geoeffnet_wird_dann_stehen_beide_Termine_als_Gedankenstrich()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(1);

        await Expect(board.Name).ToHaveTextAsync("Entwicklung");
        await Expect(board.Art).ToHaveTextAsync("Linie");
        await Expect(board.Starttermin).ToHaveTextAsync("—");
        await Expect(board.Zieltermin).ToHaveTextAsync("—");
    }
}
