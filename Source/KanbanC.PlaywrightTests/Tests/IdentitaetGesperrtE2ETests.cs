using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Der gesperrte Teil der Identitätswahl: sichtbar, damit erkennbar ist, wer sonst am Board
// schreibt — und weder mit der Maus noch mit der Tastatur erreichbar, damit niemand in fremdem
// Namen arbeitet.
[TestFixture]
public class IdentitaetGesperrtE2ETests : PageTest
{
    private const string Fusszeilenkennung = "identitaet-anlegen";

    [Test]
    [Category("US-5")]
    public async Task Wenn_alle_drei_Arten_angelegt_sind_dann_steht_der_Mensch_ueber_der_Trennlinie_und_die_anderen_darunter()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var stefan = await agent.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var claude = await agent.LegeKontributorAn("Claude-Agent", Kontributorart.Agent);
        var maria = await agent.LegeKontributorAn("Maria Lenz", Kontributorart.Abgebildet);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();

        await rahmen.OeffneIdentitaetswahl();

        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToHaveCountAsync(1);
        await Expect(rahmen.IdentitaetGesperrteZeilen).ToHaveCountAsync(2);
        await Expect(rahmen.IdentitaetGesperrteZeile(claude.KontributorId)).ToContainTextAsync("Claude-Agent");
        await Expect(rahmen.IdentitaetGesperrteZeile(maria.KontributorId)).ToContainTextAsync("Maria Lenz");
        await Expect(rahmen.IdentitaetsPlaketten).ToHaveTextAsync(["nur API", "abgebildet"]);
        await Expect(rahmen.IdentitaetsTrenner).ToBeVisibleAsync();

        var mensch = await rahmen.IdentitaetWaehlbareZeile(stefan.KontributorId).BoundingBoxAsync();
        var trenner = await rahmen.IdentitaetsTrenner.BoundingBoxAsync();
        var gesperrter = await rahmen.IdentitaetGesperrteZeile(claude.KontributorId).BoundingBoxAsync();
        Assert.Multiple(() =>
        {
            Assert.That(mensch!.Y, Is.LessThan(trenner!.Y), "Der wählbare Mensch steht nicht über der Trennlinie.");
            Assert.That(trenner!.Y, Is.LessThan(gesperrter!.Y), "Der gesperrte Kontributor steht nicht unter der Trennlinie.");
        });
    }

    // Rechenbeispiel aus R00013: Stefan ist gewählt; weder der Agent noch der abgebildete
    // Kontributor ändern daran etwas.
    [Test]
    [Category("US-5")]
    public async Task Wenn_eine_gesperrte_Zeile_angeklickt_wird_dann_bleibt_der_Identitaetsplatz_stehen_und_das_Popover_offen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var stefan = await agent.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var claude = await agent.LegeKontributorAn("Claude-Agent", Kontributorart.Agent);
        var maria = await agent.LegeKontributorAn("Maria Lenz", Kontributorart.Abgebildet);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(stefan.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("Stefan");
        await rahmen.OeffneIdentitaetswahl();

        await rahmen.IdentitaetGesperrteZeile(claude.KontributorId).ClickAsync();
        await rahmen.IdentitaetGesperrteZeile(maria.KontributorId).ClickAsync();

        await Expect(rahmen.Identitaetspopover).ToBeVisibleAsync();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("Stefan");
        await Expect(rahmen.IdentitaetWaehlbareZeile(stefan.KontributorId).Locator(".identitaetshaken")).ToHaveCountAsync(1);
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_vom_letzten_waehlbaren_Eintrag_weitergetabt_wird_dann_landet_der_Fokus_auf_der_Fusszeile()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var stefan = await agent.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var claude = await agent.LegeKontributorAn("Claude-Agent", Kontributorart.Agent);
        var maria = await agent.LegeKontributorAn("Maria Lenz", Kontributorart.Abgebildet);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(stefan.KontributorId).FocusAsync();

        await Page.Keyboard.PressAsync("Tab");

        var fokussierteKennung = await Page.EvaluateAsync<string?>("() => document.activeElement.id");
        await Expect(rahmen.IdentitaetGesperrteZeile(claude.KontributorId)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(rahmen.IdentitaetGesperrteZeile(maria.KontributorId)).ToHaveAttributeAsync("aria-disabled", "true");
        Assert.That(fokussierteKennung, Is.EqualTo(Fusszeilenkennung),
            "Der Tabulator ist in einem gesperrten Eintrag gelandet statt auf der Fußzeile.");
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_auf_der_Fusszeile_die_Eingabetaste_gedrueckt_wird_dann_steht_man_auf_der_Kontributorenseite()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        var kontributoren = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetFusszeile.FocusAsync();

        await Page.Keyboard.PressAsync("Enter");

        await Expect(kontributoren.Liste).ToBeVisibleAsync();
        await Expect(rahmen.Seitentitel).ToHaveTextAsync("Kontributoren");
    }

    [Test]
    [Category("US-6")]
    public async Task Wenn_dort_ein_Mensch_angelegt_wird_dann_steht_er_beim_naechsten_Oeffnen_ueber_der_Trennlinie()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Claude-Agent", Kontributorart.Agent);
        var rahmen = new Rahmen(Page);
        var kontributoren = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await kontributoren.Oeffne();
        await rahmen.OeffneIdentitaetswahl();
        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToHaveCountAsync(0);
        await Page.Keyboard.PressAsync("Escape");

        await kontributoren.TrageNamenEin("Nina Barth");
        await kontributoren.WaehleArt("mensch");
        await kontributoren.LegeAn();
        await Expect(kontributoren.Kontributorzeilen).ToHaveCountAsync(2);

        await rahmen.OeffneIdentitaetswahl();
        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToHaveCountAsync(1);
        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToContainTextAsync(["Nina Barth"]);
        await Expect(rahmen.IdentitaetGesperrteZeilen).ToHaveCountAsync(1);
    }
}
