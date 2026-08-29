using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class SpalteEntfernenE2ETests : PageTest
{
    [Test]
    [Category("US-4")]
    public async Task Wenn_die_mittlere_Spalte_entfernt_wird_dann_bleiben_zwei_mit_den_Positionen_1_und_2()
    {
        var seite = await BoardMitStandardspalten();
        await Expect(seite.Spalten).ToHaveCountAsync(3);

        await seite.EntferneSpalte(seite.SpaltenzeileAnStelle(1));

        await Expect(seite.Spalten).ToHaveCountAsync(2);
        await Expect(seite.Spalten.Nth(0)).ToHaveTextAsync("Zu erledigen");
        await Expect(seite.Spalten.Nth(1)).ToContainTextAsync("Erledigt");
        await Expect(seite.SpaltenzeileAnStelle(0)).ToContainTextAsync("Position 1");
        await Expect(seite.SpaltenzeileAnStelle(1)).ToContainTextAsync("Position 2");

        await seite.Oeffne();
        await seite.ZeigeSpalten(1);
        await Expect(seite.Spalten).ToHaveCountAsync(2);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_auch_die_letzte_Spalte_entfernt_wird_dann_bleibt_das_Board_und_die_naechste_Spalte_bekommt_Position_1()
    {
        var seite = await BoardMitStandardspalten();

        await seite.EntferneSpalte(seite.SpaltenzeileAnStelle(0));
        await Expect(seite.Spalten).ToHaveCountAsync(2);
        await seite.EntferneSpalte(seite.SpaltenzeileAnStelle(0));
        await Expect(seite.Spalten).ToHaveCountAsync(1);
        await seite.EntferneSpalte(seite.SpaltenzeileAnStelle(0));

        await Expect(seite.HinweisKeineSpalten).ToBeVisibleAsync();
        await Expect(seite.Spalten).ToHaveCountAsync(0);
        await Expect(seite.Boardzeile(1)).ToContainTextAsync("Entwicklung");

        await seite.FuelleNeueSpalte("Eingang", false, null);
        await seite.LegeSpalteAn();

        await Expect(seite.Spalten).ToHaveCountAsync(1);
        await Expect(seite.SpaltenzeileAnStelle(0)).ToContainTextAsync("Position 1");

        await seite.Oeffne();
        await seite.ZeigeSpalten(1);
        await Expect(seite.Spalten.Nth(0)).ToHaveTextAsync("Eingang");
    }

    [Test]
    public async Task Wenn_eine_zweite_Sicht_eine_bereits_entfernte_Spalte_entfernt_dann_erscheint_eine_Meldung_statt_eines_Absturzes()
    {
        var seite = await BoardMitStandardspalten();
        var zweiteSeite = new BoardsSeite(await Context.NewPageAsync(), Testumgebung.Aktuelle.BlazorAdresse);
        await zweiteSeite.Oeffne();
        await zweiteSeite.ZeigeSpalten(1);
        await Expect(zweiteSeite.Spalten).ToHaveCountAsync(3);

        await seite.EntferneSpalte(seite.SpaltenzeileAnStelle(1));
        await Expect(seite.Spalten).ToHaveCountAsync(2);
        await zweiteSeite.EntferneSpalte(zweiteSeite.SpaltenzeileAnStelle(1));

        await Expect(zweiteSeite.SpaltenZurueckweisung).ToBeVisibleAsync();
        await Expect(seite.Spalten).ToHaveCountAsync(2);
    }

    private async Task<BoardsSeite> BoardMitStandardspalten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular("Entwicklung", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeile(1)).ToBeVisibleAsync();
        await seite.ZeigeSpalten(1);
        return seite;
    }
}
