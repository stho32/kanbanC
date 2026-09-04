using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
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

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Seite_nach_dem_Anlegen_neu_geladen_wird_dann_stehen_beide_Kontributoren_weiterhin_da()
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

        await seite.Oeffne();

        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(2);
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Codex-Agent", "Stefan"]);
        await Expect(seite.Artplaketten).ToHaveTextAsync(["Agent", "Mensch"]);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Agent_sich_ueber_die_API_selbst_anlegt_dann_sieht_der_Mensch_ihn_in_der_danach_geoeffneten_Liste()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(0);

        var angelegter = await agent.LegeKontributorAn("Codex-Agent", Kontributorart.Agent);

        await seite.Oeffne();
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(1);
        await Expect(seite.Kontributorzeile(angelegter.KontributorId)).ToContainTextAsync("Codex-Agent");
        await Expect(seite.Artplaketten).ToHaveTextAsync(["Agent"]);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_der_Mensch_in_der_Oberflaeche_anlegt_dann_liefert_der_Abruf_des_Agenten_denselben_Kontributor()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.TrageNamenEin("Nina Barth");
        await seite.WaehleArt("abgebildet");
        await seite.LegeAn();
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(1);

        var kontributoren = await agent.LadeAlleKontributoren();
        Assert.That(kontributoren, Is.EqualTo(new[] { new Kontributor(1, "Nina Barth", Kontributorart.Abgebildet) }));
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_alle_drei_Arten_in_der_Oberflaeche_angelegt_sind_dann_ueberstehen_sie_einen_Neustart_der_WebApi_unveraendert()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.TrageNamenEin("stefan");
        await seite.LegeAn();
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(1);
        await seite.TrageNamenEin("Codex-Agent");
        await seite.WaehleArt("agent");
        await seite.LegeAn();
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(2);
        await seite.TrageNamenEin("Nina Barth");
        await seite.WaehleArt("abgebildet");
        await seite.LegeAn();
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Codex-Agent", "Nina Barth", "stefan"]);

        await Testumgebung.Aktuelle.StarteWebApiNeu();

        await seite.Oeffne();
        await Expect(seite.Kontributorzeilen).ToContainTextAsync(["Codex-Agent", "Nina Barth", "stefan"]);
        await Expect(seite.Artplaketten).ToHaveTextAsync(["Agent", "abgebildet", "Mensch"]);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_die_WebApi_beim_Anlegen_nicht_erreichbar_ist_dann_erscheint_die_Ausfallmeldung_und_die_Liste_bleibt_stehen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.TrageNamenEin("Stefan");
        await seite.LegeAn();
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(1);

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.TrageNamenEin("Codex-Agent");
        await seite.LegeAn();

        await Expect(seite.Fehlermeldung).ToBeVisibleAsync();
        await Expect(seite.Fehlermeldung).ToContainTextAsync("Die WebApi ist nicht erreichbar.");
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(1);
        await Expect(seite.Anlegezeile).ToBeVisibleAsync();
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_ohne_Namen_angelegt_wird_dann_erscheint_der_Satz_und_die_Liste_bekommt_keinen_Eintrag_dazu()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.TrageNamenEin("Stefan");
        await seite.LegeAn();
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(1);

        await seite.WaehleArt("agent");
        await seite.LegeAn();

        await Expect(seite.Zurueckweisung).ToBeVisibleAsync();
        await Expect(seite.Zurueckweisung).ToContainTextAsync("Ohne Namen entsteht kein Kontributor.");
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(1);
        await Expect(seite.Anlegezeile).ToBeVisibleAsync();
        await Expect(Page.Locator("#art-agent input")).ToBeCheckedAsync();
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_nach_der_Zurueckweisung_ein_Name_eingetragen_wird_dann_entsteht_der_Kontributor_und_die_Meldung_verschwindet()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.LegeAn();
        await Expect(seite.Zurueckweisung).ToBeVisibleAsync();

        await seite.TrageNamenEin("Stefan");
        await seite.LegeAn();

        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(1);
        await Expect(seite.Kontributorzeile(1)).ToContainTextAsync("Stefan");
        await Expect(seite.Zurueckweisung).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_der_Name_nur_aus_Leerzeichen_besteht_dann_kommt_dieselbe_Meldung()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.TrageNamenEin("   ");
        await seite.LegeAn();

        await Expect(seite.Zurueckweisung).ToContainTextAsync("Ohne Namen entsteht kein Kontributor.");
        await Expect(seite.Kontributorzeilen).ToHaveCountAsync(0);
    }
}
