using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
public class KarteAnlegenE2ETests : PageTest
{
    [Test]
    [Category("US-3")]
    public async Task Wenn_am_Fuss_einer_Bahn_eine_Karte_angelegt_wird_dann_erscheint_sie_als_letzte_derselben_Bahn()
    {
        var seite = await BoardMitDreiBahnen();
        var bereit = seite.SpaltenbahnAnStelle(0);
        await seite.OeffneKartenanlage(bereit);
        await seite.LegeKarteAn(bereit, "Migration schreiben");
        await Expect(seite.KartentitelDerBahn(bereit)).ToHaveTextAsync(["Migration schreiben"]);
        await seite.OeffneKartenanlage(bereit);
        await seite.LegeKarteAn(bereit, "Endpunkt bauen");
        await Expect(seite.KartentitelDerBahn(bereit)).ToHaveCountAsync(2);

        await seite.OeffneKartenanlage(bereit);
        await seite.LegeKarteAn(bereit, "Anzeigegrenze im Spaltenkopf");

        await Expect(seite.KartentitelDerBahn(bereit))
            .ToHaveTextAsync(["Migration schreiben", "Endpunkt bauen", "Anzeigegrenze im Spaltenkopf"]);
        await Expect(seite.Karten).ToHaveCountAsync(3);
        await seite.LadeNeu();
        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(0)))
            .ToHaveTextAsync(["Migration schreiben", "Endpunkt bauen", "Anzeigegrenze im Spaltenkopf"]);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_begonnene_Anlage_abgebrochen_wird_dann_entsteht_keine_Karte()
    {
        var seite = await BoardMitDreiBahnen();
        var bahn = seite.SpaltenbahnAnStelle(0);
        await seite.OeffneKartenanlage(bahn);
        await seite.KartenanlageTitelfeld(bahn).FillAsync("Nie entstanden");

        await seite.BrichKartenanlageAb(bahn);

        await Expect(seite.KartenanlageTitelfeld(bahn)).ToBeHiddenAsync();
        await Expect(seite.Karten).ToHaveCountAsync(0);
        await seite.LadeNeu();
        await Expect(seite.Karten).ToHaveCountAsync(0);
        await Expect(seite.LeerhinweiseDerBahnen).ToHaveCountAsync(3);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Anlage_in_zwei_Bahnen_begonnen_wird_dann_entsteht_die_Karte_in_der_Bahn_deren_Fuss_bedient_wurde()
    {
        var seite = await BoardMitDreiBahnen();
        var erste = seite.SpaltenbahnAnStelle(0);
        var zweite = seite.SpaltenbahnAnStelle(1);
        await seite.OeffneKartenanlage(erste);
        await seite.KartenanlageTitelfeld(erste).FillAsync("Gehoert in die erste Bahn");

        await seite.OeffneKartenanlage(zweite);
        await seite.LegeKarteAn(zweite, "Gehoert in die zweite Bahn");

        await Expect(seite.KartentitelDerBahn(zweite)).ToHaveTextAsync(["Gehoert in die zweite Bahn"]);
        await Expect(seite.KartentitelDerBahn(erste)).ToHaveCountAsync(0);
        await Expect(seite.KartenanlageTitelfeld(erste)).ToHaveValueAsync("Gehoert in die erste Bahn");
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_ein_Agent_die_Karte_ueber_die_API_anlegt_dann_sieht_ein_Mensch_sie_danach_in_derselben_Bahn()
    {
        var seite = await BoardMitDreiBahnen();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var bahn = seite.SpaltenbahnAnStelle(1);
        await seite.OeffneKartenanlage(bahn);
        await seite.LegeKarteAn(bahn, "Vom Menschen angelegt");
        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["Vom Menschen angelegt"]);
        var spalten = (await webApi.LadeBoard(1)).Spalten;

        await webApi.LegeKarteAn(1, spalten[1].SpalteId, "Vom Agenten angelegt");

        await seite.LadeNeu();
        await Expect(seite.KartentitelDerBahn(seite.SpaltenbahnAnStelle(1)))
            .ToHaveTextAsync(["Vom Menschen angelegt", "Vom Agenten angelegt"]);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_die_Karte_ohne_Titel_angelegt_wird_dann_erscheint_eine_lesbare_Meldung_und_es_entsteht_keine_Karte()
    {
        var seite = await BoardMitDreiBahnen();
        var bahn = seite.SpaltenbahnAnStelle(0);
        await seite.OeffneKartenanlage(bahn);

        await seite.LegeKarteAn(bahn, "   ");

        await Expect(seite.KartenanlageZurueckweisung(bahn)).ToBeVisibleAsync();
        await Expect(seite.KartenanlageZurueckweisung(bahn)).ToContainTextAsync("Der Titel darf nicht leer sein.");
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
        await Expect(seite.Karten).ToHaveCountAsync(0);

        await seite.LegeKarteAn(bahn, "Doch noch angelegt");

        await Expect(seite.KartentitelDerBahn(bahn)).ToHaveTextAsync(["Doch noch angelegt"]);
        await Expect(seite.KartenanlageZurueckweisung(bahn)).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_der_Titel_ueber_1000_Zeichen_lang_ist_dann_erscheint_eine_lesbare_Meldung_an_derselben_Bahn()
    {
        var seite = await BoardMitDreiBahnen();
        var bahn = seite.SpaltenbahnAnStelle(0);
        await seite.OeffneKartenanlage(bahn);

        await seite.LegeKarteAn(bahn, new string('a', 1001));

        await Expect(seite.KartenanlageZurueckweisung(bahn)).ToContainTextAsync("1000");
        await Expect(seite.Karten).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_die_WebApi_beim_Anlegen_nicht_erreichbar_ist_dann_erscheint_eine_lesbare_Meldung_statt_einer_Ausnahmeseite()
    {
        var seite = await BoardMitDreiBahnen();
        var bahn = seite.SpaltenbahnAnStelle(0);
        await seite.OeffneKartenanlage(bahn);
        Testumgebung.Aktuelle.HalteWebApiAn();

        await seite.LegeKarteAn(bahn, "Migration schreiben");

        await Expect(seite.KartenanlageFehlermeldung(bahn)).ToBeVisibleAsync();
        await Expect(seite.KartenanlageFehlermeldung(bahn)).ToContainTextAsync("nicht erreichbar");
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
    }

    private async Task<BoardSeite> BoardMitDreiBahnen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();
        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
        return seite;
    }
}
