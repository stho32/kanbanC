using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KontributorAnlegenE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Seite_geoeffnet_wird_dann_steht_am_Ende_der_Liste_die_Anlegezeile_mit_vorgewaehltem_Mensch()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne();

        await Expect(seite.Anlegezeile).ToBeVisibleAsync();
        await Expect(seite.Namensfeld).ToHaveValueAsync("");
        await Expect(seite.Artwahl).ToHaveTextAsync(["Mensch", "Agent", "abgebildet"]);
        await Expect(Page.Locator("#art-mensch input")).ToBeCheckedAsync();
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_Stefan_als_Mensch_angelegt_wird_dann_steht_er_in_der_Liste_und_die_Anlegezeile_ist_wieder_leer()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(0);

        await seite.TrageNamenEin("Stefan");
        await seite.LegeAn();

        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(1);
        await Expect(seite.Kontributorzeile(1)).ToContainTextAsync("Stefan");
        await Expect(seite.Artplaketten).ToHaveTextAsync(["Mensch"]);
        await Expect(seite.Namensfeld).ToHaveValueAsync("");
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_danach_ein_Agent_angelegt_wird_dann_stehen_beide_mit_ihrer_eigenen_Art_in_der_Liste()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.TrageNamenEin("Stefan");
        await seite.LegeAn();
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(1);

        await seite.TrageNamenEin("Codex-Agent");
        await seite.WaehleArt("agent");
        await seite.LegeAn();

        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(2);
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Codex-Agent", "Stefan"]);
        await Expect(seite.Artplaketten).ToHaveTextAsync(["Agent", "Mensch"]);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_eine_abgebildete_Person_angelegt_wird_dann_traegt_ihre_Zeile_die_neutrale_Plakette()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.TrageNamenEin("Nina Barth");
        await seite.WaehleArt("abgebildet");
        await seite.LegeAn();

        await Expect(seite.Kontributorzeile(1)).ToContainTextAsync("Nina Barth");
        await Expect(seite.Artplaketten).ToHaveTextAsync(["abgebildet"]);
        await Expect(seite.Artplaketten.First).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("tag-neutral"));
    }
}
