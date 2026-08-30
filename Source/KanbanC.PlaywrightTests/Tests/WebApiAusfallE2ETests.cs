using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class WebApiAusfallE2ETests : PageTest
{
    private const string Ausfallmeldung = "Die WebApi ist nicht erreichbar.";

    [Test]
    public async Task Wenn_die_WebApi_beim_Laden_der_Seite_fehlt_dann_erscheint_eine_lesbare_Meldung_statt_eines_Absturzes()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await Expect(seite.HinweisKeineBoards).ToBeVisibleAsync();

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.OeffneOhneBoardliste();

        await Expect(seite.Fehlermeldung).ToBeVisibleAsync();
        await Expect(seite.Fehlermeldung).ToContainTextAsync(Ausfallmeldung);
    }

    [Test]
    public async Task Wenn_die_WebApi_waehrend_der_Nutzung_ausfaellt_dann_erscheint_eine_lesbare_Meldung_und_die_Liste_bleibt_stehen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular("Entwicklung", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.FuelleFormular("Wartung", "Linie", null, null);
        await seite.SendeFormularAb();

        await Expect(seite.Fehlermeldung).ToBeVisibleAsync();
        await Expect(seite.Fehlermeldung).ToContainTextAsync(Ausfallmeldung);
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
    }

    [Test]
    public async Task Wenn_die_WebApi_waehrend_der_Spaltenpflege_ausfaellt_dann_erscheint_eine_lesbare_Meldung_statt_eines_Absturzes()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();
        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.OeffneImLayoutModus(1);
        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(3);

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.FuelleNeueSpalte("Wartet auf Zulieferung", false, null);
        await seite.LegeSpalteAn();

        await Expect(seite.SpaltenFehlermeldung).ToBeVisibleAsync();
        await Expect(seite.SpaltenFehlermeldung).ToContainTextAsync(Ausfallmeldung);
        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(3);
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
    }

    [Test]
    public async Task Wenn_die_WebApi_nach_einem_Ausfall_in_der_Spaltenpflege_zurueckkehrt_dann_nimmt_die_Seite_die_Bedienung_an()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();
        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.OeffneImLayoutModus(1);
        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.FuelleNeueSpalte("Wartet auf Zulieferung", false, null);
        await seite.LegeSpalteAn();
        await Expect(seite.SpaltenFehlermeldung).ToBeVisibleAsync();

        await Testumgebung.Aktuelle.StarteWebApiNeu();
        await seite.LegeSpalteAn();

        await Expect(seite.Spaltenbahnanzeigen).ToHaveCountAsync(4);
        await Expect(seite.Spaltenbahnanzeigen.Nth(3)).ToHaveTextAsync("Wartet auf Zulieferung");
        await Expect(seite.SpaltenFehlermeldung).ToBeHiddenAsync();
    }

    [Test]
    public async Task Wenn_die_WebApi_nach_einem_Ausfall_zurueckkehrt_dann_verschwindet_die_Meldung_und_die_Liste_erscheint()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.FuelleFormular("Entwicklung", "Linie", null, null);
        await seite.SendeFormularAb();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.OeffneOhneBoardliste();
        await Expect(seite.Fehlermeldung).ToBeVisibleAsync();

        await Testumgebung.Aktuelle.StarteWebApiNeu();
        await seite.Oeffne();

        await Expect(seite.Fehlermeldung).ToBeHiddenAsync();
        await Expect(seite.Boardzeilen).ToHaveCountAsync(1);
    }
}
