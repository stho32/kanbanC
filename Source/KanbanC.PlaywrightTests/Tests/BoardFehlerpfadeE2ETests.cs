using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class BoardFehlerpfadeE2ETests : PageTest
{
    private const string Ausfallmeldung = "Die WebApi ist nicht erreichbar.";

    [Test]
    [Category("US-5")]
    public async Task Wenn_die_Nummer_999_nicht_vergeben_ist_dann_nennt_die_Seite_sie_und_bietet_den_Weg_zur_Liste_an()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeilen).ToHaveCountAsync(1);
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await board.Rufe(999);

        await Expect(board.MeldungUnbekanntesBoard).ToBeVisibleAsync();
        await Expect(board.MeldungUnbekanntesBoard).ToContainTextAsync("999");
        await Expect(board.Kopfdaten).ToBeHiddenAsync();
        await Expect(board.Ausnahmeanzeige).ToBeHiddenAsync();

        await board.VerweisZurListe.ClickAsync();

        await Expect(liste.Boardzeilen).ToHaveCountAsync(1);
        Assert.That(Page.Url, Is.EqualTo($"{Testumgebung.Aktuelle.BlazorAdresse}/boards"));
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_die_WebApi_beim_Oeffnen_eines_Boards_fehlt_dann_erscheint_eine_lesbare_Meldung_statt_einer_Ausnahmeseite()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeilen).ToHaveCountAsync(1);
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(1);
        await Expect(board.Fehlermeldung).ToBeHiddenAsync();

        Testumgebung.Aktuelle.HalteWebApiAn();
        await board.Rufe(1);

        await Expect(board.Fehlermeldung).ToBeVisibleAsync();
        await Expect(board.Fehlermeldung).ToContainTextAsync(Ausfallmeldung);
        await Expect(board.Kopfdaten).ToBeHiddenAsync();
        await Expect(board.Ausnahmeanzeige).ToBeHiddenAsync();
    }

    [Test]
    public async Task Wenn_die_Adresse_statt_einer_Nummer_Buchstaben_traegt_dann_erscheint_die_Nicht_gefunden_Seite()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await Page.GotoAsync($"{Testumgebung.Aktuelle.BlazorAdresse}/boards/abc");

        await Expect(Page.GetByText("Not Found")).ToBeVisibleAsync();
        await Expect(board.Kopfdaten).ToBeHiddenAsync();
        await Expect(board.MeldungUnbekanntesBoard).ToBeHiddenAsync();
        await Expect(board.Ausnahmeanzeige).ToBeHiddenAsync();
    }
}
