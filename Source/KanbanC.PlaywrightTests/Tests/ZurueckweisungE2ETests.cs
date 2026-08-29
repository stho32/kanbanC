using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class ZurueckweisungE2ETests : PageTest
{
    [Test]
    [Category("US-4")]
    public async Task Wenn_das_Formular_ohne_Namen_abgesendet_wird_dann_erscheint_eine_lesbare_Meldung_und_die_Liste_bleibt_unveraendert()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular("Entwicklung", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);

        await seite.FuelleFormular("", "Linie", null, null);
        await seite.SendeFormularAb();

        await Expect(seite.Zurueckweisung).ToBeVisibleAsync();
        await Expect(seite.Zurueckweisung).ToContainTextAsync("Der Name darf nicht leer sein.");
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);

        await seite.Oeffne();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_der_Zieltermin_vor_dem_Starttermin_liegt_dann_erscheint_eine_lesbare_Meldung_und_es_entsteht_kein_Board()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.HinweisKeineBoards).ToBeVisibleAsync();

        await seite.FuelleFormular("KanbanC 1.0", "Projekt", "2026-09-01", "2026-08-01");
        await seite.SendeFormularAb();

        await Expect(seite.Zurueckweisung).ToBeVisibleAsync();
        await Expect(seite.Zurueckweisung).ToContainTextAsync("Der Zieltermin darf nicht vor dem Starttermin liegen.");
        await Expect(seite.HinweisKeineBoards).ToBeVisibleAsync();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(0);

        await seite.Oeffne();
        await Expect(seite.HinweisKeineBoards).ToBeVisibleAsync();
    }
}
