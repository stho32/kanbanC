using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Probe der Frage, die vor dem Titelverweis offen war: KindZiehbarkeitProbeE2ETests hat sie fuer
// einen <button> beantwortet, ein <a> bringt zusaetzlich das native Ziehen des Verweises mit, das
// der Browser von sich aus anbietet. Geprueft wird deshalb beides — ob ein Klick auf den Titel
// einen Zug beginnt und ob das native Linkziehen den Kartenzug verdraengt. Bleibt als
// Regressionsschutz stehen.
[TestFixture]
public class VerweisInZiehbarerKarteProbeE2ETests : PageTest
{
    [Test]
    public async Task PROBE_Wenn_der_Titelverweis_nur_angeklickt_wird_dann_beginnt_kein_Zug_und_die_Kartenseite_oeffnet_sich()
    {
        var seite = await BoardMitEinerKarte();
        var karte = seite.KarteMitTitel("A");

        await seite.TitelverweisDerKarte(karte).ClickAsync();

        var detail = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await Expect(detail.Ueberschrift).ToHaveTextAsync("A");
        await Expect(seite.GezogeneKarten).ToHaveCountAsync(0);
    }

    // Die Gegenprobe zum Klick: wird am Titel gezogen, darf das native Linkziehen den Kartenzug
    // nicht verdraengen — sonst waere ein Teil jeder Karte nicht mehr zum Ziehen zu gebrauchen.
    [Test]
    public async Task PROBE_Wenn_am_Titelverweis_gezogen_wird_dann_zieht_Chromium_weiterhin_die_ganze_Karte()
    {
        var seite = await BoardMitEinerKarte();
        var karte = seite.KarteMitTitel("A");

        await seite.ZieheAmTitelverweis(karte);

        await Expect(seite.GezogeneKarten).ToHaveCountAsync(1);
        await seite.LasseAusserhalbJederStelleLos();
        await Expect(seite.GezogeneKarten).ToHaveCountAsync(0);
    }

    private async Task<BoardSeite> BoardMitEinerKarte()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "A");

        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(board.BoardId);
        await Expect(seite.Karten).ToHaveCountAsync(1);
        return seite;
    }
}
