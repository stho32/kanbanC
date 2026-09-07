using System.Globalization;
using KanbanC.BL.Integrations.Auswertungen;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.Integrations.Auswertungen;

// Der Dienst verdrahtet nur: prüfen, lesen, schneiden, zusammensetzen. **Ein Aufruf trägt Zeilen
// und Stand** — sonst zählte der Schirm etwas anderes, als die Datei enthält.
public class AuswertungsServiceZeitexportTests
{
    private const long BoardId = 4;
    private const long FremdesBoard = 7;
    private const long KartenklasseId = 1;
    private const string Boardname = "KanbanC — Release 2";

    [Test]
    public void Wenn_der_Zeitexport_gerechnet_wird_dann_tragen_Zeilen_und_Stand_denselben_Ausschnitt()
    {
        var auswertungen = new TestAuswertungsrepository().MitZeiteintraegen(BoardId, KartenklasseId, Boardname, Rechenbeispiel());
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Zeitexport(BoardId, KartenklasseId, von: null, bis: null);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Zeilen.Zeilenanzahl, Is.EqualTo(5));
            Assert.That(ergebnis.Wert.Stand.Eintraege, Is.EqualTo(5));
            Assert.That(auswertungen.Lesevorgaenge, Is.EqualTo(1));
        });
    }

    // Die Zählzeile kommt **gerechnet** aus dem Dienst: fünf Einträge, drei Karten, zwei
    // Kontributoren, davon einer laufend.
    [Test]
    public void Wenn_der_Stand_gelesen_wird_dann_lautet_er_fuenf_Eintraege_drei_Karten_zwei_Kontributoren_und_ein_laufender()
    {
        var auswertungen = new TestAuswertungsrepository().MitZeiteintraegen(BoardId, KartenklasseId, Boardname, Rechenbeispiel());
        var dienst = Dienst(auswertungen);

        var stand = dienst.Zeitexport(BoardId, KartenklasseId, von: null, bis: null).Wert.Stand;

        Assert.Multiple(() =>
        {
            Assert.That(stand.Eintraege, Is.EqualTo(5));
            Assert.That(stand.Karten, Is.EqualTo(3));
            Assert.That(stand.Kontributoren, Is.EqualTo(2));
            Assert.That(stand.Laufende, Is.EqualTo(1));
        });
    }

    [Test]
    public void Wenn_der_Stand_gelesen_wird_dann_nennt_der_Dateiname_Board_und_die_gelieferten_Grenzen()
    {
        var auswertungen = new TestAuswertungsrepository().MitZeiteintraegen(BoardId, KartenklasseId, Boardname, Rechenbeispiel());
        var dienst = Dienst(auswertungen);

        var stand = dienst.Zeitexport(BoardId, KartenklasseId, von: null, bis: null).Wert.Stand;

        Assert.Multiple(() =>
        {
            Assert.That(stand.Von, Is.EqualTo(new DateOnly(2026, 8, 31)));
            Assert.That(stand.Bis, Is.EqualTo(new DateOnly(2026, 9, 7)));
            Assert.That(stand.Dateiname, Is.EqualTo("kanbanc-release-2-zeiten-2026-08-31_2026-09-07.csv"));
        });
    }

    [Test]
    public void Wenn_eine_Spanne_gewaehlt_ist_dann_schneidet_sie_Zeilen_und_Stand_zugleich()
    {
        var auswertungen = new TestAuswertungsrepository().MitZeiteintraegen(BoardId, KartenklasseId, Boardname, Rechenbeispiel());
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Zeitexport(BoardId, KartenklasseId, new DateOnly(2026, 9, 6), new DateOnly(2026, 9, 6));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Zeilen.Zeilenanzahl, Is.EqualTo(1));
            Assert.That(ergebnis.Wert.Stand.Eintraege, Is.EqualTo(1));
            Assert.That(ergebnis.Wert.Stand.Dateiname, Is.EqualTo("kanbanc-release-2-zeiten-2026-09-06_2026-09-06.csv"));
        });
    }

    // Ein Bestand ohne jeden Zeiteintrag ist kein Fehler: der Stand steht mit Nullen da, und die
    // Datei bekommt die Kopfzeile allein.
    [Test]
    public void Wenn_der_Bestand_keinen_Zeiteintrag_fuehrt_dann_ist_das_kein_Fehler()
    {
        var auswertungen = new TestAuswertungsrepository().MitZeiteintraegen(BoardId, KartenklasseId, Boardname);
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Zeitexport(BoardId, KartenklasseId, von: null, bis: null);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Stand.Eintraege, Is.Zero);
            Assert.That(ergebnis.Wert.Stand.Von, Is.EqualTo(Heute()));
            Assert.That(ergebnis.Wert.Stand.Bis, Is.EqualTo(Heute()));
        });
    }

    [Test]
    public void Wenn_es_das_Board_nicht_gibt_dann_wird_gar_nicht_erst_gelesen()
    {
        var auswertungen = new TestAuswertungsrepository();
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Zeitexport(999, KartenklasseId, von: null, bis: null);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(EinzigerBefund(ergebnis.Befunde).Code, Is.EqualTo("board-unbekannt"));
            Assert.That(auswertungen.WurdeGelesen, Is.False);
        });
    }

    [Test]
    public void Wenn_es_die_Kartenklasse_nicht_gibt_dann_nennt_der_Befund_ihren_eigenen_Code()
    {
        var auswertungen = new TestAuswertungsrepository();
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Zeitexport(BoardId, 999, von: null, bis: null);

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

        var ergebnis = dienst.Zeitexport(BoardId, fremdeKartenklasseId, von: null, bis: null);

        Assert.That(ergebnis.IstErfolg, Is.False);
        var befund = EinzigerBefund(ergebnis.Befunde);
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kartenklasse-fremd"));
            Assert.That(befund.Meldung, Does.Contain(FremdesBoard.ToString(CultureInfo.InvariantCulture)));
            Assert.That(auswertungen.WurdeGelesen, Is.False);
        });
    }

    private static Zeitexportzeile[] Rechenbeispiel()
    {
        return
        [
            new Zeitexportzeile(5, "WBS-12", "Kartentitel", 2, "Claude-Agent", Kontributorart.Agent, Zeitpunkt(8, 31, 22, 0), Zeitpunkt(9, 2, 5, 40)),
            new Zeitexportzeile(3, "WBS-24", "Timer stoppen", 1, "Stefan", Kontributorart.Mensch, Zeitpunkt(9, 6, 14, 2), Zeitpunkt(9, 6, 14, 50)),
            new Zeitexportzeile(1, "WBS-30", "WBS-Datei importieren", 2, "Claude-Agent", Kontributorart.Agent, Zeitpunkt(9, 7, 9, 12), Zeitpunkt(9, 7, 11, 24)),
            new Zeitexportzeile(2, "WBS-30", "WBS-Datei importieren", 1, "Stefan", Kontributorart.Mensch, Zeitpunkt(9, 7, 9, 40), Zeitpunkt(9, 7, 9, 52)),
            new Zeitexportzeile(4, "WBS-24", "Timer stoppen", 1, "Stefan", Kontributorart.Mensch, Zeitpunkt(9, 7, 13, 0), null),
        ];
    }

    private static DateTimeOffset Zeitpunkt(int monat, int tag, int stunde, int minute)
    {
        return new DateTimeOffset(2026, monat, tag, stunde, minute, 0, TimeSpan.FromHours(2));
    }

    private static DateOnly Heute()
    {
        return DateOnly.FromDateTime(DateTime.Today); // stil-check: C03 dieselbe Uhr wie der Dienst, dessen Leerfall der Test prüft
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
