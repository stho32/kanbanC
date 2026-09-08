using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// **Ein** Lauf über beide Prozesse: ein Board mit Karten, einer archivierten Karte und einem
// Zeiteintrag aufbauen, `/auswertungen` öffnen, `Rohdaten über die API` wählen, die zwei Pfade
// lesen — und **beide Adressen neben dem Browser wirklich abrufen**. Das ist der Beweis, dass der
// gezeigte Pfad der ist, der antwortet; ein Fuß mit einem falschen Pfad wäre schlimmer als keiner.
// Gewartet wird auf Zustände, nie auf Zeit.
[TestFixture]
public class RohdatenUeberDieApiE2ETests : PageTest
{
    // US-6: der Punkt ist wählbar, und die Fläche nennt die zwei Aufrufe mit der Board-Nummer.
    [Test]
    [Category("US-6")]
    public async Task Wenn_Rohdaten_gewaehlt_wird_dann_nennt_die_Flaeche_die_zwei_Pfade_des_gewaehlten_Boards()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await LegeBestandAn(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleRohdaten();

        await seite.WaehleBoard(aufbau.BoardId);

        await Expect(seite.Rohdatenkartenaufruf).ToContainTextAsync($"GET /api/boards/{aufbau.BoardId}/karten");
        await Expect(seite.Rohdatenzeitenaufruf).ToContainTextAsync($"GET /api/boards/{aufbau.BoardId}/zeiten");
    }

    // US-6, der Kern: der Fuß führt erstmals **zwei** Aufrufe, und beide antworten wirklich.
    [Test]
    [Category("US-6")]
    public async Task Wenn_die_zwei_gezeigten_Adressen_neben_dem_Browser_gerufen_werden_dann_antworten_genau_sie()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await LegeBestandAn(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleRohdaten();
        await seite.WaehleBoard(aufbau.BoardId);
        await Expect(seite.Rohdatenflaeche).ToBeVisibleAsync();

        var zeilen = await seite.Aufrufzeilen();

        Assert.That(zeilen, Has.Count.EqualTo(2), "Der Fuß nannte nicht zwei Pfade.");
        Assert.Multiple(() =>
        {
            Assert.That(zeilen[0], Is.EqualTo($"GET /api/boards/{aufbau.BoardId}/karten"));
            Assert.That(zeilen[1], Is.EqualTo($"GET /api/boards/{aufbau.BoardId}/zeiten"));
        });

        var karten = await webApi.LadeRohdatenkarten(aufbau.BoardId);
        var zeiten = await webApi.LadeRohdatenzeiten(aufbau.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(karten, Has.Count.EqualTo(3));
            Assert.That(karten.Count(karte => karte.Archivstand.IstArchiviert), Is.EqualTo(1));
            Assert.That(zeiten, Has.Count.EqualTo(1));
            Assert.That(zeiten[0].Ende, Is.Not.Null);
        });
    }

    // US-2: die Anzeige bleibt die Anzeige. Das Board führt die archivierte Karte nicht, die
    // Rohdaten führen sie — zwei Zusagen, zwei Ressourcen.
    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Kartenzahl_der_Rohdaten_gegen_das_Board_gehalten_wird_dann_traegt_nur_sie_die_archivierte_Karte()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await LegeBestandAn(webApi);

        var rohdaten = await webApi.LadeRohdatenkarten(aufbau.BoardId);
        var board = await webApi.LadeBoard(aufbau.BoardId);

        var kartenDesBoards = board.Spalten.SelectMany(spalte => spalte.Karten).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(rohdaten, Has.Count.EqualTo(3));
            Assert.That(kartenDesBoards, Has.Count.EqualTo(2));
            Assert.That(rohdaten.Select(karte => karte.Karte.KarteId), Does.Contain(aufbau.ArchivierteId));
            Assert.That(kartenDesBoards.Select(karte => karte.KarteId), Has.None.EqualTo(aufbau.ArchivierteId));
        });
    }

    // US-6: die Kartenklassenwahl tritt für diesen Eintrag zurück — ein Bedienelement ohne Wirkung
    // wäre eine stille Lüge —, und `Puffer-Verbrauch` steht seit R00042 wählbar daneben.
    [Test]
    [Category("US-6")]
    public async Task Wenn_Rohdaten_gewaehlt_ist_dann_tritt_die_Kartenklassenwahl_zurueck_und_kommt_bei_der_naechsten_Wahl_wieder()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await LegeBestandAn(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.WaehleRohdaten();
        await seite.WaehleBoard(aufbau.BoardId);

        await Expect(seite.Kartenklassenwahl).ToHaveCountAsync(0);
        await Expect(seite.Boardwahl).ToBeVisibleAsync();
        await Expect(seite.GesperrteAuswertungen).ToHaveCountAsync(0);
        await Expect(seite.PunktPuffer).ToBeVisibleAsync();

        await seite.WaehleBurndown();

        await Expect(seite.Kartenklassenwahl).ToBeVisibleAsync();
    }

    // US-6: ohne gewähltes Board zeigt die Fläche keine halben Pfade — derselbe Zustand, den die
    // übrigen Auswertungen für „Bestand ungewählt" schon führen.
    [Test]
    [Category("US-6")]
    public async Task Wenn_kein_Board_gewaehlt_ist_dann_steht_der_Hinweis_und_keine_halben_Pfade()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();

        await seite.WaehleRohdaten();

        await Expect(seite.OhneBestandHinweis).ToContainTextAsync("Der Bestand ist hier das Board.");
        await Expect(seite.Rohdatenflaeche).ToHaveCountAsync(0);
    }

    // Der Eintrag bedient nichts: kein Knopf, kein Download — nur die zwei Aufrufe.
    [Test]
    [Category("US-6")]
    public async Task Wenn_die_Rohdatenflaeche_steht_dann_traegt_sie_keinen_Knopf_und_keinen_Verweis()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aufbau = await LegeBestandAn(webApi);
        var seite = new AuswertungenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne();
        await seite.WaehleRohdaten();
        await seite.WaehleBoard(aufbau.BoardId);
        await Expect(seite.Rohdatenflaeche).ToBeVisibleAsync();

        await Expect(seite.Rohdatenflaeche.Locator("button")).ToHaveCountAsync(0);
        await Expect(seite.Rohdatenflaeche.Locator("a")).ToHaveCountAsync(0);
    }

    // Zwei aktive Karten, eine archivierte und ein abgeschlossener Zeiteintrag: klein genug für
    // einen Lauf, groß genug für die zwei Zusagen dieses Slice.
    private static async Task<Aufbau> LegeBestandAn(WebApiKlient webApi)
    {
        var board = await webApi.LegeBoardAn("Release 2");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var spalteId = board.Spalten[0].SpalteId;
        var erste = await webApi.LegeKarteAn(board.BoardId, spalteId, "Rohdaten abrufen");
        await webApi.LegeKarteAn(board.BoardId, spalteId, "Auswertung rechnen");
        var archivierte = await webApi.LegeKarteAn(board.BoardId, spalteId, "Alte Karte");
        await webApi.SchalteKartenarchivierung(board.BoardId, archivierte.KarteId, true);
        await webApi.TrageZeitNach(
            erste.KarteId,
            stefan.KontributorId,
            new DateTimeOffset(2026, 9, 6, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 6, 10, 30, 0, TimeSpan.Zero));
        return new Aufbau(board.BoardId, erste.KarteId, archivierte.KarteId);
    }

    private sealed record Aufbau(long BoardId, long ErsteKarteId, long ArchivierteId);
}
