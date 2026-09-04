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

    [Test]
    [Category("US-1")]
    public async Task Wenn_eine_Zeile_aufgeklappt_wird_dann_stehen_Name_und_Art_des_Kontributors_vorbelegt_und_das_Feld_ist_so_breit_wie_das_der_Anlegezeile()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var bert = await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        var breiteDerAnlegezeile = await seite.Namensfeld.BoundingBoxAsync();

        await seite.OeffneBearbeitung(bert.KontributorId);

        await Expect(seite.BearbeitungsNamensfeld).ToHaveValueAsync("Bert");
        await Expect(seite.Bearbeitungsartwahl).ToHaveTextAsync(["Mensch", "Agent", "abgebildet"]);
        await Expect(Page.Locator("#bearbeiten-art-agent input")).ToBeCheckedAsync();
        var breiteDerBearbeitungszeile = await seite.BearbeitungsNamensfeld.BoundingBoxAsync();
        Assert.That(breiteDerBearbeitungszeile!.Width, Is.EqualTo(breiteDerAnlegezeile!.Width));
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_Name_und_Art_geaendert_und_gesichert_werden_dann_schliesst_die_Zeile_und_der_neue_Stand_steht_an_seiner_alphabetischen_Stelle()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        var bert = await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cara", Kontributorart.Abgebildet);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.OeffneBearbeitung(bert.KontributorId);

        await seite.TrageBearbeitungsnamenEin("Zora");
        await seite.WaehleBearbeitungsart("mensch");
        await seite.Sichere();

        await Expect(seite.Bearbeitungszeile).ToHaveCountAsync(0);
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Anna", "Cara", "Zora"]);
        await Expect(seite.Artplaketten).ToHaveTextAsync(["Mensch", "abgebildet", "Mensch"]);
        await Expect(seite.Kontributorzeile(bert.KontributorId)).ToContainTextAsync("Zora");
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_verworfen_wird_dann_schliesst_die_Zeile_und_der_Kontributor_steht_unveraendert_da()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var bert = await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.OeffneBearbeitung(bert.KontributorId);

        await seite.TrageBearbeitungsnamenEin("Unsinn");
        await seite.WaehleBearbeitungsart("abgebildet");
        await seite.Verwirf();

        await Expect(seite.Bearbeitungszeile).ToHaveCountAsync(0);
        await Expect(seite.Kontributorzeile(bert.KontributorId)).ToContainTextAsync("Bert");
        await Expect(seite.Artplaketten).ToHaveTextAsync(["Agent"]);
        var kontributoren = await agent.LadeAlleKontributoren();
        Assert.That(kontributoren, Is.EqualTo(new[] { new Kontributor(bert.KontributorId, "Bert", Kontributorart.Agent) }));
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_nach_dem_Verwerfen_erneut_aufgeklappt_wird_dann_steht_wieder_der_gespeicherte_Stand_im_Formular()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var bert = await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.OeffneBearbeitung(bert.KontributorId);
        await seite.TrageBearbeitungsnamenEin("Unsinn");
        await seite.WaehleBearbeitungsart("abgebildet");
        await seite.Verwirf();

        await seite.OeffneBearbeitung(bert.KontributorId);

        await Expect(seite.BearbeitungsNamensfeld).ToHaveValueAsync("Bert");
        await Expect(Page.Locator("#bearbeiten-art-agent input")).ToBeCheckedAsync();
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_zwischen_zwei_Zeilen_gewechselt_wird_dann_zeigt_das_Formular_den_Stand_der_neu_geoeffneten_Zeile()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var anna = await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        var bert = await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.OeffneBearbeitung(bert.KontributorId);
        await seite.TrageBearbeitungsnamenEin("Unsinn");

        await seite.OeffneBearbeitung(anna.KontributorId);

        await Expect(seite.BearbeitungsNamensfeld).ToHaveValueAsync("Anna");
        await Expect(Page.Locator("#bearbeiten-art-mensch input")).ToBeCheckedAsync();
    }
}
