using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KontributorAendernE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Liste_geoeffnet_wird_dann_traegt_sie_die_Spalte_Pflege_und_je_Zeile_einen_Stift()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cara", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.KopfzellePflege).ToHaveTextAsync("Pflege");
        await Expect(seite.Stifte).ToHaveCountAsync(3);
        await Expect(seite.Bearbeitungszeile).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_eine_Zeile_aufgeklappt_wird_dann_steht_sie_an_der_Stelle_ihres_Kontributors_und_die_uebrigen_bleiben_sichtbar()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        var bert = await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cara", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(3);

        await seite.OeffneBearbeitung(bert.KontributorId);

        await Expect(seite.Bearbeitungszeile).ToHaveCountAsync(1);
        await Expect(seite.Kontributorzeile(bert.KontributorId)).ToHaveCountAsync(0);
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Anna", "Cara"]);
        await Expect(seite.Artplaketten).ToHaveTextAsync(["Mensch", "abgebildet"]);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_ein_zweiter_Stift_angeklickt_wird_dann_schliesst_die_erste_Zeile_und_es_bleibt_bei_einer_offenen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        var bert = await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.OeffneBearbeitung(bert.KontributorId);
        await Expect(seite.Kontributorzeile(anna.KontributorId)).ToHaveCountAsync(1);

        await seite.OeffneBearbeitung(anna.KontributorId);

        await Expect(seite.Bearbeitungszeile).ToHaveCountAsync(1);
        await Expect(seite.Kontributorzeile(anna.KontributorId)).ToHaveCountAsync(0);
        await Expect(seite.Kontributorzeile(bert.KontributorId)).ToContainTextAsync("Bert");
    }
}
