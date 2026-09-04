using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KartenzahlImBahnenkopfE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Kartenzahl_aus_ist_dann_bleibt_die_Stelle_in_jedem_Bahnenkopf_leer()
    {
        var seite = await BoardMitDreiBahnenUndVierKarten();

        await Expect(seite.Kartenzahlstellen).ToHaveCountAsync(3);
        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["", "", ""]);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Kartenzahl_eingeschaltet_wird_dann_traegt_jede_Bahn_die_Zahl_ihrer_Karten()
    {
        var seite = await BoardMitDreiBahnenUndVierKarten();

        await seite.SchalteKartenzahl(true);

        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["3", "1", "0"]);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Kartenzahl_wieder_ausgeschaltet_wird_dann_sind_die_Stellen_wieder_leer()
    {
        var seite = await BoardMitDreiBahnenUndVierKarten();
        await seite.SchalteKartenzahl(true);
        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["3", "1", "0"]);

        await seite.SchalteKartenzahl(false);

        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["", "", ""]);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Seite_mit_eingeschalteter_Kartenzahl_neu_geladen_wird_dann_stehen_die_Zahlen_wieder_da()
    {
        var seite = await BoardMitDreiBahnenUndVierKarten();
        await seite.SchalteKartenzahl(true);
        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["3", "1", "0"]);

        await seite.LadeNeu();

        await seite.ErwarteGeoeffnet();
        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["3", "1", "0"]);
    }

    private async Task<BoardSeite> BoardMitDreiBahnenUndVierKarten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var spalten = (await webApi.LadeBoard(1)).Spalten;
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Migration schreiben");
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Endpunkt bauen");
        await webApi.LegeKarteAn(1, spalten[0].SpalteId, "Bahn fuellen");
        await webApi.LegeKarteAn(1, spalten[1].SpalteId, "Kartenform zeichnen");

        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        await Expect(seite.Karten).ToHaveCountAsync(4);
        return seite;
    }
}
