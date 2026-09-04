using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
[Category("US-3")]
public class RahmenE2ETests : PageTest
{
    [Test]
    public async Task Wenn_eine_Seite_offen_ist_dann_steht_oben_waagerecht_ihr_Titel_mit_den_drei_Navigationspunkten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();

        await Expect(rahmen.Seitentitel).ToHaveTextAsync("Boards");
        await Expect(rahmen.Navigationspunkte).ToHaveTextAsync(["Boards", "Auswertungen", "Kontributoren"]);

        var kopfzeile = await rahmen.Kopfzeile.BoundingBoxAsync();
        var titel = await rahmen.Seitentitel.BoundingBoxAsync();
        var identitaet = await rahmen.Identitaetsplatz.BoundingBoxAsync();
        Assert.That(kopfzeile, Is.Not.Null);
        Assert.That(titel, Is.Not.Null);
        Assert.That(identitaet, Is.Not.Null);
        Assert.That(titel!.Y, Is.EqualTo(identitaet!.Y).Within(kopfzeile!.Height),
            "Seitentitel und Identitaetsplatz stehen nicht auf derselben waagerechten Zeile.");
        Assert.That(titel.X, Is.LessThan(identitaet.X));
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
    public async Task Wenn_eine_Seite_offen_ist_dann_steht_Auswertungen_ohne_Weg_da_und_Kontributoren_mit()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);

        await liste.Oeffne();

        await Expect(rahmen.PunktAuswertungen).ToBeVisibleAsync();
        await Expect(rahmen.PunktKontributoren).ToBeVisibleAsync();
        await Expect(rahmen.PunktAuswertungen).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(rahmen.PunktKontributoren).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(rahmen.PunktKontributoren).ToHaveAttributeAsync("href", "kontributoren");
        await Expect(rahmen.NavigationsVerweise).ToHaveCountAsync(2);
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

        // Der Rahmen ist derselbe; sein Titel wechselt mit der Seite — auf einem Board
        // steht dort dessen Name, nicht mehr eine feste Wortmarke.
        await Expect(rahmen.Seitentitel).ToHaveTextAsync("Entwicklung");
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("nicht gewählt");
        await Expect(rahmen.Seitenleiste).ToHaveCountAsync(0);

        // Der Rueckweg in der Kopfzeile fuehrt schon zur Uebersicht: der gleichnamige
        // Navigationspunkt waere dieselbe Verknuepfung ein zweites Mal.
        await Expect(rahmen.Navigationspunkte).ToHaveTextAsync(["Auswertungen", "Kontributoren"]);
        await Expect(board.VerweisZurListe).ToBeVisibleAsync();
    }

    [Test]
    public async Task Wenn_von_einem_Board_zur_Uebersicht_zurueckgegangen_wird_dann_steht_der_Punkt_Boards_wieder_da()
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
        await Expect(rahmen.Navigationspunkte).ToHaveTextAsync(["Auswertungen", "Kontributoren"]);

        await board.VerweisZurListe.ClickAsync();

        await Expect(rahmen.Navigationspunkte).ToHaveTextAsync(["Boards", "Auswertungen", "Kontributoren"]);
    }
}
