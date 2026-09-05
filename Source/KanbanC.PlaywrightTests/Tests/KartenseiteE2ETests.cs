using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Die Seite selbst: Adresse, Kopfzeile, Brotkrumen und die beiden Wege, auf denen sie nichts
// zeigen kann. Die Wege vom Board her prueft KartendetailOeffnenE2ETests.
[TestFixture]
public class KartenseiteE2ETests : PageTest
{
    private const string Ausfallmeldung = "Die WebApi ist nicht erreichbar.";

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Kartenadresse_direkt_aufgerufen_wird_dann_stehen_Rueckpfeil_Boardname_Plakette_Brotkrumen_und_Titel_da()
    {
        var karteId = await BoardMitEinerKarte();
        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Oeffne(karteId);

        await Expect(seite.Ueberschrift).ToHaveTextAsync("Migration schreiben");
        await Expect(seite.Boardname).ToHaveTextAsync("Entwicklung");
        await Expect(seite.Plakette).ToHaveTextAsync($"Karte {karteId}");
        await Expect(seite.Spalte).ToContainTextAsync("Zu erledigen");
        await Expect(seite.Brotkrumen).ToContainTextAsync("Boards");
        await Expect(seite.Rueckpfeil).ToHaveAttributeAsync("href", "/boards/1");
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Kartennummer_unbekannt_ist_dann_nennt_die_Seite_sie_und_bietet_einen_Rueckweg_statt_einer_Ausnahmeseite()
    {
        await BoardMitEinerKarte();
        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await seite.Rufe(9999);

        await Expect(seite.MeldungUnbekannteKarte).ToBeVisibleAsync();
        await Expect(seite.MeldungUnbekannteKarte).ToContainTextAsync("Eine Karte mit der Nummer 9999 gibt es nicht.");
        await Expect(seite.Ueberschrift).ToHaveCountAsync(0);
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
        await Expect(seite.VerweisZurListe).ToBeVisibleAsync();
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_die_WebApi_beim_Oeffnen_der_Karte_fehlt_dann_erscheint_die_Ausfallmeldung_statt_einer_Ausnahmeseite()
    {
        var karteId = await BoardMitEinerKarte();
        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.Rufe(karteId);

        await Expect(seite.Fehlermeldung).ToBeVisibleAsync();
        await Expect(seite.Fehlermeldung).ToContainTextAsync(Ausfallmeldung);
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
    }

    private static async Task<long> BoardMitEinerKarte()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        return karte.KarteId;
    }
}
