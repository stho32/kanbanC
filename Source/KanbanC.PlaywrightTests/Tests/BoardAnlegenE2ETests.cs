using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class BoardAnlegenE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Linienboard_Entwicklung_angelegt_wird_dann_steht_es_mit_Nummer_1_und_Art_Linie_in_der_Liste()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.HinweisKeineBoards).ToBeVisibleAsync();

        await seite.FuelleFormular("Entwicklung", "Linie", null, null);
        await seite.SendeFormularAb();

        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        await Expect(seite.Boardzeile(1)).ToContainTextAsync("Entwicklung");
        await Expect(seite.Boardzeile(1)).ToContainTextAsync("Linie");

        await seite.Oeffne();
        await Expect(seite.Boardzeile(1)).ToContainTextAsync("Entwicklung");
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Projektboard_mit_Terminen_angelegt_wird_dann_zeigt_der_Abruf_beide_Termine_unveraendert()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.FuelleFormular("KanbanC 1.0", "Projekt", "2026-09-01", "2026-12-31");
        await seite.SendeFormularAb();

        await Expect(seite.Boardzeile(1)).ToContainTextAsync("KanbanC 1.0");
        await Expect(seite.Boardzeile(1)).ToContainTextAsync("Projekt");
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.OeffneBoard(1);
        await board.ErwarteGeoeffnet();
        await Expect(board.Starttermin).ToHaveTextAsync("2026-09-01");
        await Expect(board.Zieltermin).ToHaveTextAsync("2026-12-31");
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_ein_neues_Board_abgerufen_wird_dann_hat_es_die_drei_Standardspalten_mit_Erledigt_als_Abschlussspalte()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular("Entwicklung", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeile(1)).ToBeVisibleAsync();

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.OeffneBoard(1);
        await board.ErwarteGeoeffnet();

        await Expect(board.Spaltenbahnen).ToHaveCountAsync(3);
        await Expect(board.Spaltenbezeichnungen.Nth(0)).ToHaveTextAsync("Zu erledigen");
        await Expect(board.Spaltenbezeichnungen.Nth(1)).ToHaveTextAsync("In Arbeit");
        await Expect(board.Spaltenbahnen.Nth(2)).ToContainTextAsync("Erledigt");
        await Expect(board.Abschlussvermerke).ToHaveCountAsync(1);
        await Expect(board.Abschlussvermerke).ToHaveTextAsync(["Grenze 20"]);
    }
}
