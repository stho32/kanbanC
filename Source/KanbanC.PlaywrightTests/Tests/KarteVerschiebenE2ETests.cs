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

        await seite.ZieheKarteAuf(
            seite.KarteMitTitel("Endpunkt bauen"),
            seite.ObereHaelfte(seite.KarteMitTitel("Kartenform zeichnen")));

        await Expect(seite.KartentitelDerBahn(inArbeit))
            .ToHaveTextAsync(["Endpunkt bauen", "Kartenform zeichnen", "Bahn testen"]);
        await Expect(seite.KartentitelDerBahn(rueckstand)).ToHaveTextAsync(["Migration schreiben", "Bahn fuellen"]);
        await Expect(seite.Karten).ToHaveCountAsync(5);
        await Expect(seite.Einfuegelinien).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_letzte_Karte_einer_Bahn_zwischen_die_erste_und_die_zweite_gezogen_wird_dann_steht_sie_dort()
    {
        var seite = await BoardMitKarten(["A", "B", "C", "D"], []);
        var bahn = seite.SpaltenbahnAnStelle(0);

        await seite.ZieheKarteAuf(seite.KarteMitTitel("D"), seite.UntereHaelfte(seite.KarteMitTitel("A")));

        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["A", "D", "B", "C"]);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_der_Zug_ausserhalb_jedes_Ablageziels_endet_dann_bleibt_die_Bahn_unveraendert_und_die_Linie_verschwindet()
    {
        var seite = await BoardMitKarten(["A", "B", "C", "D"], []);
        var bahn = seite.SpaltenbahnAnStelle(0);

        await seite.NimmKarteAuf(seite.KarteMitTitel("D"));
        await seite.LegeAufStelleAb(seite.ObereHaelfte(seite.KarteMitTitel("B")));
        await Expect(seite.Einfuegelinien).ToHaveCountAsync(0);
        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["A", "D", "B", "C"]);

        await seite.NimmKarteAuf(seite.KarteMitTitel("D"));
        await seite.FahreUeberZone(seite.ObereHaelfte(seite.KarteMitTitel("A")));
        await Expect(seite.Einfuegelinien).ToHaveCountAsync(1);
        await seite.LasseAusserhalbJederStelleLos();

        await Expect(seite.Einfuegelinien).ToHaveCountAsync(0);
        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["A", "D", "B", "C"]);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Zug_laeuft_dann_nimmt_jede_Bahn_an_auch_die_leere()
    {
        var seite = await BoardMitKarten(["A", "B"], ["X"]);
        var rueckstand = seite.SpaltenbahnAnStelle(0);
        var inArbeit = seite.SpaltenbahnAnStelle(1);
        var erledigt = seite.SpaltenbahnAnStelle(2);

        await seite.NimmKarteAuf(seite.KarteMitTitel("A"));

        await Expect(seite.AblageflaecheDerBahn(rueckstand)).ToHaveCountAsync(1);
        await Expect(seite.KartenhaelftenDerBahn(rueckstand)).ToHaveCountAsync(4);
        await Expect(seite.AblageflaecheDerBahn(inArbeit)).ToHaveCountAsync(1);
        await Expect(seite.KartenhaelftenDerBahn(inArbeit)).ToHaveCountAsync(2);
        await Expect(seite.AblageflaecheDerBahn(erledigt)).ToHaveCountAsync(1);
        await Expect(seite.KartenhaelftenDerBahn(erledigt)).ToHaveCountAsync(0);
        await seite.LasseAusserhalbJederStelleLos();
        await Expect(seite.Ablageflaechen).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Seite_nach_zwei_Zuegen_neu_geladen_wird_dann_stehen_die_Karten_an_ihren_neuen_Stellen()
    {
        var seite = await BoardMitKarten(["Migration schreiben", "Endpunkt bauen", "Bahn fuellen"], []);
        var rueckstand = seite.SpaltenbahnAnStelle(0);
        var inArbeit = seite.SpaltenbahnAnStelle(1);
        await seite.ZieheKarteAufsBahnende(seite.KarteMitTitel("Endpunkt bauen"), inArbeit);
        await Expect(seite.KartentitelDerBahn(inArbeit)).ToHaveTextAsync(["Endpunkt bauen"]);
        await seite.ZieheKarteAuf(
            seite.KarteMitTitel("Bahn fuellen"),
            seite.ObereHaelfte(seite.KarteMitTitel("Endpunkt bauen")));
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
        await seite.ZieheKarteAufsBahnende(seite.KarteMitTitel("Endpunkt bauen"), inArbeit);
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


    // Der Browser zeigt einen Stand, den es nicht mehr gibt: waehrend die Seite offen ist, raeumt
    // ein Agent die Zielbahn ueber die API leer. Die untere Haelfte der zweiten Karte zielt danach
    // auf eine Position, die es nach dem Zug nicht mehr gaebe.
    [Test]
    [Category("US-5")]
    public async Task Wenn_die_Zielbahn_inzwischen_leerer_ist_dann_erscheint_eine_lesbare_Zurueckweisung_und_die_Karte_kehrt_zurueck()
    {
        var seite = await BoardMitKarten(["A", "B", "C", "D"], ["X", "Y"]);
        var rueckstand = seite.SpaltenbahnAnStelle(0);
        var inArbeit = seite.SpaltenbahnAnStelle(1);
        await Expect(seite.KartentitelDerBahn(inArbeit)).ToHaveTextAsync(["X", "Y"]);
        await RaeumeZweiteBahnLeer();

        await seite.ZieheKarteAuf(seite.KarteMitTitel("D"), seite.UntereHaelfte(seite.KarteMitTitel("Y")));

        await Expect(seite.KarteZurueckweisung).ToBeVisibleAsync();
        await Expect(seite.KarteZurueckweisung).ToContainTextAsync("liegt außerhalb der Zielspalte");
        await Expect(seite.KartentitelDerBahn(rueckstand)).ToHaveTextAsync(["A", "B", "C", "D"]);
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_die_WebApi_beim_Ablegen_nicht_erreichbar_ist_dann_erscheint_die_Ausfallmeldung_und_das_Board_bleibt_stehen()
    {
        var seite = await BoardMitKarten(["A", "B"], []);
        var rueckstand = seite.SpaltenbahnAnStelle(0);
        var inArbeit = seite.SpaltenbahnAnStelle(1);

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.ZieheKarteAufsBahnende(seite.KarteMitTitel("B"), inArbeit);

        await Expect(seite.KarteFehlermeldung).ToBeVisibleAsync();
        await Expect(seite.KarteFehlermeldung).ToContainTextAsync("Die WebApi ist nicht erreichbar.");
        await Expect(seite.KartentitelDerBahn(rueckstand)).ToHaveTextAsync(["A", "B"]);
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_nach_einer_Zurueckweisung_die_WebApi_ausfaellt_dann_steht_die_alte_Zurueckweisung_nicht_mehr_daneben()
    {
        var seite = await BoardMitKarten(["A", "B", "C", "D"], ["X", "Y"]);
        var inArbeit = seite.SpaltenbahnAnStelle(1);
        await RaeumeZweiteBahnLeer();
        await seite.ZieheKarteAuf(seite.KarteMitTitel("D"), seite.UntereHaelfte(seite.KarteMitTitel("Y")));
        await Expect(seite.KarteZurueckweisung).ToBeVisibleAsync();

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.ZieheKarteAufsBahnende(seite.KarteMitTitel("C"), inArbeit);

        // Die Ausfallmeldung sagt, dass der Zug nicht ankam. Bliebe die Zurückweisung des vorigen
        // Zugs daneben stehen, nennte sie einen Grund, der für diesen Zug nie geprüft wurde.
        await Expect(seite.KarteFehlermeldung).ToBeVisibleAsync();
        await Expect(seite.KarteZurueckweisung).ToBeHiddenAsync();
    }

    private static async Task RaeumeZweiteBahnLeer()
    {
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        var erledigt = spalten[2].SpalteId;
        foreach (var karte in spalten[1].Karten)
        {
            await webApi.VerschiebeKarte(1, karte.KarteId, new Kartenlage(erledigt, 1));
        }
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
