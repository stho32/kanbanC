using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class BoardOeffnenE2ETests : PageTest
{
    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Board_ueber_seine_Adresse_aufgerufen_wird_dann_nennt_die_Kopfzeile_Name_Art_und_beide_Termine()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("KanbanC 1.0", "Projekt", "2026-09-01", "2026-12-31");
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(1);

        await Expect(board.Name).ToHaveTextAsync("KanbanC 1.0");
        await Expect(board.Art).ToHaveTextAsync("Projekt");
        await Expect(board.Starttermin).ToHaveTextAsync("2026-09-01");
        await Expect(board.Zieltermin).ToHaveTextAsync("2026-12-31");
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_neues_Board_geoeffnet_wird_dann_stehen_seine_drei_Spalten_als_Bahnen_nebeneinander()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(1);

        await Expect(board.Spaltenbahnen).ToHaveCountAsync(3);
        var ersteBahn = await board.Spaltenbahnen.Nth(0).BoundingBoxAsync();
        var zweiteBahn = await board.Spaltenbahnen.Nth(1).BoundingBoxAsync();
        Assert.That(ersteBahn, Is.Not.Null);
        Assert.That(zweiteBahn, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(zweiteBahn.Y, Is.EqualTo(ersteBahn.Y).Within(1));
            Assert.That(zweiteBahn.X, Is.GreaterThanOrEqualTo(ersteBahn.X + ersteBahn.Width));
        });
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Board_ohne_Termine_geoeffnet_wird_dann_stehen_beide_Termine_als_Gedankenstrich()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(1);

        await Expect(board.Name).ToHaveTextAsync("Entwicklung");
        await Expect(board.Art).ToHaveTextAsync("Linie");
        await Expect(board.Starttermin).ToHaveTextAsync("—");
        await Expect(board.Zieltermin).ToHaveTextAsync("—");
    }
    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Listeneintrag_angeklickt_wird_dann_zeigt_die_Adresszeile_das_Board_und_die_Kopfzeile_Name_und_Art()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await liste.OeffneBoard(1);
        await board.ErwarteGeoeffnet();

        Assert.That(Page.Url, Is.EqualTo(board.Adresse(1)));
        await Expect(board.Name).ToHaveTextAsync("Entwicklung");
        await Expect(board.Art).ToHaveTextAsync("Linie");
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_Board_3_ohne_Besuch_der_Liste_aufgerufen_wird_dann_erscheint_es_und_ueberlebt_einen_Reload()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Wartung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeilen).ToHaveCountAsync(1);
        await liste.FuelleFormular("beschaffung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeilen).ToHaveCountAsync(2);
        await liste.FuelleFormular("KanbanC", "Projekt", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeilen).ToHaveCountAsync(3);

        await using var frischerKontext = await Browser.NewContextAsync();
        var frischeSeite = await frischerKontext.NewPageAsync();
        var board = new BoardSeite(frischeSeite, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(3);

        await Expect(board.Name).ToHaveTextAsync("KanbanC");
        await Expect(board.Art).ToHaveTextAsync("Projekt");
        await board.LadeNeu();
        await board.ErwarteGeoeffnet();
        await Expect(board.Name).ToHaveTextAsync("KanbanC");
        await Expect(board.Spaltenbahnen).ToHaveCountAsync(3);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_vom_geoeffneten_Board_der_Rueckweg_genommen_wird_dann_steht_wieder_die_Board_Liste_da()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeilen).ToHaveCountAsync(1);
        await liste.FuelleFormular("Wartung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeilen).ToHaveCountAsync(2);
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(1);

        await board.VerweisZurListe.ClickAsync();

        await Expect(liste.Boardzeilen).ToHaveCountAsync(2);
        Assert.That(Page.Url, Is.EqualTo($"{Testumgebung.Aktuelle.BlazorAdresse}/boards"));
        await Expect(board.Kopfzeile).ToBeHiddenAsync();
    }
}
