using KanbanC.Contracts.Klassen;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Der Rundlauf über die Oberfläche: die Klasse wählen vergibt die nächste Nummer, die Plakette
// erscheint auf der Kartenseite und auf der Karte in der Bahn und überlebt den Reload, der
// Wechsel vergibt eine neue Nummer, „ohne Klasse" nimmt sie weg — und die verfallene Nummer
// kommt nie wieder. Die drei Schritte am selben Board sind die sichtbare Probe auf die
// Identitätszusage; der nebenläufige Fall ist über den Browser nicht herstellbar und liegt
// deshalb bei den Integrationstests.
[TestFixture]
public class KarteEinerKlasseZuordnenE2ETests : PageTest
{
    [Test]
    [Category("US-1")]
    public async Task Wenn_auf_der_Kartenseite_eine_Klasse_gewaehlt_wird_dann_traegt_die_Karte_ihre_Nummer_bis_in_die_Bahn()
    {
        var aufbau = await BoardMitKarteUndKlassen();
        var kartenseite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await kartenseite.Oeffne(aufbau.ErsteKarteId);

        await Expect(kartenseite.Klassenfeld).ToHaveValueAsync(string.Empty);
        await Expect(kartenseite.Klassenwahlmoeglichkeiten).ToHaveTextAsync(["ohne Klasse", "WBS- WBS", "BUG- Bugmeldungen"]);
        await Expect(kartenseite.Klassenhinweis).ToContainTextAsync("Vergibt beim Speichern WBS-01");

        await kartenseite.WaehleKlasse(aufbau.Wbs.KartenklasseId);

        await Expect(kartenseite.Kartennummer).ToHaveTextAsync("WBS-01");

        var boardseite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await boardseite.Oeffne(aufbau.BoardId);

        await Expect(boardseite.Kartennummern).ToHaveTextAsync(["WBS-01"]);
        await Expect(boardseite.Kartentitel.Nth(0)).ToHaveAttributeAsync("href", $"/karten/{aufbau.ErsteKarteId}");

        await boardseite.BetreteLayoutModus();

        await Expect(boardseite.Klassenstaende.Nth(0)).ToContainTextAsync("1 vergeben · nächste WBS-02");

        await kartenseite.Oeffne(aufbau.ErsteKarteId);
        await kartenseite.LadeNeu();

        await Expect(kartenseite.Kartennummer).ToHaveTextAsync("WBS-01");
        await Expect(kartenseite.Klassenfeld).ToHaveValueAsync(aufbau.Wbs.KartenklasseId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    // US-2: der Wechsel vergibt eine neue Nummer aus der neuen Klasse; der Zählerstand der alten
    // bleibt stehen, und die nächste Karte dort bekommt die **nächste** Nummer.
    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Klasse_gewechselt_wird_dann_kommt_die_neue_Nummer_und_die_alte_faellt_nicht_an_eine_andere_Karte()
    {
        var aufbau = await BoardMitKarteUndKlassen();
        var kartenseite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await kartenseite.Oeffne(aufbau.ErsteKarteId);
        await kartenseite.WaehleKlasse(aufbau.Wbs.KartenklasseId);
        await Expect(kartenseite.Kartennummer).ToHaveTextAsync("WBS-01");

        await kartenseite.WaehleKlasse(aufbau.Bug.KartenklasseId);

        await Expect(kartenseite.Kartennummer).ToHaveTextAsync("BUG-01");

        await kartenseite.Oeffne(aufbau.ZweiteKarteId);
        await kartenseite.WaehleKlasse(aufbau.Wbs.KartenklasseId);

        await Expect(kartenseite.Kartennummer).ToHaveTextAsync("WBS-02");

        var boardseite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await boardseite.Oeffne(aufbau.BoardId);

        await Expect(boardseite.Kartennummern).ToHaveTextAsync(["BUG-01", "WBS-02"]);
    }

    // US-3: „ohne Klasse" nimmt die Plakette weg, und die verfallene Nummer kommt nie wieder.
    [Test]
    [Category("US-3")]
    public async Task Wenn_ohne_Klasse_gewaehlt_wird_dann_verschwindet_die_Plakette_und_die_naechste_Wahl_bekommt_die_naechste_Nummer()
    {
        var aufbau = await BoardMitKarteUndKlassen();
        var kartenseite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await kartenseite.Oeffne(aufbau.ErsteKarteId);
        await kartenseite.WaehleKlasse(aufbau.Wbs.KartenklasseId);
        await Expect(kartenseite.Kartennummer).ToHaveTextAsync("WBS-01");

        await kartenseite.WaehleOhneKlasse();

        await Expect(kartenseite.Kartennummer).ToHaveCountAsync(0);
        await Expect(kartenseite.Klassenhinweis).ToContainTextAsync("Vergibt beim Speichern WBS-02");

        var boardseite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await boardseite.Oeffne(aufbau.BoardId);

        await Expect(boardseite.Kartennummern).ToHaveCountAsync(0);
        await Expect(boardseite.Kartentitel).ToHaveCountAsync(2);

        await kartenseite.Oeffne(aufbau.ErsteKarteId);
        await kartenseite.WaehleKlasse(aufbau.Wbs.KartenklasseId);

        await Expect(kartenseite.Kartennummer).ToHaveTextAsync("WBS-02");
    }

    // US-4: ein Board ohne Klasse sagt es — ein Satz statt eines leeren Auswahlfeldes; nach dem
    // Anlegen steht das Feld da.
    [Test]
    [Category("US-4")]
    public async Task Wenn_das_Board_keine_Klasse_hat_dann_steht_an_der_Stelle_ein_Satz_und_nach_dem_Anlegen_das_Auswahlfeld()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Klassenfilter über die API");
        var kartenseite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await kartenseite.Oeffne(karte.KarteId);

        await Expect(kartenseite.HinweisKeineKlassen).ToHaveTextAsync("Dieses Board hat keine Klasse.");
        await Expect(kartenseite.Klassenfeld).ToHaveCountAsync(0);

        var boardseite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await boardseite.OeffneImLayoutModus(board.BoardId);
        await boardseite.FuelleNeueKlasse("WBS", "WBS-");
        await boardseite.LegeKlasseAn();
        await Expect(boardseite.Klassenzeilen).ToHaveCountAsync(1);

        await kartenseite.Oeffne(karte.KarteId);

        await Expect(kartenseite.HinweisKeineKlassen).ToHaveCountAsync(0);
        await Expect(kartenseite.Klassenwahlmoeglichkeiten).ToHaveTextAsync(["ohne Klasse", "WBS- WBS"]);
    }

    private static async Task<Aufbau> BoardMitKarteUndKlassen()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var spalteId = board.Spalten[0].SpalteId;
        var erste = await webApi.LegeKarteAn(board.BoardId, spalteId, "Klassenfilter über die API");
        var zweite = await webApi.LegeKarteAn(board.BoardId, spalteId, "Nummernkreis prüfen");
        var wbs = await webApi.LegeKartenklasseAn(board.BoardId, "WBS", "WBS-");
        var bug = await webApi.LegeKartenklasseAn(board.BoardId, "Bugmeldungen", "BUG-");
        return new Aufbau(board.BoardId, erste.KarteId, zweite.KarteId, wbs, bug);
    }

    private sealed record Aufbau(long BoardId, long ErsteKarteId, long ZweiteKarteId, Kartenklasse Wbs, Kartenklasse Bug);
}
