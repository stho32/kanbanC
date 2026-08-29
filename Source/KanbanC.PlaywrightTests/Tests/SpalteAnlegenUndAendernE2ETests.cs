using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class SpalteAnlegenUndAendernE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_eine_vierte_Spalte_angelegt_wird_dann_steht_sie_am_Ende_der_Liste_mit_Position_4()
    {
        var seite = await BoardMitStandardspalten();
        await Expect(seite.Spalten).ToHaveCountAsync(3);

        await seite.FuelleNeueSpalte("Wartet auf Zulieferung", false, null);
        await seite.LegeSpalteAn();

        await Expect(seite.Spalten).ToHaveCountAsync(4);
        await Expect(seite.Spalten.Nth(3)).ToHaveTextAsync("Wartet auf Zulieferung");
        await Expect(seite.SpaltenzeileAnStelle(3)).ToContainTextAsync("Position 4");

        await seite.Oeffne();
        await seite.ZeigeSpalten(1);
        await Expect(seite.Spalten.Nth(3)).ToHaveTextAsync("Wartet auf Zulieferung");
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_In_Arbeit_umbenannt_wird_dann_zeigt_die_Liste_In_Umsetzung_an_derselben_Stelle()
    {
        var seite = await BoardMitStandardspalten();
        await Expect(seite.Spalten.Nth(1)).ToHaveTextAsync("In Arbeit");

        await seite.BearbeiteSpalte(seite.SpaltenzeileAnStelle(1), "In Umsetzung", false, "");

        await Expect(seite.Spalten.Nth(1)).ToHaveTextAsync("In Umsetzung");
        await Expect(seite.SpaltenzeileAnStelle(1)).ToContainTextAsync("Position 2");
        await Expect(seite.Spalten.Nth(0)).ToHaveTextAsync("Zu erledigen");

        await seite.Oeffne();
        await seite.ZeigeSpalten(1);
        await Expect(seite.Spalten.Nth(1)).ToHaveTextAsync("In Umsetzung");
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_eine_zweite_Spalte_als_Abschlussspalte_markiert_wird_dann_traegt_jede_ihre_eigene_Anzeigegrenze()
    {
        var seite = await BoardMitStandardspalten();
        await seite.FuelleNeueSpalte("Abgenommen", false, null);
        await seite.LegeSpalteAn();
        await Expect(seite.Spalten).ToHaveCountAsync(4);

        await seite.BearbeiteSpalte(seite.SpaltenzeileAnStelle(3), "Abgenommen", true, "10");

        await Expect(seite.Spalten.Nth(3)).ToContainTextAsync("Abschlussspalte, Anzeigegrenze 10");
        await Expect(seite.Spalten.Nth(2)).ToContainTextAsync("Abschlussspalte, Anzeigegrenze 20");

        await seite.Oeffne();
        await seite.ZeigeSpalten(1);
        await Expect(seite.Spalten.Nth(3)).ToContainTextAsync("Anzeigegrenze 10");
        await Expect(seite.Spalten.Nth(2)).ToContainTextAsync("Anzeigegrenze 20");
    }

    [Test]
    [Category("US-7")]
    public async Task Wenn_eine_Spalte_ohne_Bezeichnung_angelegt_wird_dann_erscheint_eine_lesbare_Meldung_und_die_Liste_bleibt_unveraendert()
    {
        var seite = await BoardMitStandardspalten();
        await Expect(seite.Spalten).ToHaveCountAsync(3);

        await seite.FuelleNeueSpalte("", false, null);
        await seite.LegeSpalteAn();

        await Expect(seite.SpaltenZurueckweisung).ToBeVisibleAsync();
        await Expect(seite.SpaltenZurueckweisung).ToContainTextAsync("Die Bezeichnung darf nicht leer sein.");
        await Expect(seite.Spalten).ToHaveCountAsync(3);

        await seite.Oeffne();
        await seite.ZeigeSpalten(1);
        await Expect(seite.Spalten).ToHaveCountAsync(3);
    }

    [Test]
    [Category("US-8")]
    public async Task Wenn_eine_Markierung_ohne_Anzeigegrenze_gesetzt_wird_dann_erscheint_eine_Meldung_und_die_Spalte_bleibt_unmarkiert()
    {
        var seite = await BoardMitStandardspalten();

        await seite.FuelleNeueSpalte("Abgenommen", true, null);
        await seite.LegeSpalteAn();

        await Expect(seite.SpaltenZurueckweisung).ToBeVisibleAsync();
        await Expect(seite.SpaltenZurueckweisung).ToContainTextAsync("Eine Abschlussspalte braucht eine Anzeigegrenze.");
        await Expect(seite.Spalten).ToHaveCountAsync(3);
        await Expect(seite.Spalten.Nth(0)).Not.ToContainTextAsync("Abschlussspalte");
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
