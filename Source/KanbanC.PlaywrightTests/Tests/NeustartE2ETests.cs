using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class NeustartE2ETests : PageTest
{
    [Test]
    public async Task US6_Wenn_die_WebApi_neu_startet_dann_stehen_beide_Boards_noch_in_der_Liste_und_das_dritte_bekommt_Nummer_3()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular("Entwicklung", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        await seite.FuelleFormular("KanbanC 1.0", "Projekt", "2026-09-01", "2026-12-31");
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);

        await Testumgebung.Aktuelle.StarteWebApiNeu();

        await seite.Oeffne();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await Expect(seite.Boardzeile(1)).ToContainTextAsync("Entwicklung");
        await Expect(seite.Boardzeile(2)).ToContainTextAsync("KanbanC 1.0");
        await seite.FuelleFormular("Betrieb", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeile(3)).ToContainTextAsync("Betrieb");
    }
}
