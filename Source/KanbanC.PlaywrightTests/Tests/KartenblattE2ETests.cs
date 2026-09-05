using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-2: die vier Felder, die zusammengehören — ihr Leerzustand, ihr Ändern, ihr Stand nach einem
// Reload und die Zurückweisung des geleerten Titels.
[TestFixture]
public class KartenblattE2ETests : PageTest
{
    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Karte_frisch_angelegt_ist_dann_tragen_die_leeren_Felder_eine_Handlung_statt_einer_Null()
    {
        var seite = await FrischeKarte();

        await Expect(seite.BeschreibungHinzufuegen).ToHaveTextAsync("Beschreibung hinzufügen");
        await Expect(seite.Faelligkeit).ToHaveTextAsync("—");
        await Expect(seite.Farbpunkte).ToHaveCountAsync(5);
        await Expect(seite.GewaehlterFarbpunkt).ToHaveAttributeAsync("id", "farbpunkt-Ohne");
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_Titel_Beschreibung_Faelligkeit_und_Farbe_gesetzt_werden_dann_stehen_alle_vier_nach_einem_Reload_da()
    {
        var seite = await FrischeKarte();

        await seite.SchreibeTitel("WBS-Import");
        await seite.SchreibeBeschreibung("Knoten in Karten überführen");
        await seite.SetzeFaelligkeit("2026-09-02");
        await seite.Farbpunkt("Terrakotta").ClickAsync();
        await Expect(seite.GewaehlterFarbpunkt).ToHaveAttributeAsync("id", "farbpunkt-Terrakotta");

        await seite.LadeNeu();

        await Expect(seite.Ueberschrift).ToHaveTextAsync("WBS-Import");
        await Expect(seite.Beschreibung).ToHaveTextAsync("Knoten in Karten überführen");
        await Expect(seite.Faelligkeit).ToHaveTextAsync("2026-09-02");
        await Expect(seite.GewaehlterFarbpunkt).ToHaveAttributeAsync("id", "farbpunkt-Terrakotta");
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_der_Titel_geleert_wird_dann_erscheint_die_Zurueckweisung_und_die_vorige_Fassung_bleibt_stehen()
    {
        var seite = await FrischeKarte();
        await seite.SchreibeTitel("WBS-Import");
        await Expect(seite.Ueberschrift).ToHaveTextAsync("WBS-Import");

        await seite.SchreibeTitel("");

        await Expect(seite.BlattZurueckweisung).ToContainTextAsync("Der Titel darf nicht leer sein.");
        await Expect(seite.Ueberschrift).ToHaveTextAsync("WBS-Import");

        await seite.LadeNeu();

        await Expect(seite.Ueberschrift).ToHaveTextAsync("WBS-Import");
        await Expect(seite.BlattZurueckweisung).ToHaveCountAsync(0);
    }

    private async Task<KartendetailSeite> FrischeKarte()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        return seite;
    }
}
