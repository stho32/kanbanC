using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
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

    [Test]
    [Category("US-1")]
    public async Task Wenn_zwei_Menschen_angelegt_sind_dann_zeigt_das_Popover_je_eine_waehlbare_Zeile_mit_Kuerzel_und_Namen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();

        await rahmen.OeffneIdentitaetswahl();

        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToHaveCountAsync(2);
        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToContainTextAsync(["NB", "ST"]);
        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToContainTextAsync(["Nina Barth", "Stefan"]);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_eine_waehlbare_Zeile_angeklickt_wird_dann_schliesst_das_Popover_und_der_Platz_traegt_den_Namen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var stefan = await agent.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync(NichtGewaehlt);
        await rahmen.OeffneIdentitaetswahl();

        await rahmen.IdentitaetWaehlbareZeile(stefan.KontributorId).ClickAsync();

        await Expect(rahmen.Identitaetspopover).ToHaveCountAsync(0);
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("Stefan");
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_das_Popover_nach_der_Wahl_erneut_geoeffnet_wird_dann_traegt_genau_die_gewaehlte_Zeile_den_Haken()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var stefan = await agent.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var nina = await agent.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();
        await Expect(rahmen.IdentitaetsHaken).ToHaveCountAsync(0);
        await rahmen.IdentitaetWaehlbareZeile(stefan.KontributorId).ClickAsync();

        await rahmen.OeffneIdentitaetswahl();

        await Expect(rahmen.IdentitaetsHaken).ToHaveCountAsync(1);
        await Expect(rahmen.IdentitaetWaehlbareZeile(stefan.KontributorId).Locator(".identitaetshaken")).ToHaveCountAsync(1);
        await Expect(rahmen.IdentitaetWaehlbareZeile(nina.KontributorId).Locator(".identitaetshaken")).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_zweiter_Mensch_gewaehlt_wird_dann_ersetzt_er_den_ersten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var stefan = await agent.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var nina = await agent.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(stefan.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("Stefan");

        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(nina.KontributorId).ClickAsync();

        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("Nina Barth");
        await rahmen.OeffneIdentitaetswahl();
        await Expect(rahmen.IdentitaetsHaken).ToHaveCountAsync(1);
        await Expect(rahmen.IdentitaetWaehlbareZeile(nina.KontributorId).Locator(".identitaetshaken")).ToHaveCountAsync(1);
    }

    // Angenommen im stillen Lauf und in R00013 festgehalten: gibt es keinen wählbaren Menschen,
    // zeigt das Popover keine leere Fläche, sondern die Fußzeile als den Weg, der weiterhilft.
    [Test]
    [Category("US-6")]
    public async Task Wenn_es_keinen_Menschen_gibt_dann_zeigt_das_Popover_keine_Zeile_sondern_die_Fusszeile()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Claude-Agent", Kontributorart.Agent);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();

        await rahmen.OeffneIdentitaetswahl();

        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToHaveCountAsync(0);
        await Expect(rahmen.IdentitaetFusszeile).ToBeVisibleAsync();
        await Expect(rahmen.IdentitaetFusszeile).ToContainTextAsync("Kontributor anlegen");
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_die_Fusszeile_angeklickt_wird_dann_steht_man_auf_der_Kontributorenseite()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        var kontributoren = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();

        await rahmen.IdentitaetFusszeile.ClickAsync();

        await Expect(kontributoren.Liste).ToBeVisibleAsync();
        await Expect(rahmen.Seitentitel).ToHaveTextAsync("Kontributoren");
        await Expect(rahmen.Identitaetspopover).ToHaveCountAsync(0);
    }
}
