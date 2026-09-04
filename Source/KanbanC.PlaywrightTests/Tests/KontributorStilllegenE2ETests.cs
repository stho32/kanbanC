using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KontributorStilllegenE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_drei_Kontributoren_aktiv_sind_dann_traegt_jede_Zeile_Stift_und_Pausensymbol()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(3);
        await Expect(seite.Stifte).ToHaveCountAsync(3);
        await Expect(seite.Pausensymbole).ToHaveCountAsync(3);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_das_Pausensymbol_geklickt_wird_dann_rutscht_die_Zeile_ans_Ende_und_verliert_ihre_Pflegeschalter()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Anna", "Bert", "Cem"]);

        await seite.LegeStill(anna.KontributorId);

        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Bert", "Cem", "Anna"]);
        await Expect(seite.Pausensymbole).ToHaveCountAsync(2);
        await Expect(seite.Stifte).ToHaveCountAsync(2);
        var kontributoren = await agent.LadeAlleKontributoren();
        Assert.That(kontributoren.Single(kontributor => kontributor.Name == "Anna").StillgelegtAm, Is.Not.Null);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_die_WebApi_beim_Stilllegen_nicht_erreichbar_ist_dann_erscheint_die_Ausfallmeldung_und_die_Liste_bleibt_stehen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.LegeStill(anna.KontributorId);

        await Expect(seite.Fehlermeldung).ToContainTextAsync("Die WebApi ist nicht erreichbar.");
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Anna", "Bert", "Cem"]);

        // Der gescheiterte Klick hat nichts geändert: nach dem Neustart ist Anna unverändert aktiv.
        await Testumgebung.Aktuelle.StarteWebApiNeu();
        await seite.Oeffne();
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Anna", "Bert", "Cem"]);
        await Expect(seite.Pausensymbole).ToHaveCountAsync(3);
    }
}
