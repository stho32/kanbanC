using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class SpaltenUmsortierenE2ETests : PageTest
{
    [Test]
    [Category("US-3")]
    public async Task Wenn_Erledigt_zweimal_nach_vorn_geschoben_wird_dann_steht_es_vorn_und_die_Positionen_sind_1_bis_3()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular("Entwicklung", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeile(1)).ToBeVisibleAsync();
        await seite.ZeigeSpalten(1);
        await Expect(seite.Spalten.Nth(2)).ToContainTextAsync("Erledigt");

        await seite.SchiebeSpalteHoch(seite.SpaltenzeileAnStelle(2));
        await Expect(seite.Spalten.Nth(1)).ToContainTextAsync("Erledigt");
        await seite.SchiebeSpalteHoch(seite.SpaltenzeileAnStelle(1));

        await Expect(seite.Spalten.Nth(0)).ToContainTextAsync("Erledigt");
        await Expect(seite.Spalten.Nth(1)).ToHaveTextAsync("Zu erledigen");
        await Expect(seite.Spalten.Nth(2)).ToHaveTextAsync("In Arbeit");
        await Expect(seite.SpaltenzeileAnStelle(0)).ToContainTextAsync("Position 1");
        await Expect(seite.SpaltenzeileAnStelle(1)).ToContainTextAsync("Position 2");
        await Expect(seite.SpaltenzeileAnStelle(2)).ToContainTextAsync("Position 3");

        await seite.Oeffne();
        await seite.ZeigeSpalten(1);
        await Expect(seite.Spalten.Nth(0)).ToContainTextAsync("Erledigt");
        await Expect(seite.Spalten.Nth(2)).ToHaveTextAsync("In Arbeit");
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_erste_Spalte_nach_unten_geschoben_wird_dann_steht_sie_an_zweiter_Stelle()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular("Entwicklung", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeile(1)).ToBeVisibleAsync();
        await seite.ZeigeSpalten(1);
        await Expect(seite.Spalten.Nth(0)).ToHaveTextAsync("Zu erledigen");

        await seite.SchiebeSpalteRunter(seite.SpaltenzeileAnStelle(0));

        await Expect(seite.Spalten.Nth(0)).ToHaveTextAsync("In Arbeit");
        await Expect(seite.Spalten.Nth(1)).ToHaveTextAsync("Zu erledigen");
        await Expect(seite.SpaltenzeileAnStelle(1)).ToContainTextAsync("Position 2");
    }
}
