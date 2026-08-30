using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class GestaltungsfundamentE2ETests : PageTest
{
    private const string Grundton = "rgb(245, 234, 216)";
    private const string Ueberschriftenschrift = "Caprasimo";
    private const string Fliesstextschrift = "Figtree";

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Board_Uebersicht_geoeffnet_wird_dann_traegt_die_Seite_den_warmen_Grundton_statt_Weiss()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        var hintergrund = await Page.EvaluateAsync<string>("getComputedStyle(document.body).backgroundColor");
        Assert.That(hintergrund, Is.EqualTo(Grundton));
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Board_Uebersicht_geoeffnet_wird_dann_steht_die_Ueberschrift_in_Caprasimo_und_der_Fliesstext_in_Figtree()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await ErwarteGeladeneSchriften();
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Board_Uebersicht_geoeffnet_wird_dann_geht_keine_Anfrage_an_einen_fremden_Host()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var angefragteAdressen = new List<string>();
        Page.Request += (_, anfrage) => angefragteAdressen.Add(anfrage.Url);

        await seite.Oeffne();
        await ErwarteGeladeneSchriften();

        var fremdeAdressen = angefragteAdressen
            .Where(adresse => !adresse.StartsWith(Testumgebung.Aktuelle.BlazorAdresse, StringComparison.Ordinal))
            .ToList();
        Assert.That(fremdeAdressen, Is.Empty);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Board_Uebersicht_geoeffnet_wird_dann_ist_kein_Bootstrap_Stylesheet_geladen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        var stylesheets = await Page.EvaluateAsync<string[]>(
            "Array.from(document.styleSheets).map(sheet => sheet.href ?? 'eingebettet')");
        Assert.That(stylesheets, Is.Not.Empty);
        Assert.That(stylesheets, Has.None.Contains("bootstrap"));
        // Der Ladeplan fingerprintet die Dateinamen: aus gestaltung.css wird gestaltung.<hash>.css.
        Assert.That(stylesheets, Has.Some.Contains("/gestaltung."));
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_jede_Anfrage_an_fremde_Hosts_blockiert_wird_dann_steht_die_Ueberschrift_weiterhin_in_Caprasimo()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        await BlockiereFremdeHosts();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await ErwarteGeladeneSchriften();
    }

    private async Task BlockiereFremdeHosts()
    {
        await Page.RouteAsync("**/*", async route =>
        {
            var adresseGehoertZurAnwendung = route.Request.Url.StartsWith(
                Testumgebung.Aktuelle.BlazorAdresse, StringComparison.Ordinal);
            if (adresseGehoertZurAnwendung)
            {
                await route.ContinueAsync();
                return;
            }

            await route.AbortAsync();
        });
    }

    private async Task ErwarteGeladeneSchriften()
    {
        var ueberschriftenfamilie = await Page.EvaluateAsync<string>(
            "getComputedStyle(document.querySelector('h1')).fontFamily");
        var fliesstextfamilie = await Page.EvaluateAsync<string>(
            "getComputedStyle(document.body).fontFamily");
        Assert.That(ueberschriftenfamilie, Does.Contain(Ueberschriftenschrift));
        Assert.That(fliesstextfamilie, Does.Contain(Fliesstextschrift));

        var schriftenSindGeladen = await Page.EvaluateAsync<bool>(
            """
            async () => {
                await document.fonts.ready;
                return document.fonts.check('400 42px Caprasimo')
                    && document.fonts.check('400 15px Figtree');
            }
            """);
        Assert.That(schriftenSindGeladen, Is.True, "Die mitgelieferten Schriften wurden nicht geladen.");
    }
}
