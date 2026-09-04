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

    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_Karte_angelegt_wird_dann_steht_in_ihrer_Bahn_ohne_Reload_eine_um_eins_hoehere_Zahl()
    {
        var seite = await BoardMitDreiBahnenUndVierKarten();
        await seite.SchalteKartenzahl(true);
        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["3", "1", "0"]);

        var rueckstand = seite.SpaltenbahnAnStelle(0);
        await seite.OeffneKartenanlage(rueckstand);
        await seite.LegeKarteAn(rueckstand, "Zahl nachziehen");

        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["4", "1", "0"]);
        await Expect(seite.Karten).ToHaveCountAsync(5);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_Karte_in_eine_andere_Bahn_abgelegt_wird_dann_sinkt_die_Quellzahl_und_die_Zielzahl_steigt()
    {
        var seite = await BoardMitDreiBahnenUndVierKarten();
        await seite.SchalteKartenzahl(true);
        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["3", "1", "0"]);

        await seite.ZieheKarteAuf(
            seite.KarteMitTitel("Endpunkt bauen"),
            seite.ObereHaelfte(seite.KarteMitTitel("Kartenform zeichnen")));

        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["2", "2", "0"]);
        await Expect(seite.Karten).ToHaveCountAsync(4);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_Karte_innerhalb_ihrer_Bahn_verschoben_wird_dann_aendert_sich_keine_Zahl()
    {
        var seite = await BoardMitDreiBahnenUndVierKarten();
        await seite.SchalteKartenzahl(true);
        await Expect(seite.Kartenzahlstellen).ToHaveTextAsync(["3", "1", "0"]);

        await seite.ZieheKarteAuf(
            seite.KarteMitTitel("Bahn fuellen"),
            seite.ObereHaelfte(seite.KarteMitTitel("Migration schreiben")));

        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(0)))
            .ToHaveTextAsync(["Bahn fuellen", "Migration schreiben", "Endpunkt bauen"]);
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
