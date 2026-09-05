using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Probe der Frage, die vor dem ⋯-Menü auf der Karte offen war: kann ein Kind eines ziehbaren
// Elements sich vom Ziehen abmelden? Antwort für Chromium: nein. Ein Klick auf den Schalter
// beginnt keinen Zug — dafür genügt, dass ein Klick die Maus nicht bewegt. Wird am Schalter
// gezogen, zieht die ganze Karte, obwohl der Schalter darunter draggable="false" und
// -webkit-user-drag: none trüge. Deshalb steht am Schalter keins von beidem: es wäre eine
// Zusage, die der Browser nicht einlöst. Bleibt als Regressionsschutz stehen.
[TestFixture]
public class KindZiehbarkeitProbeE2ETests : PageTest
{
    [Test]
    public async Task Wenn_am_Menueschalter_einer_Karte_gezogen_wird_dann_zieht_Chromium_die_ganze_Karte()
    {
        var seite = await BoardMitEinerKarte();
        var karte = seite.KarteMitTitel("A");

        await seite.ZieheAmMenueschalter(karte);

        await Expect(seite.GezogeneKarten).ToHaveCountAsync(1);
        await seite.LasseAusserhalbJederStelleLos();
        await Expect(seite.GezogeneKarten).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Wenn_der_Menueschalter_nur_angeklickt_wird_dann_beginnt_kein_Zug()
    {
        var seite = await BoardMitEinerKarte();
        var karte = seite.KarteMitTitel("A");

        await seite.MenueschalterDerKarte(karte).ClickAsync();

        await Expect(seite.MenueDerKarte(karte)).ToBeVisibleAsync();
        await Expect(seite.GezogeneKarten).ToHaveCountAsync(0);
        await Expect(seite.Ablageflaechen).ToHaveCountAsync(0);
    }

    private async Task<BoardSeite> BoardMitEinerKarte()
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

        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        await Expect(seite.Karten).ToHaveCountAsync(1);
        return seite;
    }
}
