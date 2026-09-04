using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KartenzahlSchalterE2ETests : PageTest
{
    private const string Ausfallmeldung = "Die WebApi ist nicht erreichbar.";
    private static readonly TimeSpan Wartefrist = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Abfrageintervall = TimeSpan.FromMilliseconds(100);

    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Board_geoeffnet_wird_dann_steht_das_Kontrollfeld_Kartenzahl_in_der_Navigationszeile_und_ist_aus()
    {
        var seite = await BoardMitStandardspalten();

        await Expect(seite.Kartenzahlschalter).ToBeVisibleAsync();
        await Expect(seite.Kartenzahlschalter).Not.ToBeCheckedAsync();
        await Expect(new Rahmen(Page).Kopfzeile.Locator("#kartenzahl-schalter")).ToHaveCountAsync(1);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_das_Kontrollfeld_eingeschaltet_wird_dann_traegt_das_Board_die_Einstellung_auch_ueber_die_API()
    {
        var seite = await BoardMitStandardspalten();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        Assert.That((await webApi.LadeBoard(1)).ZeigtKartenzahl, Is.False);

        await seite.SchalteKartenzahl(true);

        await Expect(seite.Kartenzahlschalter).ToBeCheckedAsync();
        await Erwarte(webApi, zeigtKartenzahl: true);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_das_Kontrollfeld_wieder_ausgeschaltet_wird_dann_steht_die_Einstellung_auch_in_der_API_wieder_auf_aus()
    {
        var seite = await BoardMitStandardspalten();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await seite.SchalteKartenzahl(true);
        await Erwarte(webApi, zeigtKartenzahl: true);

        await seite.SchalteKartenzahl(false);

        await Expect(seite.Kartenzahlschalter).Not.ToBeCheckedAsync();
        await Erwarte(webApi, zeigtKartenzahl: false);
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_die_WebApi_beim_Umschalten_fehlt_dann_erscheint_eine_lesbare_Meldung_und_das_Board_bleibt_bedienbar()
    {
        var seite = await BoardMitStandardspalten();
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);

        Testumgebung.Aktuelle.HalteWebApiAn();
        // Klicken statt SetChecked: die Oberfläche stellt das Häkchen nach dem gescheiterten
        // Aufruf zurück, und SetChecked prüft den Zustand, den es gerade gesetzt hat.
        await seite.Kartenzahlschalter.ClickAsync();

        await Expect(seite.KartenzahlFehlermeldung).ToBeVisibleAsync();
        await Expect(seite.KartenzahlFehlermeldung).ToContainTextAsync(Ausfallmeldung);
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
        await Expect(seite.Kartenzahlschalter).Not.ToBeCheckedAsync();
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
        await Expect(seite.LayoutBearbeiten).ToBeVisibleAsync();
    }

    // Die Oberfläche schaltet über HTTP; erst der Abruf über die API belegt, dass die Einstellung
    // wirklich am Board hängt und nicht nur im Browser steht. Der Klick und der Aufruf laufen in
    // verschiedenen Prozessen — deshalb wird abgewartet, nicht einmal gemessen.
    private static async Task Erwarte(WebApiKlient webApi, bool zeigtKartenzahl)
    {
        var frist = DateTime.UtcNow + Wartefrist;
        while (DateTime.UtcNow < frist)
        {
            var board = await webApi.LadeBoard(1);
            if (board.ZeigtKartenzahl == zeigtKartenzahl)
            {
                return;
            }

            await Task.Delay(Abfrageintervall);
        }

        Assert.Fail($"Die API meldet ZeigtKartenzahl nicht als {zeigtKartenzahl}.");
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
