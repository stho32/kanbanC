using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KarteArchivierenE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_Archivieren_angeklickt_wird_dann_ist_die_Karte_ohne_Neuladen_aus_ihrer_Bahn_fort()
    {
        var seite = await BoardMitDreiKarten();

        await seite.ArchiviereKarte(seite.KarteMitTitel("B"));

        await Expect(seite.Kartentitel).ToHaveTextAsync(["A", "C"]);
        await Expect(seite.Karten).ToHaveCountAsync(2);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_eine_Karte_archiviert_wird_dann_zeigt_der_Bahnenkopf_eine_Karte_weniger()
    {
        var seite = await BoardMitDreiKarten();
        await seite.SchalteKartenzahl(true);
        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["3", "0", "0"]);

        await seite.ArchiviereKarte(seite.KarteMitTitel("B"));

        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["2", "0", "0"]);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_nach_dem_Archivieren_neu_geladen_wird_dann_ist_die_Karte_weiterhin_fort()
    {
        var seite = await BoardMitDreiKarten();
        await seite.ArchiviereKarte(seite.KarteMitTitel("B"));
        await Expect(seite.Kartentitel).ToHaveTextAsync(["A", "C"]);

        await seite.LadeNeu();

        await seite.ErwarteGeoeffnet();
        await Expect(seite.Kartentitel).ToHaveTextAsync(["A", "C"]);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Karte_archiviert_ist_dann_ist_auch_ihr_Menue_fort_und_die_uebrigen_bleiben_bedienbar()
    {
        var seite = await BoardMitDreiKarten();

        await seite.ArchiviereKarte(seite.KarteMitTitel("B"));

        await Expect(seite.Kartenmenueschalter).ToHaveCountAsync(2);
        await Expect(seite.Kartenmenuelisten).ToHaveCountAsync(0);
        await seite.OeffneKartenmenue(seite.KarteMitTitel("C"));
        await Expect(seite.MenuepunkteDerKarte(seite.KarteMitTitel("C"))).ToHaveTextAsync(["Archivieren"]);
    }

    private async Task<BoardSeite> BoardMitDreiKarten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "A");
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "B");
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "C");

        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        await Expect(seite.Karten).ToHaveCountAsync(3);
        return seite;
    }
}
