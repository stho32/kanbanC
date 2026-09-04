using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class IdentitaetWaehlenE2ETests : PageTest
{
    private const string NichtGewaehlt = "nicht gewählt";

    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Identitaetsplatz_angeklickt_wird_dann_klappt_das_Popover_Ich_bin_auf()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await Expect(rahmen.Identitaetspopover).ToHaveCountAsync(0);

        await rahmen.Identitaetsplatz.ClickAsync();

        await Expect(rahmen.Identitaetspopover).ToBeVisibleAsync();
        await Expect(rahmen.Identitaetspopover).ToContainTextAsync("Ich bin");
        await Expect(rahmen.Identitaetsplatz).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Identitaetsplatz_ein_zweites_Mal_angeklickt_wird_dann_schliesst_das_Popover()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();

        await rahmen.Identitaetsplatz.ClickAsync();

        await Expect(rahmen.Identitaetspopover).ToHaveCountAsync(0);
        await Expect(rahmen.Identitaetsplatz).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_bei_offenem_Popover_Escape_gedrueckt_wird_dann_schliesst_es()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(rahmen.Identitaetspopover).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_bei_offenem_Popover_daneben_geklickt_wird_dann_schliesst_es()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();

        await Page.Mouse.ClickAsync(30, 300);

        await Expect(rahmen.Identitaetspopover).ToHaveCountAsync(0);
    }

    // Auflage an die Gestaltung aus R00013: aus dem Platz wird ein Schalter, id und Wortlaut
    // bleiben. Das Chevron ist ein SVG ohne Textinhalt, damit der Wortlaut genau greift.
    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Identitaetsplatz_zum_Schalter_wird_dann_traegt_er_weiterhin_die_id_und_den_Wortlaut()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);

        await liste.Oeffne();

        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync(NichtGewaehlt);
        await Expect(rahmen.Identitaetsplatz).ToHaveAttributeAsync("aria-haspopup", "true");
    }
}
