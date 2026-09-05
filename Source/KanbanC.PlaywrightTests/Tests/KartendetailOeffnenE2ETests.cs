using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-1 als Rundlauf: die beiden Wege vom Board auf dieselbe Adresse, Reload und Rueckweg.
// US-7 kommt hinzu, weil sie nur hier im Browser sichtbar wird: eine archivierte Karte
// verschwindet aus der Bahn, behaelt aber ihre Seite.
[TestFixture]
public class KartendetailOeffnenE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Kartentitel_angeklickt_wird_dann_steht_die_Kartenseite_offen_und_kein_Zug_hat_begonnen()
    {
        var aufbau = await BoardMitDreiKarten();
        var b = aufbau.Board.KarteMitTitel("B");

        await aufbau.Board.TitelverweisDerKarte(b).ClickAsync();

        await Expect(aufbau.Detail.Ueberschrift).ToHaveTextAsync("B");
        Assert.That(Page.Url, Is.EqualTo(aufbau.Detail.Adresse(aufbau.KarteIdVonB)));
        await Expect(aufbau.Board.GezogeneKarten).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_Details_oeffnen_im_Menue_gewaehlt_wird_dann_fuehrt_es_auf_dieselbe_Adresse_wie_der_Titel()
    {
        var aufbau = await BoardMitDreiKarten();
        var b = aufbau.Board.KarteMitTitel("B");
        await aufbau.Board.OeffneKartenmenue(b);

        await Expect(aufbau.Board.MenuepunkteDerKarte(b)).ToHaveTextAsync(["Details öffnen", "Archivieren"]);
        await aufbau.Board.DetailpunktDerKarte(b).ClickAsync();

        await Expect(aufbau.Detail.Ueberschrift).ToHaveTextAsync("B");
        Assert.That(Page.Url, Is.EqualTo(aufbau.Detail.Adresse(aufbau.KarteIdVonB)));
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Kartenseite_neu_geladen_wird_dann_zeigt_sie_dieselbe_Karte()
    {
        var aufbau = await BoardMitDreiKarten();
        await aufbau.Detail.Oeffne(aufbau.KarteIdVonB);

        await aufbau.Detail.LadeNeu();

        await Expect(aufbau.Detail.Ueberschrift).ToHaveTextAsync("B");
        Assert.That(Page.Url, Is.EqualTo(aufbau.Detail.Adresse(aufbau.KarteIdVonB)));
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Rueckpfeil_angeklickt_wird_dann_steht_wieder_das_Board_der_Karte_offen()
    {
        var aufbau = await BoardMitDreiKarten();
        await aufbau.Detail.Oeffne(aufbau.KarteIdVonB);

        await aufbau.Detail.Rueckpfeil.ClickAsync();

        await aufbau.Board.ErwarteGeoeffnet();
        await Expect(aufbau.Board.Kartentitel).ToHaveTextAsync(["A", "B", "C"]);
        Assert.That(Page.Url, Is.EqualTo(aufbau.Board.Adresse(1)));
    }

    // US-7: die Zusage aus I0014 im Browser — vom Board fort, unter ihrer Adresse da.
    [Test]
    [Category("US-7")]
    public async Task Wenn_die_Karte_archiviert_wurde_dann_zeigt_ihre_Adresse_sie_weiterhin()
    {
        var aufbau = await BoardMitDreiKarten();
        await aufbau.Board.ArchiviereKarte(aufbau.Board.KarteMitTitel("B"));
        await Expect(aufbau.Board.Kartentitel).ToHaveTextAsync(["A", "C"]);

        await aufbau.Detail.Rufe(aufbau.KarteIdVonB);

        await Expect(aufbau.Detail.Ueberschrift).ToHaveTextAsync("B");
        await Expect(aufbau.Detail.MeldungUnbekannteKarte).ToHaveCountAsync(0);
    }

    private async Task<Aufbau> BoardMitDreiKarten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var spalteId = board.Spalten[1].SpalteId;
        await webApi.LegeKarteAn(board.BoardId, spalteId, "A");
        var b = await webApi.LegeKarteAn(board.BoardId, spalteId, "B");
        await webApi.LegeKarteAn(board.BoardId, spalteId, "C");

        var boardSeite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await boardSeite.Oeffne(board.BoardId);
        await Expect(boardSeite.Karten).ToHaveCountAsync(3);
        return new Aufbau(boardSeite, new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse), b.KarteId);
    }

    private sealed record Aufbau(BoardSeite Board, KartendetailSeite Detail, long KarteIdVonB);
}
