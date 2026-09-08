using System.Globalization;
using KanbanC.BL.Integrations.Auswertungen;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.Integrations.Auswertungen;

// Der Dienst verdrahtet nur: prüfen, lesen, je Karte rechnen, über die Kette rechnen,
// zusammensetzen. Die Kopfzahlen kommen gerechnet heraus — ein Agent bekommt den Pufferstand und
// nicht seine Summanden.
public class AuswertungsServicePufferTests
{
    private const long BoardId = 4;
    private const long FremdesBoard = 7;
    private const long KartenklasseId = 1;
    private static readonly DateOnly Gestern = new(2026, 9, 7);

    [Test]
    public void Wenn_das_Rechenbeispiel_gerechnet_wird_dann_tragen_die_Kopfzahlen_Kettenpuffer_Verbrauch_Anteil_und_Fortschritt()
    {
        var auswertungen = new TestAuswertungsrepository().MitPufferstaenden(BoardId, KartenklasseId, Rechenbeispiel());
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Puffer(BoardId, KartenklasseId);

        Assert.That(ergebnis.IstErfolg, Is.True);
        var kopfzahlen = ergebnis.Wert.Kopfzahlen;
        Assert.Multiple(() =>
        {
            Assert.That(kopfzahlen.KettenpufferStunden, Is.EqualTo(5.1m));
            Assert.That(kopfzahlen.VerbrauchteStunden, Is.EqualTo(4.0m));
            Assert.That(kopfzahlen.VerbrauchsanteilProzent, Is.EqualTo(78m));
            Assert.That(kopfzahlen.FortschrittProzent, Is.EqualTo(74m));
            Assert.That(kopfzahlen.ErledigteKarten, Is.EqualTo(2));
            Assert.That(kopfzahlen.Kartenanzahl, Is.EqualTo(5));
            Assert.That(kopfzahlen.KartenOhneSoll, Is.EqualTo(1));
        });
    }

    // Je Zeile Nummer, Titel, Sollband, erfasste Zeit und verbrauchter Puffer — in
    // Kartennummernfolge, wie sie gelesen wurden.
    [Test]
    public void Wenn_das_Rechenbeispiel_gerechnet_wird_dann_traegt_jede_Zeile_Nummer_Titel_Band_Zeit_und_Verbrauch()
    {
        var auswertungen = new TestAuswertungsrepository().MitPufferstaenden(BoardId, KartenklasseId, Rechenbeispiel());
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Puffer(BoardId, KartenklasseId);

        var erste = ergebnis.Wert.Zeilen[0];
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Zeilen.Select(zeile => zeile.Kartennummer), Is.EqualTo(new[] { "WBS-01", "WBS-02", "WBS-03", "WBS-04", "WBS-05" }));
            Assert.That(erste.Titel, Is.EqualTo("[I0001] Karte K1"));
            Assert.That(erste.Sollband, Is.EqualTo(new Zeitband(2.0m, 4.0m)));
            Assert.That(erste.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(5)));
            Assert.That(erste.VerbrauchterPufferStunden, Is.EqualTo(3.0m));
            Assert.That(erste.IstErledigt, Is.True);
            Assert.That(erste.IstArchiviert, Is.False);
        });
    }

    // Eine Karte ohne Band trägt Band und Verbrauch ohne Wert — nicht 0,0 —, obwohl 8,0 h an ihr
    // erfasst sind.
    [Test]
    public void Wenn_eine_Karte_kein_Sollband_traegt_dann_stehen_Band_und_Verbrauch_ihrer_Zeile_ohne_Wert()
    {
        var auswertungen = new TestAuswertungsrepository().MitPufferstaenden(BoardId, KartenklasseId, Rechenbeispiel());
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Puffer(BoardId, KartenklasseId);

        var ohneBand = ergebnis.Wert.Zeilen.Single(zeile => zeile.Kartennummer == "WBS-05");
        Assert.Multiple(() =>
        {
            Assert.That(ohneBand.Sollband, Is.Null);
            Assert.That(ohneBand.VerbrauchterPufferStunden, Is.Null);
            Assert.That(ohneBand.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(8)));
        });
    }

    // Ein Bestand ohne Karten ist kein Fehler: leere Zeilenliste, alle vier Größen ohne Wert.
    [Test]
    public void Wenn_der_Bestand_keine_Karte_fuehrt_dann_kommt_die_leere_Auswertung_und_kein_Fehler()
    {
        var dienst = Dienst(new TestAuswertungsrepository());

        var ergebnis = dienst.Puffer(BoardId, KartenklasseId);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Zeilen, Is.Empty);
            Assert.That(ergebnis.Wert.Kopfzahlen.Kartenanzahl, Is.Zero);
            Assert.That(ergebnis.Wert.Kopfzahlen.KettenpufferStunden, Is.Null);
            Assert.That(ergebnis.Wert.Kopfzahlen.FortschrittProzent, Is.Null);
        });
    }

    // Ein Lesevorgang je Bestand und nicht je Karte.
    [Test]
    public void Wenn_der_Bestand_vierzig_Karten_hat_dann_wird_er_genau_einmal_gelesen()
    {
        var auswertungen = new TestAuswertungsrepository().MitPufferstaenden(BoardId, KartenklasseId, VierzigKarten());
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Puffer(BoardId, KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Kopfzahlen.Kartenanzahl, Is.EqualTo(40));
            Assert.That(auswertungen.Lesevorgaenge, Is.EqualTo(1));
        });
    }

    // Dieselbe Prüfreihenfolge wie beim Burndown: erst das Board, dann die Kartenklasse — die
    // Karten einer fremden Klasse werden gar nicht erst gelesen.
    [Test]
    public void Wenn_es_das_Board_nicht_gibt_dann_wird_der_Bestand_gar_nicht_erst_gelesen()
    {
        var auswertungen = new TestAuswertungsrepository();
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Puffer(999, KartenklasseId);

        Assert.That(ergebnis.IstErfolg, Is.False);
        var befund = EinzigerBefund(ergebnis.Befunde);
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("board-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(auswertungen.WurdeGelesen, Is.False);
        });
    }

    [Test]
    public void Wenn_es_die_Kartenklasse_nicht_gibt_dann_nennt_der_Befund_ihren_eigenen_Code()
    {
        var auswertungen = new TestAuswertungsrepository();
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Puffer(BoardId, 999);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(EinzigerBefund(ergebnis.Befunde).Code, Is.EqualTo("kartenklasse-unbekannt"));
            Assert.That(auswertungen.WurdeGelesen, Is.False);
        });
    }

    [Test]
    public void Wenn_die_Kartenklasse_einem_fremden_Board_gehoert_dann_sagt_der_Befund_wem()
    {
        var kartenklassen = TestKartenklassenRepository.MitKartenklassen(BoardId, ("WBS", "WBS-")).MitZusaetzlichemBoard(FremdesBoard);
        kartenklassen.LegeAn(FremdesBoard, new KartenklasseAnlegenAnfrage("Bugs", "BUG-"));
        var fremdeKartenklasseId = kartenklassen.Kartenklassen(FremdesBoard).Single().KartenklasseId;
        var auswertungen = new TestAuswertungsrepository();
        var dienst = new AuswertungsService(auswertungen, kartenklassen);

        var ergebnis = dienst.Puffer(BoardId, fremdeKartenklasseId);

        Assert.That(ergebnis.IstErfolg, Is.False);
        var befund = EinzigerBefund(ergebnis.Befunde);
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kartenklasse-fremd"));
            Assert.That(befund.Meldung, Does.Contain(FremdesBoard.ToString(CultureInfo.InvariantCulture)));
            Assert.That(auswertungen.WurdeGelesen, Is.False);
        });
    }

    private static Pufferstandkarte[] Rechenbeispiel()
    {
        return
        [
            Karte(1, TimeSpan.FromHours(5), new Zeitband(2.0m, 4.0m), Gestern),
            Karte(2, TimeSpan.FromMinutes(12), new Zeitband(0.4m, 1.5m), null),
            Karte(3, TimeSpan.FromHours(3), new Zeitband(2.0m, 2.0m), Gestern),
            Karte(4, TimeSpan.Zero, new Zeitband(1.0m, 3.0m), null),
            Karte(5, TimeSpan.FromHours(8), null, null),
        ];
    }

    private static Pufferstandkarte[] VierzigKarten()
    {
        var karten = new List<Pufferstandkarte>();
        for (var nummer = 1; nummer <= 40; nummer++)
        {
            karten.Add(Karte(nummer, TimeSpan.FromHours(1), new Zeitband(1.0m, 2.0m), null));
        }

        return karten.ToArray();
    }

    private static Pufferstandkarte Karte(long karteId, TimeSpan erfassteZeit, Zeitband? sollband, DateOnly? erledigtAm)
    {
        return new Pufferstandkarte(karteId, $"WBS-{karteId:D2}", $"[I{karteId:D4}] Karte K{karteId}", erfassteZeit, sollband, erledigtAm, IstArchiviert: false);
    }

    private static Fehlerbefund EinzigerBefund(Pruefbefunde befunde)
    {
        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        var befund = befunde[0];
        Befundpruefung.ErwarteVollstaendigenBefund(befund, befund.Code);
        return befund;
    }

    private static AuswertungsService Dienst(TestAuswertungsrepository auswertungen)
    {
        return new AuswertungsService(auswertungen, TestKartenklassenRepository.MitKartenklassen(BoardId, ("WBS", "WBS-")));
    }
}
