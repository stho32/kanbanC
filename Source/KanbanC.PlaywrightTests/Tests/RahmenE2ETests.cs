using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
[Category("US-3")]
public class RahmenE2ETests : PageTest
{
    [Test]
    public async Task Wenn_eine_Seite_offen_ist_dann_steht_oben_waagerecht_die_Marke_mit_den_drei_Navigationspunkten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();

        await Expect(rahmen.Marke).ToHaveTextAsync("KanbanC");
        await Expect(rahmen.Navigationspunkte).ToHaveTextAsync(["Boards", "Auswertungen", "Kontributoren"]);

        var kopfzeile = await rahmen.Kopfzeile.BoundingBoxAsync();
        var marke = await rahmen.Marke.BoundingBoxAsync();
        var identitaet = await rahmen.Identitaetsplatz.BoundingBoxAsync();
        Assert.That(kopfzeile, Is.Not.Null);
        Assert.That(marke, Is.Not.Null);
        Assert.That(identitaet, Is.Not.Null);
        Assert.That(marke!.Y, Is.EqualTo(identitaet!.Y).Within(kopfzeile!.Height),
            "Marke und Identitaetsplatz stehen nicht auf derselben waagerechten Zeile.");
        Assert.That(marke.X, Is.LessThan(identitaet.X));
    }

    [Test]
    public async Task Wenn_die_Board_Uebersicht_offen_ist_dann_ist_der_Punkt_Boards_als_aktiv_erkennbar()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);

        await liste.Oeffne();

        await Expect(rahmen.PunktBoards).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("navigationspunkt-aktiv"));
    }

    [Test]
    public async Task Wenn_eine_Seite_offen_ist_dann_stehen_Auswertungen_und_Kontributoren_sichtbar_aber_ohne_Weg_da()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);

        await liste.Oeffne();

        await Expect(rahmen.PunktAuswertungen).ToBeVisibleAsync();
        await Expect(rahmen.PunktKontributoren).ToBeVisibleAsync();
        await Expect(rahmen.PunktAuswertungen).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(rahmen.PunktKontributoren).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(rahmen.NavigationsVerweise).ToHaveCountAsync(1);
    }

    [Test]
    public async Task Wenn_eine_Seite_offen_ist_dann_steht_rechts_der_Identitaetsplatz_mit_nicht_gewaehlt()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);

        await liste.Oeffne();

        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("nicht gewählt");
    }

    [Test]
    public async Task Wenn_eine_Seite_offen_ist_dann_gibt_es_keine_Seitenleiste_mehr()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);

        await liste.Oeffne();

        await Expect(rahmen.Seitenleiste).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Wenn_ein_Board_offen_ist_dann_traegt_auch_diese_Seite_dieselbe_Kopfzeile()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(1);

        await Expect(rahmen.Marke).ToHaveTextAsync("KanbanC");
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("nicht gewählt");
        await Expect(rahmen.Seitenleiste).ToHaveCountAsync(0);
    }
}
