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
        var seite = await BoardMitStandardspalten();
        await Expect(seite.Spaltenpflegeanzeigen.Nth(2)).ToContainTextAsync("Erledigt");

        await seite.SchiebeSpalteHoch(seite.SpaltenpflegezeileAnStelle(2));
        await Expect(seite.Spaltenpflegeanzeigen.Nth(1)).ToContainTextAsync("Erledigt");
        await seite.SchiebeSpalteHoch(seite.SpaltenpflegezeileAnStelle(1));

        await Expect(seite.Spaltenpflegeanzeigen.Nth(0)).ToContainTextAsync("Erledigt");
        await Expect(seite.Spaltenpflegeanzeigen.Nth(1)).ToHaveTextAsync("Zu erledigen");
        await Expect(seite.Spaltenpflegeanzeigen.Nth(2)).ToHaveTextAsync("In Arbeit");
        await Expect(seite.SpaltenpflegezeileAnStelle(0)).ToContainTextAsync("Position 1");
        await Expect(seite.SpaltenpflegezeileAnStelle(1)).ToContainTextAsync("Position 2");
        await Expect(seite.SpaltenpflegezeileAnStelle(2)).ToContainTextAsync("Position 3");
        await Expect(seite.Spaltenbezeichnungen.Nth(0)).ToHaveTextAsync("Erledigt");

        await seite.Oeffne(1);
        await Expect(seite.Spaltenpflegeanzeigen.Nth(0)).ToContainTextAsync("Erledigt");
        await Expect(seite.Spaltenpflegeanzeigen.Nth(2)).ToHaveTextAsync("In Arbeit");
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_erste_Spalte_nach_unten_geschoben_wird_dann_steht_sie_an_zweiter_Stelle()
    {
        var seite = await BoardMitStandardspalten();
        await Expect(seite.Spaltenpflegeanzeigen.Nth(0)).ToHaveTextAsync("Zu erledigen");

        await seite.SchiebeSpalteRunter(seite.SpaltenpflegezeileAnStelle(0));

        await Expect(seite.Spaltenpflegeanzeigen.Nth(0)).ToHaveTextAsync("In Arbeit");
        await Expect(seite.Spaltenpflegeanzeigen.Nth(1)).ToHaveTextAsync("Zu erledigen");
        await Expect(seite.SpaltenpflegezeileAnStelle(1)).ToContainTextAsync("Position 2");
    }

    [Test]
    public async Task Wenn_eine_zweite_Sicht_mit_veralteter_Spaltenliste_umsortiert_dann_erscheint_eine_Meldung_und_die_Ordnung_bleibt()
    {
        var seite = await BoardMitStandardspalten();
        var zweiteSeite = new BoardSeite(await Context.NewPageAsync(), Testumgebung.Aktuelle.BlazorAdresse);
        await zweiteSeite.Oeffne(1);
        await Expect(zweiteSeite.Spaltenpflegeanzeigen).ToHaveCountAsync(3);

        await seite.EntferneSpalte(seite.SpaltenpflegezeileAnStelle(1));
        await Expect(seite.Spaltenpflegeanzeigen).ToHaveCountAsync(2);
        await zweiteSeite.SchiebeSpalteHoch(zweiteSeite.SpaltenpflegezeileAnStelle(2));

        await Expect(zweiteSeite.SpaltenZurueckweisung).ToBeVisibleAsync();
        await seite.Oeffne(1);
        await Expect(seite.Spaltenpflegeanzeigen.Nth(0)).ToHaveTextAsync("Zu erledigen");
        await Expect(seite.Spaltenpflegeanzeigen.Nth(1)).ToContainTextAsync("Erledigt");
        await Expect(seite.SpaltenpflegezeileAnStelle(1)).ToContainTextAsync("Position 2");
    }

    private async Task<BoardSeite> BoardMitStandardspalten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();
        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        return seite;
    }
}
