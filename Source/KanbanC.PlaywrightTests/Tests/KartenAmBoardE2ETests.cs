using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KartenAmBoardE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Board_mit_Karten_geoeffnet_wird_dann_stehen_sie_in_ihren_Bahnen_in_der_gelieferten_Reihenfolge()
    {
        var seite = await BoardMitDreiBahnen();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Migration schreiben");
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Endpunkt bauen");
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Bahn fuellen");
        await webApi.LegeKarteAn(1, spalten[1].SpalteId, "Kartenform zeichnen");

        await seite.Oeffne(1);

        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(0)))
            .ToHaveTextAsync(["Migration schreiben", "Endpunkt bauen", "Bahn fuellen"]);
        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(1))).ToHaveTextAsync(["Kartenform zeichnen"]);
        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(2))).ToHaveCountAsync(0);
        await Expect(seite.Karten).ToHaveCountAsync(4);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_eine_Bahn_keine_Karte_traegt_dann_zeigt_sie_einen_Hinweis_statt_einer_unbeschrifteten_Flaeche()
    {
        var seite = await BoardMitDreiBahnen();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Migration schreiben");

        await seite.Oeffne(1);

        await Expect(seite.LeerhinweiseDerBahnen).ToHaveCountAsync(2);
        await Expect(seite.LeerhinweisDerBahn(seite.SpaltenbahnAnStelle(1))).ToContainTextAsync("Noch keine Karte");
        await Expect(seite.LeerhinweisDerBahn(seite.SpaltenbahnAnStelle(0))).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Seite_neu_geladen_wird_dann_stehen_die_Karten_unveraendert_in_derselben_Reihenfolge()
    {
        var seite = await BoardMitDreiBahnen();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Migration schreiben");
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Endpunkt bauen");
        await seite.Oeffne(1);
        await Expect(seite.Karten).ToHaveCountAsync(2);

        await seite.LadeNeu();

        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(0)))
            .ToHaveTextAsync(["Migration schreiben", "Endpunkt bauen"]);
        await Expect(seite.LeerhinweiseDerBahnen).ToHaveCountAsync(2);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Layout_Modus_betreten_wird_dann_stehen_die_Karten_weiter_in_ihren_Bahnen()
    {
        var seite = await BoardMitDreiBahnen();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Migration schreiben");
        await seite.Oeffne(1);
        await Expect(seite.Karten).ToHaveCountAsync(1);

        await seite.BetreteLayoutModus();

        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(0))).ToHaveTextAsync(["Migration schreiben"]);
        await Expect(seite.Bahnbearbeitungen).ToHaveCountAsync(3);
        await Expect(seite.LeerhinweiseDerBahnen).ToHaveCountAsync(2);
    }

    private async Task<BoardSeite> BoardMitDreiBahnen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();
        return new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
    }
}
