using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class BoardUmbenennenE2ETests : PageTest
{
    private const string Ausfallmeldung = "Die WebApi ist nicht erreichbar.";

    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Board_in_der_Kachel_umbenannt_wird_dann_traegt_die_Liste_den_neuen_Namen_und_ein_Reload_zeigt_ihn_wieder()
    {
        var seite = await UebersichtMitBoard("KanbanC — Release 1");
        await Expect(seite.Boardverweis(1)).ToHaveTextAsync("KanbanC — Release 1");

        await seite.BenenneUm(1, "KanbanC — Release 2");

        await Expect(seite.Boardverweis(1)).ToHaveTextAsync("KanbanC — Release 2");

        await seite.Oeffne();

        await Expect(seite.Boardverweis(1)).ToHaveTextAsync("KanbanC — Release 2");
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LadeBoard(1);
        Assert.That(board.Name, Is.EqualTo("KanbanC — Release 2"));
        Assert.That(board.Spalten, Has.Count.EqualTo(3));
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_das_Namensfeld_geoeffnet_wird_dann_steht_der_alte_Name_darin_und_daneben_Speichern_und_Abbrechen()
    {
        var seite = await UebersichtMitBoard("KanbanC — Release 1");

        await seite.OeffneNamensfeld(1);

        await Expect(seite.Namenseingabe(1)).ToHaveValueAsync("KanbanC — Release 1");
        await Expect(seite.Boardzeile(1).Locator(".board-kachel-speichern")).ToBeVisibleAsync();
        await Expect(seite.Boardzeile(1).Locator(".board-kachel-abbrechen")).ToBeVisibleAsync();
        await Expect(seite.Boardverweis(1)).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_der_Name_geleert_und_gespeichert_wird_dann_meldet_die_Kachel_es_das_Feld_bleibt_offen_und_der_alte_Name_steht_weiter_in_der_API()
    {
        var seite = await UebersichtMitBoard("KanbanC — Release 1");

        await seite.BenenneUm(1, "");

        await Expect(seite.Kachelmeldung(1)).ToBeVisibleAsync();
        await Expect(seite.Kachelmeldung(1)).ToContainTextAsync("Der Name darf nicht leer sein.");
        await Expect(seite.Namenseingabe(1)).ToBeVisibleAsync();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        Assert.That((await webApi.LadeBoard(1)).Name, Is.EqualTo("KanbanC — Release 1"));
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_das_Umbenennen_abgebrochen_wird_dann_steht_der_alte_Name_da_und_gespeichert_wurde_nichts()
    {
        var seite = await UebersichtMitBoard("KanbanC — Release 1");
        await seite.OeffneNamensfeld(1);
        await seite.Namenseingabe(1).FillAsync("KanbanC — Release 2");

        await seite.BrichUmbenennenAb(1);

        await Expect(seite.Boardverweis(1)).ToHaveTextAsync("KanbanC — Release 1");
        await Expect(seite.Namenseingabe(1)).ToHaveCountAsync(0);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        Assert.That((await webApi.LadeBoard(1)).Name, Is.EqualTo("KanbanC — Release 1"));
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_WebApi_beim_Speichern_des_Namens_fehlt_dann_meldet_die_Kachel_den_Ausfall_und_die_Liste_bleibt_bedienbar()
    {
        var seite = await UebersichtMitBoard("KanbanC — Release 1");
        await seite.OeffneNamensfeld(1);
        await seite.Namenseingabe(1).FillAsync("KanbanC — Release 2");
        Testumgebung.Aktuelle.HalteWebApiAn();

        await seite.SpeichereNamen(1);

        await Expect(seite.Kachelmeldung(1)).ToContainTextAsync(Ausfallmeldung);
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        await seite.BrichUmbenennenAb(1);
        await Expect(seite.Boardverweis(1)).ToHaveTextAsync("KanbanC — Release 1");
    }

    private async Task<BoardsSeite> UebersichtMitBoard(string name)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular(name, "Projekt", "2026-09-01", "2026-12-31");
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        return seite;
    }
}
