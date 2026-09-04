using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KontributorenlisteE2ETests : PageTest
{
    [Test]
    [Category("US-4")]
    public async Task Wenn_der_Punkt_Kontributoren_in_der_Kopfzeile_angeklickt_wird_dann_oeffnet_sich_die_Seite_und_der_Punkt_ist_aktiv()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        var kontributoren = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await Expect(rahmen.Seitentitel).ToHaveTextAsync("Boards");

        await rahmen.PunktKontributoren.ClickAsync();

        await Expect(kontributoren.Liste).ToBeVisibleAsync();
        await Expect(rahmen.Seitentitel).ToHaveTextAsync("Kontributoren");
        await Expect(rahmen.PunktKontributoren).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("navigationspunkt-aktiv"));
        await Expect(rahmen.PunktAuswertungen).ToHaveAttributeAsync("aria-disabled", "true");
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_drei_Kontributoren_ueber_die_API_angelegt_sind_dann_stehen_sie_alphabetisch_mit_ihrer_Art_in_der_Liste()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("stefan", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Codex-Agent", Kontributorart.Agent);
        await agent.LegeKontributorAn("Nina Barth", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(3);
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Codex-Agent", "Nina Barth", "stefan"]);
        await Expect(seite.Artplaketten).ToHaveTextAsync(["Agent", "abgebildet", "Mensch"]);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Seite_ohne_angelegte_Kontributoren_geoeffnet_wird_dann_steht_die_Liste_leer_da()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(0);
    }
}
