using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class BoardkachelMenueE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_auf_das_Menuezeichen_geklickt_wird_dann_oeffnet_das_Menue_und_ein_zweiter_Klick_schliesst_es_wieder()
    {
        var seite = await UebersichtMitEinemBoard("KanbanC — Release 1");
        await Expect(seite.Menueliste(1)).ToHaveCountAsync(0);

        await seite.SchalteMenue(1);

        await Expect(seite.Menueliste(1)).ToBeVisibleAsync();
        await Expect(seite.Menuepunkt(1, "umbenennen")).ToHaveTextAsync("Umbenennen");

        await seite.SchalteMenue(1);

        await Expect(seite.Menueliste(1)).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Menuepunkt_angeklickt_wird_dann_bleibt_die_Uebersicht_stehen_waehrend_die_uebrige_Kachel_weiterhin_ins_Board_fuehrt()
    {
        var seite = await UebersichtMitEinemBoard("KanbanC — Release 1");
        await seite.OeffneMenue(1);

        await seite.Menuepunkt(1, "umbenennen").ClickAsync();

        // Erst abwarten, dass der Menuepunkt gewirkt hat — sonst waere die Adresspruefung schon
        // gruen, bevor eine faelschlich ausgeloeste Navigation ueberhaupt stattgefunden haette.
        await Expect(seite.Namenseingabe(1)).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync($"{Testumgebung.Aktuelle.BlazorAdresse}/boards");

        await seite.BrichUmbenennenAb(1);
        await seite.OeffneBoard(1);

        await Expect(Page).ToHaveURLAsync($"{Testumgebung.Aktuelle.BlazorAdresse}/boards/1");
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_zwei_Boards_in_der_Liste_stehen_dann_oeffnet_das_Menuezeichen_nur_das_Menue_seiner_eigenen_Kachel()
    {
        var seite = await UebersichtMitEinemBoard("Entwicklung");
        await seite.FuelleFormular("Vertrieb", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);

        await seite.OeffneMenue(1);

        await Expect(seite.Menueliste(1)).ToBeVisibleAsync();
        await Expect(seite.Menueliste(2)).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Board_vor_einer_Kachel_mit_offenem_Namensfeld_verschwindet_dann_bleibt_das_Feld_bei_seiner_eigenen_Kachel()
    {
        var seite = await UebersichtMitEinemBoard("Alpha");
        await seite.FuelleFormular("Beta", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await seite.FuelleFormular("Gamma", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(3);
        await seite.OeffneNamensfeld(2);
        await seite.Namenseingabe(2).FillAsync("Beta neu");

        await seite.OeffneMenue(1);
        await seite.Menuepunkt(1, "archivieren").ClickAsync();

        await Expect(seite.Boardzeilen).ToHaveCountAsync(2);
        await Expect(seite.Namenseingabe(2)).ToBeVisibleAsync();
        await Expect(seite.Namenseingabe(2)).ToHaveValueAsync("Beta neu");
        await Expect(seite.Namenseingabe(3)).ToHaveCountAsync(0);
        await Expect(seite.Boardverweis(3)).ToHaveTextAsync("Gamma");
    }

    private async Task<BoardsSeite> UebersichtMitEinemBoard(string name)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular(name, "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        return seite;
    }
}
