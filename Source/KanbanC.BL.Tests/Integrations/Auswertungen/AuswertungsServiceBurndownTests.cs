using System.Globalization;
using KanbanC.BL.Integrations.Auswertungen;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.Integrations.Auswertungen;

// Der Dienst verdrahtet nur: lesen, Achse bestimmen, rechnen, zusammensetzen. Die Uhr liest er
// selbst — die Achse endet deshalb an dem Tag, an dem der Test läuft.
public class AuswertungsServiceBurndownTests
{
    private const long BoardId = 4;
    private const long FremdesBoard = 7;
    private const long KartenklasseId = 1;

    [Test]
    public void Wenn_der_Bestand_gerechnet_wird_dann_endet_die_Achse_heute()
    {
        var auswertungen = new TestAuswertungsrepository().MitErledigungsstaenden(BoardId, KartenklasseId, Erledigt(11, Heute().AddDays(-4)));
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Burndown(BoardId, KartenklasseId, seit: null);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Tage[^1].Tag, Is.EqualTo(Heute()));
            Assert.That(ergebnis.Wert.Tage, Has.Count.EqualTo(5));
        });
    }

    // Das durchgehende Rechenbeispiel, gegen die heutige Uhr gerechnet: vier Tage zurück, eine
    // Karte am ersten Tag, zwei am dritten, eine ohne Datum, eine mit künftigem Datum.
    [Test]
    public void Wenn_die_fuenf_Karten_des_Rechenbeispiels_gerechnet_werden_dann_lautet_die_Reihe_4_4_2_2_2()
    {
        var auswertungen = new TestAuswertungsrepository().MitErledigungsstaenden(BoardId, KartenklasseId, Rechenbeispiel());
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Burndown(BoardId, KartenklasseId, seit: null);

        Assert.That(ergebnis.Wert.Tage.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 4, 4, 2, 2, 2 }));
    }

    [Test]
    public void Wenn_die_fuenf_Karten_des_Rechenbeispiels_gerechnet_werden_dann_lauten_die_Kopfzahlen_zwei_drei_fuenf()
    {
        var auswertungen = new TestAuswertungsrepository().MitErledigungsstaenden(BoardId, KartenklasseId, Rechenbeispiel());
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Burndown(BoardId, KartenklasseId, seit: null);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Kopfzahlen.Offen, Is.EqualTo(2));
            Assert.That(ergebnis.Wert.Kopfzahlen.Erledigt, Is.EqualTo(3));
            Assert.That(ergebnis.Wert.Kopfzahlen.ImBestand, Is.EqualTo(5));
            Assert.That(ergebnis.Wert.Kopfzahlen.OhneErledigungsdatum, Is.EqualTo(1));
        });
    }

    [Test]
    public void Wenn_ein_Zeitraum_gewaehlt_ist_dann_ist_die_Reihe_kuerzer_und_die_Kopfzahlen_bleiben()
    {
        var auswertungen = new TestAuswertungsrepository().MitErledigungsstaenden(BoardId, KartenklasseId, Rechenbeispiel());
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Burndown(BoardId, KartenklasseId, Heute().AddDays(-2));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Tage.Select(tag => tag.OffeneKarten), Is.EqualTo(new[] { 2, 2, 2 }));
            Assert.That(ergebnis.Wert.Kopfzahlen.ImBestand, Is.EqualTo(5));
            Assert.That(ergebnis.Wert.Kopfzahlen.Offen, Is.EqualTo(2));
        });
    }

    // Ein Bestand ohne Karten ist kein Fehler: die Reihe über den einen Tag heute ist die Antwort.
    [Test]
    public void Wenn_der_Bestand_keine_Karte_fuehrt_dann_kommt_die_Reihe_ueber_den_einen_Tag_heute()
    {
        var dienst = Dienst(new TestAuswertungsrepository());

        var ergebnis = dienst.Burndown(BoardId, KartenklasseId, seit: null);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Tage, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Wert.Tage[0].OffeneKarten, Is.Zero);
            Assert.That(ergebnis.Wert.Kopfzahlen.ImBestand, Is.Zero);
            Assert.That(ergebnis.Wert.Kopfzahlen.Offen, Is.Zero);
            Assert.That(ergebnis.Wert.Kopfzahlen.Erledigt, Is.Zero);
        });
    }

    // Ein Lesevorgang je Bestand und nicht je Karte.
    [Test]
    public void Wenn_der_Bestand_vierzig_Karten_hat_dann_wird_er_genau_einmal_gelesen()
    {
        var auswertungen = new TestAuswertungsrepository().MitErledigungsstaenden(BoardId, KartenklasseId, VierzigKarten());
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Burndown(BoardId, KartenklasseId, seit: null);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Kopfzahlen.ImBestand, Is.EqualTo(40));
            Assert.That(auswertungen.Lesevorgaenge, Is.EqualTo(1));
        });
    }

    [Test]
    public void Wenn_es_das_Board_nicht_gibt_dann_wird_der_Bestand_gar_nicht_erst_gelesen()
    {
        var auswertungen = new TestAuswertungsrepository();
        var dienst = Dienst(auswertungen);

        var ergebnis = dienst.Burndown(999, KartenklasseId, seit: null);

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

        var ergebnis = dienst.Burndown(BoardId, 999, seit: null);

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

        var ergebnis = dienst.Burndown(BoardId, fremdeKartenklasseId, seit: null);

        Assert.That(ergebnis.IstErfolg, Is.False);
        var befund = EinzigerBefund(ergebnis.Befunde);
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("kartenklasse-fremd"));
            Assert.That(befund.Meldung, Does.Contain(FremdesBoard.ToString(CultureInfo.InvariantCulture)));
            Assert.That(auswertungen.WurdeGelesen, Is.False);
        });
    }

    private static Erledigungsstandkarte[] Rechenbeispiel()
    {
        return
        [
            Erledigt(1, Heute().AddDays(-4)),
            Erledigt(2, Heute().AddDays(-2)),
            Erledigt(3, Heute().AddDays(-2)),
            new Erledigungsstandkarte(4, "WBS-04", "[I0004] Board archivieren", null, IstArchiviert: true, StehtInAbschlussspalte: false),
            Erledigt(5, Heute().AddDays(2)),
        ];
    }

    private static Erledigungsstandkarte[] VierzigKarten()
    {
        var karten = new List<Erledigungsstandkarte>();
        for (var nummer = 1; nummer <= 40; nummer++)
        {
            karten.Add(Erledigt(nummer, Heute()));
        }

        return karten.ToArray();
    }

    private static Erledigungsstandkarte Erledigt(long karteId, DateOnly erledigtAm)
    {
        return new Erledigungsstandkarte(karteId, $"WBS-{karteId:D2}", $"[I00{karteId}] Knoten", erledigtAm, IstArchiviert: false, StehtInAbschlussspalte: true);
    }

    private static DateOnly Heute()
    {
        return DateOnly.FromDateTime(DateTime.Today);
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
