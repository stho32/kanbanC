using System.Globalization;
using KanbanC.BL.Integrations.Auswertungen;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.Integrations.Auswertungen;

public class AuswertungsServiceTests
{
    private const long BoardId = 4;
    private const long FremdesBoard = 7;
    private const long KartenklasseId = 1;

    [Test]
    public void Wenn_der_Bestand_gelesen_wird_dann_traegt_jede_Zeile_Nummer_Titel_Zeit_Band_Abweichung_und_Archivstand()
    {
        var auswertungen = new TestAuswertungsrepository().MitBestand(
            BoardId,
            KartenklasseId,
            new SollIstKarte(11, "WBS-30", "[I0030] WBS-Datei importieren", TimeSpan.FromMinutes(144), new Zeitband(38.0m, 44.0m), IstArchiviert: false));
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.SollIst(BoardId, KartenklasseId);

        Assert.That(ergebnis.IstErfolg, Is.True);
        var zeile = ergebnis.Wert.Zeilen.Single();
        Assert.Multiple(() =>
        {
            Assert.That(zeile.KarteId, Is.EqualTo(11));
            Assert.That(zeile.Kartennummer, Is.EqualTo("WBS-30"));
            Assert.That(zeile.Titel, Is.EqualTo("[I0030] WBS-Datei importieren"));
            Assert.That(zeile.ErfassteZeit, Is.EqualTo(TimeSpan.FromMinutes(144)));
            Assert.That(zeile.Sollband, Is.EqualTo(new Zeitband(38.0m, 44.0m)));
            Assert.That(zeile.Abweichung, Is.EqualTo(new Abweichung(Abweichungslage.UnterDemBand, null)));
            Assert.That(zeile.IstArchiviert, Is.False);
        });
    }

    // Ein Lesevorgang je Bestand und nicht je Karte: vierzig Karten kosten dieselbe eine Abfrage
    // wie eine.
    [Test]
    public void Wenn_der_Bestand_vierzig_Karten_hat_dann_wird_er_genau_einmal_gelesen()
    {
        var auswertungen = new TestAuswertungsrepository().MitBestand(BoardId, KartenklasseId, VierzigKarten());
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.SollIst(BoardId, KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Zeilen, Has.Count.EqualTo(40));
            Assert.That(auswertungen.Lesevorgaenge, Is.EqualTo(1));
        });
    }

    [Test]
    public void Wenn_der_Bestand_gerechnet_wird_dann_traegt_die_Summenzeile_Zeitsumme_Bandsumme_Abweichung_und_die_Karten_ohne_Soll()
    {
        var auswertungen = new TestAuswertungsrepository().MitBestand(
            BoardId,
            KartenklasseId,
            Karte(11, TimeSpan.FromHours(40), new Zeitband(38.0m, 44.0m)),
            Karte(12, TimeSpan.FromHours(1), new Zeitband(3.2m, 4.3m)),
            Karte(13, TimeSpan.FromHours(2), null));
        var dienst = Dienst(auswertungen);

        var summe = dienst.SollIst(BoardId, KartenklasseId).Wert.Summe;

        Assert.Multiple(() =>
        {
            Assert.That(summe.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(43)));
            Assert.That(summe.Sollband, Is.EqualTo(new Zeitband(41.2m, 48.3m)));
            Assert.That(summe.Abweichung, Is.EqualTo(new Abweichung(Abweichungslage.ImBand, null)));
            Assert.That(summe.KartenOhneSoll, Is.EqualTo(1));
        });
    }

    // Ohne Band keine Abweichung — nicht „im Band" und nicht „0".
    [Test]
    public void Wenn_eine_Karte_kein_Sollband_traegt_dann_traegt_ihre_Zeile_weder_Band_noch_Abweichung()
    {
        var auswertungen = new TestAuswertungsrepository().MitBestand(BoardId, KartenklasseId, Karte(13, TimeSpan.FromHours(2), null));
        var dienst = Dienst(auswertungen);

        var zeile = dienst.SollIst(BoardId, KartenklasseId).Wert.Zeilen.Single();

        Assert.Multiple(() =>
        {
            Assert.That(zeile.Sollband, Is.Null);
            Assert.That(zeile.Abweichung, Is.Null);
        });
    }

    // Ein Bestand ohne Karten ist eine Antwort und kein Fehler.
    [Test]
    public void Wenn_der_Bestand_keine_Karte_hat_dann_kommt_eine_leere_Auswertung_und_keine_Zurueckweisung()
    {
        var dienst = Dienst(new TestAuswertungsrepository());

        var ergebnis = dienst.SollIst(BoardId, KartenklasseId);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Zeilen, Is.Empty);
            Assert.That(ergebnis.Wert.Summe.ErfassteZeit, Is.EqualTo(TimeSpan.Zero));
            Assert.That(ergebnis.Wert.Summe.Sollband, Is.Null);
            Assert.That(ergebnis.Wert.Summe.KartenOhneSoll, Is.Zero);
        });
    }

    [Test]
    public void Wenn_es_das_Board_nicht_gibt_dann_nennt_der_Befund_seine_Nummer_und_den_Weg_zur_Boardliste()
    {
        var auswertungen = new TestAuswertungsrepository();
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.SollIst(999, KartenklasseId);

        Assert.That(ergebnis.IstErfolg, Is.False);
        var befund = EinzigerBefund(ergebnis.Befunde);
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("board-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/boards"));
            Assert.That(auswertungen.WurdeGelesen, Is.False, "Der Bestand eines unbekannten Boards wurde gelesen.");
        });
    }

    [Test]
    public void Wenn_es_die_Kartenklasse_nicht_gibt_dann_nennt_der_Befund_ihre_Nummer_und_den_Weg_zur_Klassenliste()
    {
        var auswertungen = new TestAuswertungsrepository();
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.SollIst(BoardId, 999);

        Assert.That(ergebnis.IstErfolg, Is.False);
        var befund = EinzigerBefund(ergebnis.Befunde);
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kartenklasse-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain($"GET /api/boards/{BoardId}/kartenklassen"));
            Assert.That(auswertungen.WurdeGelesen, Is.False, "Der Bestand einer unbekannten Kartenklasse wurde gelesen.");
        });
    }

    // „Gibt es, nur nicht hier": ein eigener Code, weil die Kompensation eine andere ist.
    [Test]
    public void Wenn_die_Kartenklasse_einem_fremden_Board_gehoert_dann_sagt_der_Befund_wem()
    {
        var kartenklassen = TestKartenklassenRepository.MitKartenklassen(BoardId, ("WBS", "WBS-")).MitZusaetzlichemBoard(FremdesBoard);
        kartenklassen.LegeAn(FremdesBoard, new KartenklasseAnlegenAnfrage("Bugs", "BUG-"));
        var fremdeKartenklasseId = kartenklassen.Kartenklassen(FremdesBoard).Single().KartenklasseId;
        var auswertungen = new TestAuswertungsrepository();
        var dienst = new AuswertungsService(auswertungen, kartenklassen);

        var ergebnis = dienst.SollIst(BoardId, fremdeKartenklasseId);

        Assert.That(ergebnis.IstErfolg, Is.False);
        var befund = EinzigerBefund(ergebnis.Befunde);
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kartenklasse-fremd"));
            Assert.That(befund.Meldung, Does.Contain(FremdesBoard.ToString(CultureInfo.InvariantCulture)));
            Assert.That(auswertungen.WurdeGelesen, Is.False);
        });
    }

    // Eine archivierte Karte steht mit im Bestand — ihre Zeit wurde geleistet.
    [Test]
    public void Wenn_eine_Karte_archiviert_ist_dann_steht_sie_markiert_in_der_Auswertung_und_ihre_Zeit_in_der_Summe()
    {
        var auswertungen = new TestAuswertungsrepository().MitBestand(
            BoardId,
            KartenklasseId,
            new SollIstKarte(11, "WBS-01", "[I0001] Board anlegen", TimeSpan.FromHours(3), null, IstArchiviert: true));
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.SollIst(BoardId, KartenklasseId);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Zeilen.Single().IstArchiviert, Is.True);
            Assert.That(ergebnis.Wert.Summe.ErfassteZeit, Is.EqualTo(TimeSpan.FromHours(3)));
        });
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

    private static SollIstKarte Karte(long karteId, TimeSpan erfassteZeit, Zeitband? sollband)
    {
        return new SollIstKarte(karteId, $"WBS-{karteId:D2}", $"[I00{karteId}] Knoten", erfassteZeit, sollband, IstArchiviert: false);
    }

    private static SollIstKarte[] VierzigKarten()
    {
        var karten = new List<SollIstKarte>();
        for (var nummer = 1; nummer <= 40; nummer++)
        {
            karten.Add(Karte(nummer, TimeSpan.FromMinutes(nummer), new Zeitband(0.4m, 1.5m)));
        }

        return karten.ToArray();
    }
}
