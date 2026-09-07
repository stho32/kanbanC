using System.Text.RegularExpressions;
using KanbanC.Contracts.Karten;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-1 bis US-8 und US-11 über die ganze Strecke: der **erste** Abbruch, zwischen Oberfläche und
// WebApi. Er wird **echt erzeugt** — die WebApi wird angehalten und wieder gestartet, so wie es der
// Ausfalltest schon tut.
// Die Bewegung während der Trennung geht am Dienst vorbei in die Datenbank: die WebApi ist in
// diesem Moment aus, und genau das ist die Lage, um die es geht — die Welt verändert sich, während
// die Sicht nicht zusehen kann. Über die neu gestartete WebApi wäre es ein Wettlauf gegen die
// Wiederaufnahme der Leitung.
// Gewartet wird nie eine feste Pause: jede Erwartung ist ein Expect mit Zeitschranke.
[TestFixture]
public class AufschliessenE2ETests : PageTest
{
    // Ein Neustart der WebApi kostet die Ereignisleitung ihre Wiederaufnahmepause; die Zeitschranke
    // liegt deshalb über der Vorgabe von fünf Sekunden.
    private const int SchrankeNachNeustart = 20000;

    // US-2 und US-1: solange die Leitung steht, sagt die Kopfzeile nichts. Reißt sie ab, ist genau
    // das die Nachricht — mit einem Zeitpunkt, nicht mit einer mitzählenden Dauer.
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Leitung_abreisst_dann_nennt_die_Kopfzeile_den_Stand_und_die_Flaeche_wird_ruhiger()
    {
        var aufbau = await BoardMitDreiKarten();
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);
        await Expect(board.Verbindungsmarke).ToHaveCountAsync(0);
        await Expect(board.GetrennterSchirm).ToHaveCountAsync(0);

        Testumgebung.Aktuelle.HalteWebApiAn();

        await Expect(board.Verbindungsmarke).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });
        await Expect(board.Verbindungsmarke).ToHaveTextAsync(new Regex(@"^nicht live · Stand von \d\d:\d\d$"));
        await Expect(board.GetrennterSchirm).ToHaveCountAsync(1);
        // Der getrennte Schirm bleibt lesbar: die Titel stehen unverändert da.
        await Expect(board.Kartentitel).ToHaveTextAsync(["A", "B", "C"]);
        await Testumgebung.Aktuelle.StarteWebApiNeu();
        await Expect(board.Verbindungsmarke).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = SchrankeNachNeustart });
    }

    // US-4, US-5 und US-6: nach der Rückkehr steht das Board frisch da, das Band nennt Zahl und
    // Zeitpunkt, und die bewegte Karte trägt eine Marke ohne Wer und ohne Wann.
    [Test]
    [Category("US-4")]
    public async Task Wenn_sich_waehrend_der_Trennung_eine_Karte_bewegt_dann_steht_nach_der_Rueckkehr_das_Band_und_die_Karte_ist_markiert()
    {
        var aufbau = await BoardMitDreiKarten();
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);
        await board.SchalteKartenzahl(true);
        await Expect(board.Kartenzahlstellen.Nth(0)).ToHaveTextAsync("3");

        Testumgebung.Aktuelle.HalteWebApiAn();
        await Expect(board.Verbindungsmarke).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });
        Testumgebung.Aktuelle.Datenbank.VerschiebeKarte(aufbau.KarteC, aufbau.InArbeitId, 1);
        await Testumgebung.Aktuelle.StarteWebApiNeu();

        await Expect(board.Aufschliessband).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });
        await Expect(board.AufschliessbandAuskunft).ToHaveTextAsync(new Regex(@"^Wieder verbunden\. 1 Änderung seit \d\d:\d\d ist nachgeholt und unten markiert\.$"));
        // Frisch geholt, nicht nachgespielt: die Karte steht an ihrer neuen Stelle, und die
        // Bahnenzahlen stimmen.
        await Expect(board.KartentitelDerBahn(board.Spaltenbahn(aufbau.InArbeitId))).ToHaveTextAsync(["C"]);
        await Expect(board.Kartenzahlstellen.Nth(0)).ToHaveTextAsync("2");
        await Expect(board.Kartenzahlstellen.Nth(1)).ToHaveTextAsync("1");
        await Expect(board.EinflugmarkeDerKarte(board.KarteMitTitel("C"))).ToHaveTextAsync("geändert, während die Verbindung weg war");
        await Expect(board.Einflugmarken).ToHaveCountAsync(1);
    }

    // US-6: „schließen" räumt Band **und** Marken zusammen weg — sie sind eine Aussage. Und die
    // Marke verschwindet vorher **nicht von selbst**: als einzige im Projekt hat sie keine Frist.
    [Test]
    [Category("US-6")]
    public async Task Wenn_das_Band_geschlossen_wird_dann_verschwinden_Band_und_Marken_zusammen()
    {
        var aufbau = await BoardMitDreiKarten();
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);
        Testumgebung.Aktuelle.HalteWebApiAn();
        await Expect(board.Verbindungsmarke).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });
        Testumgebung.Aktuelle.Datenbank.VerschiebeKarte(aufbau.KarteC, aufbau.InArbeitId, 1);
        await Testumgebung.Aktuelle.StarteWebApiNeu();
        await Expect(board.Aufschliessband).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });

        // Die Standzeit einer Einflugmarke ist im Testlauf gesenkt und längst um: die Nachholmarke
        // steht trotzdem noch.
        await Task.Delay(TimeSpan.FromSeconds(Testumgebung.MarkenstandzeitInSekunden + 1));
        await Expect(board.Einflugmarken).ToHaveCountAsync(1);

        await board.AufschliessbandSchliessen.ClickAsync();

        await Expect(board.Aufschliessband).ToHaveCountAsync(0);
        await Expect(board.Einflugmarken).ToHaveCountAsync(0);
    }

    // US-8: war die Leitung weg, ohne dass etwas geschehen ist, gibt es nichts zu sagen — kein
    // Band, keine Marke.
    [Test]
    [Category("US-8")]
    public async Task Wenn_sich_waehrend_der_Trennung_nichts_bewegt_hat_dann_erscheint_kein_Band()
    {
        var aufbau = await BoardMitDreiKarten();
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        Testumgebung.Aktuelle.HalteWebApiAn();
        await Expect(board.Verbindungsmarke).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });
        await Testumgebung.Aktuelle.StarteWebApiNeu();

        await Expect(board.Verbindungsmarke).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = SchrankeNachNeustart });
        await Expect(board.Aufschliessband).ToHaveCountAsync(0);
        await Expect(board.Einflugmarken).ToHaveCountAsync(0);
    }

    // US-7: über der Zusammenfassungsschwelle bleibt es bei der Zahl im Band. Die Schwelle ist für
    // den Lauf auf eins gesenkt — im Betrieb liegt sie bei etwa zehn.
    [Test]
    [Category("US-7")]
    public async Task Wenn_mehr_Karten_als_die_Schwelle_sich_bewegt_haben_dann_nennt_das_Band_nur_die_Zahl()
    {
        var aufbau = await BoardMitDreiKarten();
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        Testumgebung.Aktuelle.HalteWebApiAn();
        await Expect(board.Verbindungsmarke).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });
        Testumgebung.Aktuelle.Datenbank.VerschiebeKarte(aufbau.KarteB, aufbau.InArbeitId, 1);
        Testumgebung.Aktuelle.Datenbank.VerschiebeKarte(aufbau.KarteC, aufbau.InArbeitId, 2);
        await Testumgebung.Aktuelle.StarteWebApiNeu();

        await Expect(board.Aufschliessband).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });
        await Expect(board.AufschliessbandAuskunft).ToHaveTextAsync(new Regex(@"^Wieder verbunden\. 2 Änderungen seit \d\d:\d\d sind nachgeholt\.$"));
        await Expect(board.KartentitelDerBahn(board.Spaltenbahn(aufbau.InArbeitId))).ToHaveTextAsync(["B", "C"]);
        await Expect(board.Einflugmarken).ToHaveCountAsync(0);
    }

    // US-11: eine frisch geöffnete Sicht schließt gegen nichts auf — die erste Verbindung nach dem
    // Start ist keine Rückkehr.
    [Test]
    [Category("US-11")]
    public async Task Wenn_ein_Board_frisch_geoeffnet_wird_dann_erscheint_weder_Band_noch_Kopfzeilenmarke()
    {
        var aufbau = await BoardMitDreiKarten();
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await board.Oeffne(aufbau.BoardId);

        await Expect(board.Kartentitel).ToHaveTextAsync(["A", "B", "C"]);
        await Expect(board.Aufschliessband).ToHaveCountAsync(0);
        await Expect(board.Verbindungsmarke).ToHaveCountAsync(0);
    }

    // Traegt eine Karte zugleich eine Nachholmarke und eine frische Einflugmarke, gewinnt die
    // **Einflugmarke**: sie ist die genauere Aussage — sie kennt Urheber und Zeitpunkt, die ein
    // Vergleich nicht kennt —, und beide teilen sich dieselbe eine Fusszeile. Laeuft ihre Frist ab,
    // steht die Nachholmarke wieder da: sie hat keine und geht erst mit dem Band.
    [Test]
    public async Task Wenn_eine_nachgeholte_Karte_erneut_bewegt_wird_dann_gewinnt_die_Einflugmarke_und_die_Nachholmarke_kehrt_zurueck()
    {
        var aufbau = await BoardMitDreiKarten();
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);
        Testumgebung.Aktuelle.HalteWebApiAn();
        await Expect(board.Verbindungsmarke).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });
        Testumgebung.Aktuelle.Datenbank.VerschiebeKarte(aufbau.KarteC, aufbau.InArbeitId, 1);
        await Testumgebung.Aktuelle.StarteWebApiNeu();
        await Expect(board.EinflugmarkeDerKarte(board.KarteMitTitel("C"))).ToHaveTextAsync("geändert, während die Verbindung weg war", new LocatorAssertionsToHaveTextOptions { Timeout = SchrankeNachNeustart });

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.BereitId, 1));

        await Expect(board.EinflugmarkeDerKarte(board.KarteMitTitel("C"))).ToHaveTextAsync("über die API · gerade eben");
        await Expect(board.Einflugmarken).ToHaveCountAsync(1);

        // Die Frist der Einflugmarke laeuft ab — die Nachholmarke stand die ganze Zeit darunter.
        await Expect(board.EinflugmarkeDerKarte(board.KarteMitTitel("C")))
            .ToHaveTextAsync("geändert, während die Verbindung weg war", new LocatorAssertionsToHaveTextOptions { Timeout = (Testumgebung.MarkenstandzeitInSekunden + 5) * 1000 });
        await Expect(board.Aufschliessband).ToBeVisibleAsync();
    }

    // US-9: hält jemand eine Karte in der Hand, ordnet sich unter der Maus nichts um — das
    // Aufschließen wartet sichtbar bis zum Loslassen. **Dieselbe Regel wie für eine fremde
    // Bewegung**, kein zweiter Mechanismus.
    [Test]
    [Category("US-9")]
    public async Task Wenn_jemand_eine_Karte_in_der_Hand_haelt_dann_wartet_das_Aufschliessen_bis_zum_Loslassen()
    {
        var aufbau = await BoardMitDreiKarten();
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);
        await board.NimmKarteAuf(board.KarteMitTitel("A"));
        await Expect(board.AblageflaecheDerBahn(board.Spaltenbahn(aufbau.InArbeitId))).ToBeVisibleAsync();

        Testumgebung.Aktuelle.HalteWebApiAn();
        await Expect(board.Verbindungsmarke).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });
        Testumgebung.Aktuelle.Datenbank.VerschiebeKarte(aufbau.KarteC, aufbau.InArbeitId, 1);
        await Testumgebung.Aktuelle.StarteWebApiNeu();

        await Expect(board.WartendeAenderung).ToHaveTextAsync("1 Änderung wartet", new LocatorAssertionsToHaveTextOptions { Timeout = SchrankeNachNeustart });
        await Expect(board.KartentitelDerBahn(board.Spaltenbahn(aufbau.BereitId))).ToHaveTextAsync(["A", "B", "C"]);
        await Expect(board.Aufschliessband).ToHaveCountAsync(0);

        await board.LasseAusserhalbJederStelleLos();

        await Expect(board.WartendeAenderung).ToHaveCountAsync(0);
        await Expect(board.Aufschliessband).ToBeVisibleAsync();
        await Expect(board.KartentitelDerBahn(board.Spaltenbahn(aufbau.InArbeitId))).ToHaveTextAsync(["C"]);
    }

    // US-10 und US-3: die offene Kartenseite holt nach der Rückkehr frisch und nennt die aktuelle
    // Spalte — **ohne Band und ohne Zahl**: sie zeigt eine Karte, nicht einen Bestand.
    [Test]
    [Category("US-10")]
    public async Task Wenn_die_Kartenseite_offen_steht_dann_holt_sie_nach_der_Rueckkehr_frisch_und_zeigt_kein_Band()
    {
        var aufbau = await BoardMitDreiKarten();
        var kartenseite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await kartenseite.Oeffne(aufbau.KarteC);
        await Expect(kartenseite.Spalte).ToHaveTextAsync("Spalte Zu erledigen");

        Testumgebung.Aktuelle.HalteWebApiAn();
        await Expect(Page.Locator("#verbindungsmarke")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });
        Testumgebung.Aktuelle.Datenbank.VerschiebeKarte(aufbau.KarteC, aufbau.InArbeitId, 1);
        await Testumgebung.Aktuelle.StarteWebApiNeu();

        await Expect(kartenseite.Spalte).ToHaveTextAsync("Spalte In Arbeit", new LocatorAssertionsToHaveTextOptions { Timeout = SchrankeNachNeustart });
        await Expect(Page.Locator("#aufschliessband")).ToHaveCountAsync(0);
    }

    // US-10: der ungesendete Text überlebt die Trennung — nichts wird ausgetauscht, solange ein
    // Feld offen steht, und die Meldung wartet sichtbar. Dieselbe Warteregel wie für eine fremde
    // Bewegung, kein zweiter Mechanismus.
    [Test]
    [Category("US-10")]
    public async Task Wenn_ein_Feld_offen_steht_dann_wartet_das_Aufschliessen_bis_zum_Schliessen_des_Feldes()
    {
        var aufbau = await BoardMitDreiKarten();
        var kartenseite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await kartenseite.Oeffne(aufbau.KarteC);
        await kartenseite.BeschreibungHinzufuegen.ClickAsync();
        await kartenseite.Beschreibungsfeld.FillAsync("Offen ist noch, was mit Knoten geschieht");

        Testumgebung.Aktuelle.HalteWebApiAn();
        await Expect(Page.Locator("#verbindungsmarke")).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachNeustart });
        Testumgebung.Aktuelle.Datenbank.VerschiebeKarte(aufbau.KarteC, aufbau.InArbeitId, 1);
        await Testumgebung.Aktuelle.StarteWebApiNeu();

        await Expect(kartenseite.WartendeAenderung).ToHaveTextAsync("1 Änderung wartet", new LocatorAssertionsToHaveTextOptions { Timeout = SchrankeNachNeustart });
        await Expect(kartenseite.Beschreibungsfeld).ToHaveValueAsync("Offen ist noch, was mit Knoten geschieht");
        await Expect(kartenseite.Spalte).ToHaveTextAsync("Spalte Zu erledigen");

        await kartenseite.Beschreibungsfeld.BlurAsync();

        await Expect(kartenseite.Spalte).ToHaveTextAsync("Spalte In Arbeit");
        await Expect(kartenseite.WartendeAenderung).ToHaveCountAsync(0);
    }

    private static async Task<Aufbau> BoardMitDreiKarten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("KanbanC — Release 2");
        var bereitId = board.Spalten[0].SpalteId;
        await webApi.LegeKarteAn(board.BoardId, bereitId, "A");
        var karteB = await webApi.LegeKarteAn(board.BoardId, bereitId, "B");
        var karteC = await webApi.LegeKarteAn(board.BoardId, bereitId, "C");
        return new Aufbau(board.BoardId, bereitId, board.Spalten[1].SpalteId, karteB.KarteId, karteC.KarteId);
    }

    private sealed record Aufbau(long BoardId, long BereitId, long InArbeitId, long KarteB, long KarteC);
}
