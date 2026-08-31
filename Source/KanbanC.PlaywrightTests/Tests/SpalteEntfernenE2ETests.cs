using KanbanC.PlaywrightTests.Infrastructure;
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
        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(3);

        await seite.EntferneSpalte(seite.SpaltenbahnAnStelle(1));

        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(2);
        await Expect(seite.Spaltenbahnanzeigen.Nth(0)).ToHaveTextAsync("Zu erledigen");
        await Expect(seite.Spaltenbahnanzeigen.Nth(1)).ToContainTextAsync("Erledigt");
        await Expect(seite.SpaltenbahnAnStelle(0)).ToContainTextAsync("Position 1");
        await Expect(seite.SpaltenbahnAnStelle(1)).ToContainTextAsync("Position 2");
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(2);

        await seite.OeffneImLayoutModus(1);
        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(2);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_auch_die_letzte_Spalte_entfernt_wird_dann_bleibt_das_Board_und_die_naechste_Spalte_bekommt_Position_1()
    {
        var seite = await BoardMitStandardspalten();

        await seite.EntferneSpalte(seite.SpaltenbahnAnStelle(0));
        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(2);
        await seite.EntferneSpalte(seite.SpaltenbahnAnStelle(0));
        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(1);
        await seite.EntferneSpalte(seite.SpaltenbahnAnStelle(0));

        await Expect(seite.HinweisKeineSpalten).ToBeVisibleAsync();
        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(0);
        await Expect(seite.Name).ToHaveTextAsync("Entwicklung");

        await seite.FuelleNeueSpalte("Eingang", false, null);
        await seite.LegeSpalteAn();

        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(1);
        await Expect(seite.SpaltenbahnAnStelle(0)).ToContainTextAsync("Position 1");

        await seite.OeffneImLayoutModus(1);
        await Expect(seite.Spaltenbahnanzeigen.Nth(0)).ToHaveTextAsync("Eingang");
    }

    [Test]
    public async Task Wenn_eine_zweite_Sicht_eine_bereits_entfernte_Spalte_entfernt_dann_erscheint_eine_Meldung_statt_eines_Absturzes()
    {
        var seite = await BoardMitStandardspalten();
        var zweiteSeite = new BoardSeite(await Context.NewPageAsync(), Testumgebung.Aktuelle.BlazorAdresse);
        await zweiteSeite.OeffneImLayoutModus(1);
        await Expect(zweiteSeite.Spaltenbahnanzeigen).ToHaveCountAsync(3);

        await seite.EntferneSpalte(seite.SpaltenbahnAnStelle(1));
        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(2);
        await zweiteSeite.EntferneSpalte(zweiteSeite.SpaltenbahnAnStelle(1));

        await Expect(zweiteSeite.SpaltenZurueckweisung).ToBeVisibleAsync();
        await Expect(zweiteSeite.Ausnahmeanzeige).ToBeHiddenAsync();
        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(2);
    }

    [Test]
    public async Task Wenn_nach_einer_zurueckgewiesenen_Entfernung_eine_Spalte_angelegt_wird_dann_nimmt_die_zweite_Sicht_sie_an()
    {
        var seite = await BoardMitStandardspalten();
        var zweiteSeite = new BoardSeite(await Context.NewPageAsync(), Testumgebung.Aktuelle.BlazorAdresse);
        await zweiteSeite.OeffneImLayoutModus(1);
        await seite.EntferneSpalte(seite.SpaltenbahnAnStelle(1));
        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(2);
        await zweiteSeite.EntferneSpalte(zweiteSeite.SpaltenbahnAnStelle(1));
        await Expect(zweiteSeite.SpaltenZurueckweisung).ToBeVisibleAsync();

        await zweiteSeite.FuelleNeueSpalte("Wartet auf Zulieferung", false, null);
        await zweiteSeite.LegeSpalteAn();

        await Expect(zweiteSeite.Spaltenbahnanzeigen).ToHaveCountAsync(3);
        await Expect(zweiteSeite.Spaltenbahnanzeigen.Nth(2)).ToHaveTextAsync("Wartet auf Zulieferung");
        await Expect(zweiteSeite.SpaltenZurueckweisung).ToBeHiddenAsync();
    }

    // Ergaenzt aus R00006: fuer die leere Spalte gilt das R00002-Kriterium unveraendert weiter,
    // fuer die belegte gilt ab hier die Zurueckweisung.
    [Test]
    public async Task Wenn_eine_Spalte_mit_Karten_entfernt_wird_dann_erscheint_eine_lesbare_Meldung_und_die_Bahn_bleibt_stehen()
    {
        var seite = await BoardMitStandardspalten();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Migration schreiben");
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Endpunkt bauen");
        await seite.OeffneImLayoutModus(1);
        await Expect(seite.Karten).ToHaveCountAsync(2);

        await seite.EntferneSpalte(seite.SpaltenbahnAnStelle(0));

        await Expect(seite.SpaltenZurueckweisung).ToBeVisibleAsync();
        await Expect(seite.SpaltenZurueckweisung).ToContainTextAsync("2 Karten");
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(3);
        await Expect(seite.Karten).ToHaveCountAsync(2);

        await seite.EntferneSpalte(seite.SpaltenbahnAnStelle(1));

        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(2);
        await Expect(seite.SpaltenZurueckweisung).ToBeHiddenAsync();
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
        await seite.OeffneImLayoutModus(1);
        return seite;
    }
}
