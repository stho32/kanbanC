using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KarteVerschiebenE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_eine_Karte_in_die_naechste_Bahn_gezogen_wird_dann_steht_sie_dort_oben_und_die_Herkunftsbahn_rueckt_auf()
    {
        var seite = await BoardMitKarten(
            ["Migration schreiben", "Endpunkt bauen", "Bahn fuellen"],
            ["Kartenform zeichnen", "Bahn testen"]);
        var rueckstand = seite.SpaltenbahnAnStelle(0);
        var inArbeit = seite.SpaltenbahnAnStelle(1);
        await Expect(seite.Karten).ToHaveCountAsync(5);

        await seite.ZieheKarteAuf(seite.KarteMitTitel("Endpunkt bauen"), seite.AblagestelleDerBahn(inArbeit, 0));

        await Expect(seite.KartentitelDerBahn(inArbeit))
            .ToHaveTextAsync(["Endpunkt bauen", "Kartenform zeichnen", "Bahn testen"]);
        await Expect(seite.KartentitelDerBahn(rueckstand)).ToHaveTextAsync(["Migration schreiben", "Bahn fuellen"]);
        await Expect(seite.Karten).ToHaveCountAsync(5);
        await Expect(seite.Ablagestellen).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_letzte_Karte_einer_Bahn_zwischen_die_erste_und_die_zweite_gezogen_wird_dann_steht_sie_dort()
    {
        var seite = await BoardMitKarten(["A", "B", "C", "D"], []);
        var bahn = seite.SpaltenbahnAnStelle(0);

        await seite.ZieheKarteAuf(seite.KarteMitTitel("D"), seite.AblagestelleDerBahn(bahn, 1));

        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["A", "D", "B", "C"]);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_der_Zug_ausserhalb_jeder_Ablagestelle_endet_dann_bleibt_die_Bahn_unveraendert_und_die_Stellen_verschwinden()
    {
        var seite = await BoardMitKarten(["A", "B", "C", "D"], []);
        var bahn = seite.SpaltenbahnAnStelle(0);

        await seite.NimmKarteAuf(seite.KarteMitTitel("D"));
        await Expect(bahn.Locator(".ablagestelle")).ToHaveCountAsync(5);
        await seite.LasseAusserhalbJederStelleLos();

        await Expect(seite.Ablagestellen).ToHaveCountAsync(0);
        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["A", "B", "C", "D"]);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Zug_laeuft_dann_traegt_jede_Bahn_eine_Ablagestelle_mehr_als_Karten()
    {
        var seite = await BoardMitKarten(["A", "B"], ["X"]);

        await seite.NimmKarteAuf(seite.KarteMitTitel("A"));

        await Expect(seite.AblagestelleDerBahn(seite.SpaltenbahnAnStelle(0), 0)).ToBeVisibleAsync();
        await Expect(seite.SpaltenbahnAnStelle(0).Locator(".ablagestelle")).ToHaveCountAsync(3);
        await Expect(seite.SpaltenbahnAnStelle(1).Locator(".ablagestelle")).ToHaveCountAsync(2);
        await Expect(seite.SpaltenbahnAnStelle(2).Locator(".ablagestelle")).ToHaveCountAsync(1);
        await seite.LasseAusserhalbJederStelleLos();
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Seite_nach_zwei_Zuegen_neu_geladen_wird_dann_stehen_die_Karten_an_ihren_neuen_Stellen()
    {
        var seite = await BoardMitKarten(["Migration schreiben", "Endpunkt bauen", "Bahn fuellen"], []);
        var rueckstand = seite.SpaltenbahnAnStelle(0);
        var inArbeit = seite.SpaltenbahnAnStelle(1);
        await seite.ZieheKarteAuf(seite.KarteMitTitel("Endpunkt bauen"), seite.AblagestelleDerBahn(inArbeit, 0));
        await Expect(seite.KartentitelDerBahn(inArbeit)).ToHaveTextAsync(["Endpunkt bauen"]);
        await seite.ZieheKarteAuf(seite.KarteMitTitel("Bahn fuellen"), seite.AblagestelleDerBahn(inArbeit, 0));
        await Expect(seite.KartentitelDerBahn(inArbeit)).ToHaveTextAsync(["Bahn fuellen", "Endpunkt bauen"]);

        await seite.LadeNeu();

        await Expect(seite.KartentitelDerBahn(rueckstand)).ToHaveTextAsync(["Migration schreiben"]);
        await Expect(seite.KartentitelDerBahn(inArbeit)).ToHaveTextAsync(["Bahn fuellen", "Endpunkt bauen"]);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_WebApi_nach_einem_Zug_neu_startet_dann_ist_die_Lage_beim_naechsten_Oeffnen_dieselbe()
    {
        var seite = await BoardMitKarten(["Migration schreiben", "Endpunkt bauen"], []);
        var inArbeit = seite.SpaltenbahnAnStelle(1);
        await seite.ZieheKarteAuf(seite.KarteMitTitel("Endpunkt bauen"), seite.AblagestelleDerBahn(inArbeit, 0));
        await Expect(seite.KartentitelDerBahn(inArbeit)).ToHaveTextAsync(["Endpunkt bauen"]);

        await Testumgebung.Aktuelle.StarteWebApiNeu();
        await seite.Oeffne(1);

        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(0))).ToHaveTextAsync(["Migration schreiben"]);
        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(1))).ToHaveTextAsync(["Endpunkt bauen"]);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_ein_Agent_eine_Karte_ueber_die_API_verschiebt_dann_sieht_der_Mensch_sie_dort_wieder()
    {
        var seite = await BoardMitKarten(["Migration schreiben", "Endpunkt bauen"], ["Kartenform zeichnen"]);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        var endpunktBauen = spalten[0].Karten.Single(karte => karte.Titel == "Endpunkt bauen");

        var nachDemZug = await webApi.VerschiebeKarte(1, endpunktBauen.KarteId, new Kartenlage(spalten[1].SpalteId, 1));

        Assert.That(nachDemZug[1].Karten.Select(karte => karte.Titel),
            Is.EqualTo(new[] { "Endpunkt bauen", "Kartenform zeichnen" }));
        await seite.Oeffne(1);
        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(0))).ToHaveTextAsync(["Migration schreiben"]);
        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(1)))
            .ToHaveTextAsync(["Endpunkt bauen", "Kartenform zeichnen"]);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Layout_Modus_aktiv_ist_dann_sind_die_Karten_nicht_ziehbar()
    {
        var seite = await BoardMitKarten(["A", "B"], []);
        await Expect(seite.ZiehbareKarten).ToHaveCountAsync(2);

        await seite.BetreteLayoutModus();

        await Expect(seite.Karten).ToHaveCountAsync(2);
        await Expect(seite.ZiehbareKarten).ToHaveCountAsync(0);
    }

    private async Task<BoardSeite> BoardMitKarten(IReadOnlyList<string> rueckstand, IReadOnlyList<string> inArbeit)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        await LegeKartenAn(webApi, spalten[0], rueckstand);
        await LegeKartenAn(webApi, spalten[1], inArbeit);

        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        return seite;
    }

    private static async Task LegeKartenAn(WebApiKlient webApi, Spalte spalte, IReadOnlyList<string> titel)
    {
        foreach (var einTitel in titel)
        {
            await webApi.LegeKarteAn(1, spalte.SpalteId, einTitel);
        }
    }
}
