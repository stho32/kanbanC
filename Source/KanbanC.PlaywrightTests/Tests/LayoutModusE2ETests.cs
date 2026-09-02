using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class LayoutModusE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Board_geoeffnet_wird_dann_stehen_die_Bahnen_ohne_Bedienelemente_zur_Spaltenpflege_da()
    {
        var seite = await BoardMitStandardspalten();

        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
        await Expect(seite.Spaltenbezeichnungen.Nth(0)).ToHaveTextAsync("Zu erledigen");
        await Expect(seite.Spaltenbezeichnungen.Nth(1)).ToHaveTextAsync("In Arbeit");
        await Expect(seite.Spaltenbezeichnungen.Nth(2)).ToHaveTextAsync("Erledigt");
        await Expect(seite.Abschlussvermerke).ToHaveCountAsync(1);
        await Expect(seite.Abschlussvermerke.Nth(0)).ToContainTextAsync("Grenze 20");
        await Expect(seite.Bahnbearbeitungen).ToHaveCountAsync(0);
        await Expect(seite.Anlegeformular).ToHaveCountAsync(0);
        await Expect(seite.LayoutBearbeiten).ToBeVisibleAsync();
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_Layout_bearbeiten_gedrueckt_wird_dann_werden_die_Bahnen_bearbeitbar_und_ein_Fertig_erscheint()
    {
        var seite = await BoardMitStandardspalten();
        await Expect(seite.Bahnbearbeitungen).ToHaveCountAsync(0);

        await seite.LayoutBearbeiten.ClickAsync();

        await Expect(seite.Bahnbearbeitungen).ToHaveCountAsync(3);
        await Expect(seite.Anlegeformular).ToBeVisibleAsync();
        await Expect(seite.LayoutFertig).ToBeVisibleAsync();
        await Expect(seite.LayoutBearbeiten).ToHaveCountAsync(0);
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_In_Arbeit_im_Layout_Modus_nach_vorn_geschoben_wird_dann_wandert_die_Bahn_sichtbar_mit()
    {
        var seite = await BoardMitStandardspalten();
        await seite.BetreteLayoutModus();
        await Expect(seite.Spaltenbezeichnungen.Nth(1)).ToHaveTextAsync("In Arbeit");

        await seite.SchiebeSpalteHoch(seite.SpaltenbahnAnStelle(1));

        await Expect(seite.Spaltenbezeichnungen.Nth(0)).ToHaveTextAsync("In Arbeit");
        await Expect(seite.Spaltenbezeichnungen.Nth(1)).ToHaveTextAsync("Zu erledigen");
        await Expect(seite.Spaltenbezeichnungen.Nth(2)).ToHaveTextAsync("Erledigt");
        await Expect(seite.SpaltenbahnAnStelle(0)).ToContainTextAsync("Position 1");
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_nach_einer_Umbenennung_Fertig_gedrueckt_wird_dann_traegt_die_Bahn_in_der_Arbeitsansicht_den_neuen_Namen()
    {
        var seite = await BoardMitStandardspalten();
        await seite.BetreteLayoutModus();
        await seite.BearbeiteSpalte(seite.SpaltenbahnAnStelle(1), "In Umsetzung", false, "");
        await Expect(seite.Spaltenbezeichnungen.Nth(1)).ToHaveTextAsync("In Umsetzung");

        await seite.LayoutFertig.ClickAsync();

        await Expect(seite.Bahnbearbeitungen).ToHaveCountAsync(0);
        await Expect(seite.Anlegeformular).ToHaveCountAsync(0);
        await Expect(seite.LayoutBearbeiten).ToBeVisibleAsync();
        await Expect(seite.Spaltenbezeichnungen.Nth(1)).ToHaveTextAsync("In Umsetzung");
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_im_Layout_Modus_neu_geladen_wird_dann_steht_die_Arbeitsansicht_da()
    {
        var seite = await BoardMitStandardspalten();
        await seite.BetreteLayoutModus();
        await Expect(seite.Bahnbearbeitungen).ToHaveCountAsync(3);

        await seite.LadeNeu();
        await seite.ErwarteGeoeffnet();

        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
        await Expect(seite.Bahnbearbeitungen).ToHaveCountAsync(0);
        await Expect(seite.Anlegeformular).ToHaveCountAsync(0);
        await Expect(seite.LayoutBearbeiten).ToBeVisibleAsync();
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_das_Board_keine_Spalte_mehr_hat_dann_fuehrt_Layout_bearbeiten_weiterhin_in_den_Modus()
    {
        var seite = await BoardMitStandardspalten();
        await seite.BetreteLayoutModus();
        await seite.EntferneSpalte(seite.SpaltenbahnAnStelle(0));
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(2);
        await seite.EntferneSpalte(seite.SpaltenbahnAnStelle(0));
        await seite.EntferneSpalte(seite.SpaltenbahnAnStelle(0));
        await Expect(seite.HinweisKeineSpalten).ToBeVisibleAsync();
        await seite.VerlasseLayoutModus();
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(0);

        await Expect(seite.LayoutBearbeiten).ToBeVisibleAsync();
        await seite.BetreteLayoutModus();

        await Expect(seite.HinweisKeineSpalten).ToBeVisibleAsync();
        await seite.FuelleNeueSpalte("Eingang", false, null);
        await seite.LegeSpalteAn();
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(1);
        await Expect(seite.Spaltenbezeichnungen.Nth(0)).ToHaveTextAsync("Eingang");
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
