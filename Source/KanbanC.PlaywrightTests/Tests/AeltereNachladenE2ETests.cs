using KanbanC.Contracts.Karten;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class AeltereNachladenE2ETests : PageTest
{
    private const int Anzeigegrenze = 20;

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Abschlussbahn_gekuerzt_ist_dann_stehen_Hinweis_und_Bedienelement_in_der_Bahnenflaeche()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);

        await Expect(seite.Nachladehinweise).ToHaveCountAsync(1);
        await Expect(seite.Nachladehinweise).ToContainTextAsync("20 neueste gezeigt");
        await Expect(seite.NachladeKnoepfe).ToHaveTextAsync(["Ältere nachladen"]);
        await Expect(seite.KarteAnlegenKnoepfe).ToHaveCountAsync(3);
        await Expect(seite.BahnenflaecheDerBahn(erledigt).Locator(".spaltenbahn-nachladen")).ToHaveCountAsync(1);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Bahn_nicht_gekuerzt_ist_dann_gibt_es_weder_Hinweis_noch_Bedienelement()
    {
        var seite = await BoardMitErledigtenKarten(heute: 2, gestern: 3);

        await Expect(seite.Karten).ToHaveCountAsync(5);
        await Expect(seite.Nachladehinweise).ToHaveCountAsync(0);
        await Expect(seite.NachladeKnoepfe).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_Aeltere_nachgeladen_werden_dann_stehen_alle_Karten_in_der_Bahn_und_der_Hinweis_ist_fort()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);
        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(Anzeigegrenze);

        await seite.LadeAeltereNach(erledigt);

        await Expect(seite.KartentitelDerBahn(erledigt)).ToHaveCountAsync(23);
        await Expect(seite.Nachladehinweise).ToHaveCountAsync(0);
        await Expect(seite.NachladeKnoepfe).ToHaveCountAsync(0);
        await Expect(seite.DatumsgruppenDerBahn(erledigt)).ToHaveTextAsync(["Heute · 3", "Gestern · 20"]);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_Aeltere_nachgeladen_werden_dann_zeigt_der_Kopf_die_genaue_Zahl_statt_20_plus()
    {
        var seite = await BoardMitErledigtenKarten(heute: 3, gestern: 20);
        var erledigt = seite.SpaltenbahnAnStelle(2);
        await seite.SchalteKartenzahl(true);
        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["0", "0", "20+"]);

        await seite.LadeAeltereNach(erledigt);

        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["0", "0", "23"]);
    }

    private async Task<BoardSeite> BoardMitErledigtenKarten(int heute, int gestern)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var abschlussspalteId = (await webApi.LadeBoard(1)).Spalten[2].SpalteId;
        await LegeErledigteAn(webApi, abschlussspalteId, "Heute", heute);
        var gestrige = await LegeErledigteAn(webApi, abschlussspalteId, "Gestern", gestern);
        foreach (var karte in gestrige)
        {
            Testumgebung.Aktuelle.Datenbank.SetzeErledigung(karte.KarteId, DateOnly.FromDateTime(DateTime.Today).AddDays(-1));
        }

        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
        return seite;
    }

    private static async Task<IReadOnlyList<Karte>> LegeErledigteAn(WebApiKlient webApi, long spalteId, string titelstamm, int anzahl)
    {
        var angelegte = new List<Karte>();
        for (var nummer = 1; nummer <= anzahl; nummer++)
        {
            angelegte.Add(await webApi.LegeKarteAn(1, spalteId, $"{titelstamm} {nummer}"));
        }

        return angelegte;
    }
}
