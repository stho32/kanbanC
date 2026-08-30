using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class SpaltenbezeichnungE2ETests : PageTest
{
    [Test]
    [Category("US-5")]
    public async Task Wenn_im_Layout_Modus_eine_bereits_vergebene_Bezeichnung_angelegt_wird_dann_erscheint_eine_lesbare_Meldung()
    {
        var seite = await BoardImLayoutModus();
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);

        await seite.FuelleNeueSpalte("erledigt", false, null);
        await seite.LegeSpalteAn();

        await Expect(seite.SpaltenZurueckweisung).ToBeVisibleAsync();
        await Expect(seite.SpaltenZurueckweisung).ToContainTextAsync("schon vergeben");
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
        await Expect(seite.Spaltenbezeichnungen.Nth(2)).ToHaveTextAsync("Erledigt");

        await seite.OeffneImLayoutModus(1);
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
        await Expect(seite.Spaltenbezeichnungen.Nth(2)).ToHaveTextAsync("Erledigt");
    }

    [Test]
    [Category("US-7")]
    public async Task Wenn_nach_einem_Namenskonflikt_eine_freie_Bezeichnung_angelegt_wird_dann_nimmt_die_Seite_sie_an()
    {
        var seite = await BoardImLayoutModus();
        await seite.FuelleNeueSpalte("Erledigt", false, null);
        await seite.LegeSpalteAn();
        await Expect(seite.SpaltenZurueckweisung).ToBeVisibleAsync();

        await seite.FuelleNeueSpalte("Abgenommen", false, null);
        await seite.LegeSpalteAn();

        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(4);
        await Expect(seite.Spaltenbezeichnungen.Nth(3)).ToHaveTextAsync("Abgenommen");
        await Expect(seite.SpaltenZurueckweisung).ToBeHiddenAsync();
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_eine_Bahn_auf_die_Bezeichnung_einer_anderen_gespeichert_wird_dann_bleiben_beide_stehen()
    {
        var seite = await BoardImLayoutModus();

        await seite.BearbeiteSpalte(seite.SpaltenbahnAnStelle(1), "ERLEDIGT", false, "");

        await Expect(seite.SpaltenZurueckweisung).ToBeVisibleAsync();
        await Expect(seite.SpaltenZurueckweisung).ToContainTextAsync("schon vergeben");
        await Expect(seite.Spaltenbezeichnungen.Nth(1)).ToHaveTextAsync("In Arbeit");
        await Expect(seite.Spaltenbezeichnungen.Nth(2)).ToHaveTextAsync("Erledigt");
    }

    [Test]
    [Category("US-7")]
    public async Task Wenn_nach_einem_Namenskonflikt_eine_freie_Bezeichnung_gespeichert_wird_dann_fuehrt_die_Seite_sie_aus()
    {
        var seite = await BoardImLayoutModus();
        await seite.BearbeiteSpalte(seite.SpaltenbahnAnStelle(1), "ERLEDIGT", false, "");
        await Expect(seite.SpaltenZurueckweisung).ToBeVisibleAsync();

        await seite.BearbeiteSpalte(seite.SpaltenbahnAnStelle(1), "In Umsetzung", false, "");

        await Expect(seite.Spaltenbezeichnungen.Nth(1)).ToHaveTextAsync("In Umsetzung");
        await Expect(seite.SpaltenZurueckweisung).ToBeHiddenAsync();

        await seite.OeffneImLayoutModus(1);
        await Expect(seite.Spaltenbezeichnungen.Nth(1)).ToHaveTextAsync("In Umsetzung");
    }

    private async Task<BoardSeite> BoardImLayoutModus()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();
        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.OeffneImLayoutModus(1);
        return seite;
    }
}
