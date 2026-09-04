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
}
