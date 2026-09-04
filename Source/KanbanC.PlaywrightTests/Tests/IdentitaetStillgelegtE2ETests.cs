using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class IdentitaetStillgelegtE2ETests : PageTest
{
    private const string NichtGewaehlt = "nicht gewählt";
    private const string Speicherschluessel = Browserspeicher.Identitaetsschluessel;

    [Test]
    [Category("US-3")]
    public async Task Wenn_der_Gewaehlte_stillgelegt_wird_dann_steht_am_Identitaetsplatz_wieder_nicht_gewaehlt()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        var dora = await agent.LegeKontributorAn("Dora", Kontributorart.Mensch);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(dora.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToContainTextAsync("Dora");

        await agent.SetzeStilllegung(dora.KontributorId, true);
        await liste.Oeffne();

        await Expect(rahmen.Identitaetsplatz).ToContainTextAsync(NichtGewaehlt);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_zwei_von_vieren_stillgelegt_sind_dann_bleibt_eine_waehlbare_und_eine_gesperrte_Zeile_im_Popover()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        var bert = await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        await agent.LegeKontributorAn("Cem", Kontributorart.Abgebildet);
        var dora = await agent.LegeKontributorAn("Dora", Kontributorart.Mensch);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();
        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToHaveCountAsync(2);
        await Expect(rahmen.IdentitaetGesperrteZeilen).ToHaveCountAsync(2);

        await agent.SetzeStilllegung(dora.KontributorId, true);
        await agent.SetzeStilllegung(bert.KontributorId, true);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();

        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToHaveCountAsync(1);
        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToContainTextAsync("Anna");
        await Expect(rahmen.IdentitaetGesperrteZeilen).ToHaveCountAsync(1);
        await Expect(rahmen.IdentitaetGesperrteZeilen).ToContainTextAsync("Cem");
        await Expect(rahmen.IdentitaetWaehlbareZeile(dora.KontributorId)).ToHaveCountAsync(0);
        await Expect(rahmen.IdentitaetGesperrteZeile(dora.KontributorId)).ToHaveCountAsync(0);
        await Expect(rahmen.IdentitaetGesperrteZeile(bert.KontributorId)).ToHaveCountAsync(0);
    }

    // Der Serverzustand fasst den Browserzustand nicht an: die gemerkte Id bleibt liegen und
    // trägt wieder, sobald zurückgeholt wird — ohne dass jemand erneut wählt.
    [Test]
    [Category("US-3")]
    public async Task Wenn_der_Stillgelegte_zurueckgeholt_wird_dann_traegt_der_Identitaetsplatz_ohne_erneute_Wahl_wieder_seinen_Namen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        var dora = await agent.LegeKontributorAn("Dora", Kontributorart.Mensch);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(dora.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToContainTextAsync("Dora");
        await agent.SetzeStilllegung(dora.KontributorId, true);
        await liste.Oeffne();
        await Expect(rahmen.Identitaetsplatz).ToContainTextAsync(NichtGewaehlt);

        var gemerkterWert = await Page.EvaluateAsync<string?>($"() => sessionStorage.getItem('{Speicherschluessel}')");
        await agent.SetzeStilllegung(dora.KontributorId, false);
        await liste.Oeffne();

        Assert.That(gemerkterWert, Is.EqualTo(dora.KontributorId.ToString()), "Der Serverzustand darf den Browserzustand nicht leeren.");
        await Expect(rahmen.Identitaetsplatz).ToContainTextAsync("Dora");
        await rahmen.OeffneIdentitaetswahl();
        await Expect(rahmen.IdentitaetWaehlbareZeile(dora.KontributorId)).ToHaveCountAsync(1);
    }

    // Auch die Trennlinie verschwindet, wenn kein aktiver Nicht-Mensch mehr übrig ist: das
    // Popover kennt keine leere gesperrte Gruppe.
    [Test]
    [Category("US-3")]
    public async Task Wenn_der_einzige_Agent_stillgelegt_wird_dann_faellt_die_Trennlinie_des_Popovers_weg()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var agent = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await agent.LegeKontributorAn("Anna", Kontributorart.Mensch);
        var bert = await agent.LegeKontributorAn("Bert", Kontributorart.Agent);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var rahmen = new Rahmen(Page);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();
        await Expect(rahmen.IdentitaetsTrenner).ToHaveCountAsync(1);

        await agent.SetzeStilllegung(bert.KontributorId, true);
        await liste.Oeffne();
        await rahmen.OeffneIdentitaetswahl();

        await Expect(rahmen.IdentitaetsTrenner).ToHaveCountAsync(0);
        await Expect(rahmen.IdentitaetGesperrteZeilen).ToHaveCountAsync(0);
        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToHaveCountAsync(1);
    }
}
