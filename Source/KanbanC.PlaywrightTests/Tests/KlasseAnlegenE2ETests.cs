using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Der Rundlauf über die Oberfläche: der Leerzustand, das Anlegen samt Plakette und nächster
// Nummer, die zwei Ränder — und der Beleg, dass die Eindeutigkeit **je Board** gilt. Der letzte
// Fall braucht deshalb zwei Boards im Aufbau: ein global eindeutiger Index bräche genau diese
// Zusage still.
[TestFixture]
public class KlasseAnlegenE2ETests : PageTest
{
    // US-3: ein Board ohne Klasse ist kein Fehler — ein Satz und die Anlegezeile, wie bei den
    // Spalten.
    [Test]
    [Category("US-3")]
    public async Task Wenn_ein_Board_ohne_Klasse_im_Layout_Modus_steht_dann_steht_dort_der_Satz_und_die_Anlegezeile()
    {
        var seite = await FrischesBoardImLayoutModus();

        await Expect(seite.Klassenbereich).ToContainTextAsync("Klassen");
        await Expect(seite.HinweisKeineKlassen).ToHaveTextAsync("Dieses Board hat keine Klasse.");
        await Expect(seite.KlassenAnlegezeile).ToBeVisibleAsync();
        await Expect(seite.Klassenzeilen).ToHaveCountAsync(0);
    }

    // US-3: in der Arbeitsansicht ist der Bereich nicht da — dort gehoert der Platz den Karten.
    [Test]
    [Category("US-3")]
    public async Task Wenn_der_Layout_Modus_verlassen_wird_dann_ist_der_Klassenbereich_nicht_mehr_da()
    {
        var seite = await FrischesBoardImLayoutModus();
        await seite.FuelleNeueKlasse("WBS", "WBS-");
        await seite.LegeKlasseAn();
        await Expect(seite.Klassenzeilen).ToHaveCountAsync(1);

        await seite.VerlasseLayoutModus();

        await Expect(seite.Klassenbereich).ToHaveCountAsync(0);
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
    }

    // US-1: das Szenario in einem Zug — anlegen, Plakette, nächste Nummer, leere Felder,
    // Anlagereihenfolge, Reload.
    [Test]
    [Category("US-1")]
    public async Task Wenn_drei_Klassen_angelegt_werden_dann_stehen_sie_in_Anlagereihenfolge_und_ueberstehen_den_Reload()
    {
        var seite = await FrischesBoardImLayoutModus();

        await seite.FuelleNeueKlasse("WBS", "WBS-");
        await seite.LegeKlasseAn();

        await Expect(seite.Klassennamen).ToHaveTextAsync(["WBS"]);
        await Expect(seite.Klassenpraefixe).ToHaveTextAsync(["WBS-"]);
        await Expect(seite.Klassenstaende.Nth(0)).ToContainTextAsync("0 vergeben · nächste WBS-01");
        await Expect(seite.HinweisKeineKlassen).ToHaveCountAsync(0);
        await Expect(seite.KlassenNamensfeld).ToHaveValueAsync(string.Empty);
        await Expect(seite.KlassenPraefixfeld).ToHaveValueAsync(string.Empty);

        await seite.FuelleNeueKlasse("Bugmeldungen", "BUG-");
        await seite.LegeKlasseAn();
        await Expect(seite.Klassenzeilen).ToHaveCountAsync(2);
        await seite.FuelleNeueKlasse("Beschaffung", "BES-");
        await seite.LegeKlasseAn();

        await Expect(seite.Klassennamen).ToHaveTextAsync(["WBS", "Bugmeldungen", "Beschaffung"]);

        await seite.LadeNeu();
        await seite.BetreteLayoutModus();

        await Expect(seite.Klassennamen).ToHaveTextAsync(["WBS", "Bugmeldungen", "Beschaffung"]);
        await Expect(seite.Klassenpraefixe).ToHaveTextAsync(["WBS-", "BUG-", "BES-"]);
    }

    // US-1: das Praefix wird genommen, wie es getippt wurde — kleingeschrieben, mit Unterstrich
    // oder ganz ohne Trenner.
    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_Praefix_mit_eigener_Schreibweise_getippt_wird_dann_steht_es_unveraendert_in_der_Plakette()
    {
        var seite = await FrischesBoardImLayoutModus();

        await seite.FuelleNeueKlasse("Arbeitspakete", "wbs_");
        await seite.LegeKlasseAn();
        await Expect(seite.Klassenzeilen).ToHaveCountAsync(1);
        await seite.FuelleNeueKlasse("Dokumentation", "DOKU");
        await seite.LegeKlasseAn();

        await Expect(seite.Klassenpraefixe).ToHaveTextAsync(["wbs_", "DOKU"]);
        await Expect(seite.Klassenstaende.Nth(0)).ToContainTextAsync("nächste wbs_01");
        await Expect(seite.Klassenstaende.Nth(1)).ToContainTextAsync("nächste DOKU01");
    }

    // US-2: der leere Name.
    [Test]
    [Category("US-2")]
    public async Task Wenn_der_Name_leer_bleibt_dann_erscheint_eine_lesbare_Meldung_und_die_Liste_bleibt_unveraendert()
    {
        var seite = await FrischesBoardImLayoutModus();
        await seite.FuelleNeueKlasse("WBS", "WBS-");
        await seite.LegeKlasseAn();
        await Expect(seite.Klassenzeilen).ToHaveCountAsync(1);

        await seite.FuelleNeueKlasse("   ", "BUG-");
        await seite.LegeKlasseAn();

        await Expect(seite.KlassenZurueckweisung).ToContainTextAsync("Die Klasse wurde nicht angelegt:");
        await Expect(seite.KlassenZurueckweisung).ToContainTextAsync("Eine Klasse braucht einen Namen.");
        await Expect(seite.Klassennamen).ToHaveTextAsync(["WBS"]);
    }

    // US-2: das auf diesem Board vergebene Praefix — in abweichender Schreibweise, damit die
    // Meldung nicht bloss auf Zeichengleichheit anspringt.
    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_vergebenes_Praefix_in_anderer_Schreibweise_kommt_dann_nennt_die_Meldung_die_haltende_Klasse()
    {
        var seite = await FrischesBoardImLayoutModus();
        await seite.FuelleNeueKlasse("WBS", "WBS-");
        await seite.LegeKlasseAn();
        await Expect(seite.Klassenzeilen).ToHaveCountAsync(1);

        await seite.FuelleNeueKlasse("Arbeitspakete", "wbs-");
        await seite.LegeKlasseAn();

        await Expect(seite.KlassenZurueckweisung).ToContainTextAsync("führt auf diesem Board schon die Klasse „WBS“");
        await Expect(seite.KlassenZurueckweisung).ToContainTextAsync("Wähle ein anderes Präfix.");
        await Expect(seite.Klassenzeilen).ToHaveCountAsync(1);
    }

    // US-2: nach der Zurückweisung geht ein freies Praefix wieder durch, und die Meldung
    // verschwindet.
    [Test]
    [Category("US-2")]
    public async Task Wenn_nach_einer_Zurueckweisung_ein_freies_Praefix_kommt_dann_nimmt_die_Seite_es_an()
    {
        var seite = await FrischesBoardImLayoutModus();
        await seite.FuelleNeueKlasse("WBS", "WBS-");
        await seite.LegeKlasseAn();
        await Expect(seite.Klassenzeilen).ToHaveCountAsync(1);
        await seite.FuelleNeueKlasse("Arbeitspakete", "WBS-");
        await seite.LegeKlasseAn();
        await Expect(seite.KlassenZurueckweisung).ToBeVisibleAsync();

        await seite.FuelleNeueKlasse("Arbeitspakete", "WB2-");
        await seite.LegeKlasseAn();

        await Expect(seite.Klassennamen).ToHaveTextAsync(["WBS", "Arbeitspakete"]);
        await Expect(seite.KlassenZurueckweisung).ToHaveCountAsync(0);
    }

    // US-4: die Eindeutigkeit gilt je Board. Der Fall braucht zwei Boards — genau diese Zusage
    // braeche ein global eindeutiger Index still.
    [Test]
    [Category("US-4")]
    public async Task Wenn_dasselbe_Praefix_auf_einem_zweiten_Board_angelegt_wird_dann_fuehren_beide_Boards_es_nebeneinander()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var projektA = await webApi.LegeBoardAn("Projekt A");
        var projektB = await webApi.LegeBoardAn("Projekt B");
        await webApi.LegeKartenklasseAn(projektA.BoardId, "WBS", "WBS-");
        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.OeffneImLayoutModus(projektB.BoardId);

        await seite.FuelleNeueKlasse("WBS", "WBS-");
        await seite.LegeKlasseAn();

        await Expect(seite.Klassenpraefixe).ToHaveTextAsync(["WBS-"]);
        await Expect(seite.Klassenstaende.Nth(0)).ToContainTextAsync("0 vergeben · nächste WBS-01");

        await seite.OeffneImLayoutModus(projektA.BoardId);

        await Expect(seite.Klassenpraefixe).ToHaveTextAsync(["WBS-"]);
        await Expect(seite.Klassenstaende.Nth(0)).ToContainTextAsync("0 vergeben · nächste WBS-01");

        await seite.FuelleNeueKlasse("Arbeitspakete", "WBS-");
        await seite.LegeKlasseAn();

        await Expect(seite.KlassenZurueckweisung).ToContainTextAsync("führt auf diesem Board schon die Klasse „WBS“");
        await Expect(seite.Klassenzeilen).ToHaveCountAsync(1);
    }

    // US-2: der Ausfall der WebApi während der Klassenpflege — der uebliche Ausfallsatz statt
    // einer Ausnahmeseite, wie bei Boardliste und Spaltenpflege.
    [Test]
    [Category("US-2")]
    public async Task Wenn_die_WebApi_waehrend_der_Klassenpflege_ausfaellt_dann_erscheint_der_uebliche_Ausfallsatz()
    {
        var seite = await FrischesBoardImLayoutModus();
        await Expect(seite.HinweisKeineKlassen).ToBeVisibleAsync();

        Testumgebung.Aktuelle.HalteWebApiAn();
        await seite.FuelleNeueKlasse("WBS", "WBS-");
        await seite.LegeKlasseAn();

        await Expect(seite.KlassenFehlermeldung).ToBeVisibleAsync();
        await Expect(seite.KlassenFehlermeldung).ToContainTextAsync("Die WebApi ist nicht erreichbar.");
        await Expect(seite.Ausnahmeanzeige).ToBeHiddenAsync();
        await Expect(seite.Klassenzeilen).ToHaveCountAsync(0);
    }

    private async Task<BoardSeite> FrischesBoardImLayoutModus()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.OeffneImLayoutModus(board.BoardId);
        return seite;
    }
}
