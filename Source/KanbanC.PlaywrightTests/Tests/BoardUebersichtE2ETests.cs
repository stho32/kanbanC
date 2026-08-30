using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class BoardUebersichtE2ETests : PageTest
{
    [Test]
    [Category("US-4")]
    public async Task Wenn_Linien_und_Projektboards_vorliegen_dann_stehen_sie_unter_ihren_beiden_Bandueberschriften()
    {
        var seite = await UebersichtMitDreiBoards();

        await Expect(seite.BandLinienboards).ToContainTextAsync("Linienboards — laufen ohne Ende");
        await Expect(seite.BandProjektboards).ToContainTextAsync("Projektboards — laufen mit dem Vorhaben aus");

        var linienboards = seite.KachelnImBand(seite.BandLinienboards);
        await Expect(linienboards).ToHaveCountAsync(2);
        await Expect(linienboards.Nth(0)).ToContainTextAsync("Beschaffung");
        await Expect(linienboards.Nth(1)).ToContainTextAsync("betrieb");

        var projektboards = seite.KachelnImBand(seite.BandProjektboards);
        await Expect(projektboards).ToHaveCountAsync(1);
        await Expect(projektboards.Nth(0)).ToContainTextAsync("KanbanC — Release 2");
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_drei_Linienboards_gemischter_Schreibweise_vorliegen_dann_stehen_sie_alphabetisch_in_ihrem_Band()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await LegeBoardAn(seite, "Zulauf", "Linie", null, null);
        await LegeBoardAn(seite, "beschaffung", "Linie", null, null);
        await LegeBoardAn(seite, "Betrieb", "Linie", null, null);

        await seite.Oeffne();

        var linienboards = seite.KachelnImBand(seite.BandLinienboards);
        await Expect(linienboards).ToHaveCountAsync(3);
        await Expect(linienboards.Nth(0)).ToContainTextAsync("beschaffung");
        await Expect(linienboards.Nth(1)).ToContainTextAsync("Betrieb");
        await Expect(linienboards.Nth(2)).ToContainTextAsync("Zulauf");
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_ein_Projektboard_einen_Zieltermin_hat_dann_nennt_ihn_der_Kachelfuss_und_beim_Linienboard_bleibt_die_Stelle_leer()
    {
        var seite = await UebersichtMitDreiBoards();

        await Expect(seite.Kachelfuss(3)).ToHaveTextAsync("bis 2026-09-30");
        await Expect(seite.Kachelfuss(1)).ToBeEmptyAsync();
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_ein_Band_kein_Board_traegt_dann_zeigt_es_seine_Ueberschrift_und_einen_Hinweis()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await LegeBoardAn(seite, "Beschaffung", "Linie", null, null);

        await seite.Oeffne();

        await Expect(seite.BandProjektboards).ToContainTextAsync("Projektboards — laufen mit dem Vorhaben aus");
        await Expect(seite.HinweisLeeresBand(seite.BandProjektboards)).ToBeVisibleAsync();
        await Expect(seite.HinweisLeeresBand(seite.BandLinienboards)).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_Kachel_angeklickt_wird_dann_oeffnet_sich_die_Seite_dieses_Boards()
    {
        var seite = await UebersichtMitDreiBoards();
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.OeffneBoard(3);

        await board.ErwarteGeoeffnet();
        await Expect(board.Name).ToHaveTextAsync("KanbanC — Release 2");
        Assert.That(Page.Url, Is.EqualTo($"{Testumgebung.Aktuelle.BlazorAdresse}/boards/3"));
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_die_Uebersicht_geoeffnet_wird_dann_steht_das_Anlegeformular_erst_nach_einem_Klick_da()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await Expect(seite.Anlegeformular).ToHaveCountAsync(0);

        await seite.OeffneAnlegeformular();

        await Expect(seite.Anlegeformular).ToBeVisibleAsync();
        await Expect(seite.Spaltenvorschau).ToHaveTextAsync(["Zu erledigen", "In Arbeit", "Erledigt"]);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_die_Art_Projektboard_gewaehlt_wird_dann_erscheinen_die_Terminfelder_und_bei_Linienboard_nicht()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.OeffneAnlegeformular();

        await Expect(seite.Terminfelder).ToHaveCountAsync(0);

        await seite.WaehleArt("Projekt");
        await Expect(seite.Terminfelder).ToBeVisibleAsync();

        await seite.WaehleArt("Linie");
        await Expect(seite.Terminfelder).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_ein_Board_angelegt_wird_dann_schliesst_sich_das_Formular_und_die_Kachel_steht_in_ihrem_Band()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.FuelleFormular("KanbanC — Release 2", "Projekt", "2026-01-05", "2026-09-30");
        await seite.SendeFormularAb();

        await Expect(seite.Anlegeformular).ToHaveCountAsync(0);
        await Expect(seite.KachelnImBand(seite.BandProjektboards)).ToHaveCountAsync(1);
        await Expect(seite.Kachelfuss(1)).ToHaveTextAsync("bis 2026-09-30");
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_das_Anlegen_zurueckgewiesen_wird_dann_bleibt_das_Formular_offen_und_traegt_den_Befund()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.FuelleFormular("", "Linie", null, null);
        await seite.SendeFormularAb();

        await Expect(seite.Anlegeformular).ToBeVisibleAsync();
        await Expect(seite.Zurueckweisung).ToContainTextAsync("Der Name darf nicht leer sein.");
        await Expect(seite.HinweisKeineBoards).ToBeVisibleAsync();
    }

    [Test]
    public async Task Wenn_das_Anlegen_abgebrochen_wird_dann_schliesst_das_Formular_ohne_ein_Board_anzulegen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.FuelleFormular("Verworfenes Board", "Linie", null, null);
        await seite.BrichAnlegenAb();

        await Expect(seite.Anlegeformular).ToHaveCountAsync(0);
        await Expect(seite.HinweisKeineBoards).ToBeVisibleAsync();

        await seite.OeffneAnlegeformular();
        await Expect(Page.Locator("#name")).ToHaveValueAsync("");
    }

    private async Task<BoardsSeite> UebersichtMitDreiBoards()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await LegeBoardAn(seite, "Beschaffung", "Linie", null, null);
        await LegeBoardAn(seite, "betrieb", "Linie", null, null);
        await LegeBoardAn(seite, "KanbanC — Release 2", "Projekt", "2026-01-05", "2026-09-30");
        await seite.Oeffne();
        return seite;
    }

    private async Task LegeBoardAn(BoardsSeite seite, string name, string art, string? starttermin, string? zieltermin)
    {
        await seite.FuelleFormular(name, art, starttermin, zieltermin);
        await seite.SendeFormularAb();
        await Expect(seite.Anlegeformular).ToHaveCountAsync(0);
    }
}
