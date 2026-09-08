using KanbanC.BL.Integrations.Export;
using KanbanC.BL.Models.Export;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Export;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Tests.Integrations.Export;

// Der Dienst rechnet nichts. Was er entscheidet, ist die eine Vorprüfung — gibt es dieses Board? —
// und was er hinzufügt, ist der Kopf, den keine Zeile der Datenbank kennt.
public class BoardexportServiceTests
{
    private const long BoardId = 2;
    private const long UnbekanntesBoard = 999;
    private static readonly DateTimeOffset Beginn = new(2026, 9, 6, 9, 0, 0, TimeSpan.Zero);

    [Test]
    public void Wenn_das_Board_ausgeleitet_wird_dann_traegt_die_Datei_seinen_ganzen_Bestand()
    {
        var dienst = new BoardexportService(new TestBoardexportRepository().MitBestand(BoardId, VollerBestand()));

        var ergebnis = dienst.Datei(BoardId);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Board.Name, Is.EqualTo("KanbanC — Release 2"));
            Assert.That(ergebnis.Wert.Spalten, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Wert.Kartenklassen, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Wert.Kontributoren, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Wert.Karten, Has.Count.EqualTo(2));
            Assert.That(ergebnis.Wert.Zeiteintraege, Has.Count.EqualTo(1));
        });
    }

    // Sollband und Zaehlerstand hängen an **ihrer** Karte und nicht an der Nachbarin.
    [Test]
    public void Wenn_nur_eine_Karte_ein_Sollband_traegt_dann_steht_es_an_ihr_und_nicht_an_der_anderen()
    {
        var dienst = new BoardexportService(new TestBoardexportRepository().MitBestand(BoardId, VollerBestand()));

        var ergebnis = dienst.Datei(BoardId);

        var mitBand = ergebnis.Wert.Karten.Single(karte => karte.Karte.Karte.KarteId == 24);
        var ohneBand = ergebnis.Wert.Karten.Single(karte => karte.Karte.Karte.KarteId == 23);
        Assert.Multiple(() =>
        {
            Assert.That(mitBand.Sollband, Is.EqualTo(new Zeitband(2.0m, 4.0m)));
            Assert.That(mitBand.Zaehlerstand, Is.EqualTo(32));
            Assert.That(mitBand.Verantwortlicher!.Name, Is.EqualTo("Stefan"));
            Assert.That(ohneBand.Sollband, Is.Null);
            Assert.That(ohneBand.Zaehlerstand, Is.Null);
            Assert.That(ohneBand.Verantwortlicher, Is.Null);
        });
    }

    // Der Kopf sagt, was die Datei nicht tragen kann — statt es zu verschweigen.
    [Test]
    public void Wenn_die_Datei_entsteht_dann_nennt_ihr_Kopf_Anwendung_Fassung_Zeitpunkt_und_die_fehlenden_Anhangbytes()
    {
        var dienst = new BoardexportService(new TestBoardexportRepository().MitBestand(BoardId, LeererBestand()));

        var ergebnis = dienst.Datei(BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Kopf.Anwendung, Is.EqualTo("KanbanC"));
            Assert.That(ergebnis.Wert.Kopf.Fassung, Is.EqualTo(1));
            Assert.That(ergebnis.Wert.Kopf.ErzeugtAm, Is.EqualTo(DateTimeOffset.Now).Within(TimeSpan.FromMinutes(1))); // stil-check: C03 die Uhr ist hier der Prüfgegenstand
            Assert.That(ergebnis.Wert.Kopf.Anhanghinweis, Does.Contain("Bytes"));
        });
    }

    // Ein leeres Board ist kein Fehler: die Datei trägt Kopf, Board, Spalten und leere Listen.
    [Test]
    public void Wenn_das_Board_leer_ist_dann_kommt_eine_vollstaendige_Datei_und_keine_Zurueckweisung()
    {
        var dienst = new BoardexportService(new TestBoardexportRepository().MitBestand(BoardId, LeererBestand()));

        var ergebnis = dienst.Datei(BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(ergebnis.Wert.Spalten, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Wert.Karten, Is.Empty);
            Assert.That(ergebnis.Wert.Zeiteintraege, Is.Empty);
            Assert.That(ergebnis.Wert.Kontributoren, Is.Empty);
        });
    }

    [Test]
    public void Wenn_es_das_Board_nicht_gibt_dann_wird_der_Abruf_mit_Nummer_und_Kompensationsaktion_zurueckgewiesen()
    {
        var dienst = new BoardexportService(new TestBoardexportRepository().MitBestand(BoardId, LeererBestand()));

        var ergebnis = dienst.Datei(UnbekanntesBoard);

        var befund = ergebnis.Befunde[0];
        Befundpruefung.ErwarteVollstaendigenBefund(befund, "board-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    // Ein Lesevorgang je Abruf: die Datei entsteht aus **einer** Lesetransaktion.
    [Test]
    public void Wenn_die_Datei_entsteht_dann_wird_der_Bestand_genau_einmal_gelesen()
    {
        var bestand = new TestBoardexportRepository().MitBestand(BoardId, VollerBestand());
        var dienst = new BoardexportService(bestand);

        dienst.Datei(BoardId);

        Assert.That(bestand.Lesevorgaenge, Is.EqualTo(1));
    }

    private static Boardbestand VollerBestand()
    {
        var kartenklasse = new Kartenklasse(1, "WBS", "WBS-", 32);
        var stefan = new Kontributor(7, "Stefan", Kontributorart.Mensch, null);
        return new Boardbestand(
            new Exportboard(BoardId, "KanbanC — Release 2", BoardArt.Projekt, null, new DateOnly(2026, 9, 30), true, false),
            [new Exportspalte(3, "Erledigt", 3, true, 20)],
            [kartenklasse],
            [stefan],
            [
                new Exportkarte(Rohdatenkarte(23, "K23", null, null), null, null, null),
                new Exportkarte(Rohdatenkarte(24, "K24", kartenklasse, stefan.KontributorId), stefan, new Zeitband(2.0m, 4.0m), 32),
            ],
            [new Zeiteintrag(1, 24, stefan, Beginn, null)]);
    }

    private static Boardbestand LeererBestand()
    {
        return new Boardbestand(
            new Exportboard(BoardId, "Frisch", BoardArt.Linie, null, null, false, false),
            [new Exportspalte(1, "Zu erledigen", 1, false, null)],
            [],
            [],
            [],
            []);
    }

    private static Rohdatenkarte Rohdatenkarte(long karteId, string titel, Kartenklasse? kartenklasse, long? kontributor)
    {
        var karte = new Karte(karteId, titel, 1, null, null, null, Kartenfarbe.Ohne, kontributor, null);
        return new Rohdatenkarte(karte, 3, "Erledigt", new Archivierung(false), kartenklasse, [], [], [], [], []);
    }
}
