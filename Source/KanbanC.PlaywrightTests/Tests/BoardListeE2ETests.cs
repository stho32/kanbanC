using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class BoardListeE2ETests : PageTest
{
    [Test]
    [Category("US-4")]
    public async Task Wenn_drei_Boards_gemischter_Schreibweise_angelegt_sind_dann_stehen_sie_alphabetisch_in_der_Liste()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular("Wartung", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        await seite.FuelleFormular("beschaffung", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await seite.FuelleFormular("KanbanC", "Projekt", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(3);

        await seite.Oeffne();

        await Expect(seite.Boardzeilen).ToHaveCountAsync(3);
        await Expect(seite.Boardzeilen.Nth(0)).ToContainTextAsync("beschaffung");
        await Expect(seite.Boardzeilen.Nth(1)).ToContainTextAsync("KanbanC");
        await Expect(seite.Boardzeilen.Nth(2)).ToContainTextAsync("Wartung");
    }
}
