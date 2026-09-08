using KanbanC.BL.Integrations.Rohdaten;
using KanbanC.BL.Models;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Tests.Integrations.Rohdaten;

// Der Dienst rechnet nichts — das ist die Regel dieses Slice. Was er entscheidet, ist die eine
// Vorprüfung: gibt es dieses Board, und heißt „nichts da" hier 200 oder 404.
public class RohdatenServiceTests
{
    private const long BoardId = 2;
    private const long UnbekanntesBoard = 999;
    private static readonly DateTimeOffset Beginn = new(2026, 9, 6, 14, 2, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Ende = new(2026, 9, 6, 14, 50, 0, TimeSpan.Zero);

    [Test]
    public void Wenn_die_Karten_gelesen_werden_dann_traegt_jede_Ort_Archivmarke_Kartenklasse_und_ihre_fuenf_Listen()
    {
        var rohdaten = new TestRohdatenRepository().MitKarten(BoardId, KarteMitAllenFuenfListen());
        var dienst = new RohdatenService(rohdaten);

        var ergebnis = dienst.Karten(BoardId);

        Assert.That(ergebnis.IstErfolg, Is.True);
        var karte = ergebnis.Wert.Single();
        Assert.Multiple(() =>
        {
            Assert.That(karte.Karte.KarteId, Is.EqualTo(24));
            Assert.That(karte.Spalte, Is.EqualTo(7));
            Assert.That(karte.Spaltenbezeichnung, Is.EqualTo("Erledigt"));
            Assert.That(karte.Archivstand, Is.EqualTo(new Archivierung(false)));
            Assert.That(karte.Kartenklasse!.Praefix, Is.EqualTo("WBS"));
            Assert.That(karte.Etiketten, Is.EqualTo(new[] { "Rohdaten" }));
            Assert.That(karte.Teilaufgaben, Has.Count.EqualTo(1));
            Assert.That(karte.Kommentare, Has.Count.EqualTo(1));
            Assert.That(karte.Anhaenge, Has.Count.EqualTo(1));
            Assert.That(karte.Dateiverweise, Has.Count.EqualTo(1));
        });
    }

    // Die Antwort ist eine flache Liste ohne Hülle: es wird nichts gekürzt, also **ist** ihre
    // Länge die Zahl. Ein Zählfeld daneben wäre eine zweite Wahrheit.
    [Test]
    public void Wenn_das_Board_vierundzwanzig_Karten_fuehrt_dann_stehen_alle_in_der_flachen_Liste()
    {
        var rohdaten = new TestRohdatenRepository().MitKarten(BoardId, VierundzwanzigKarten());
        var dienst = new RohdatenService(rohdaten);

        var ergebnis = dienst.Karten(BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert, Has.Count.EqualTo(24));
            Assert.That(rohdaten.Lesevorgaenge, Is.EqualTo(1), "Der Bestand wurde mehr als einmal gelesen.");
        });
    }

    // Ein Board ohne Karten ist kein Fehler: die leere Liste ist die Antwort, nicht 404 — wie bei
    // soll-ist.
    [Test]
    public void Wenn_das_Board_keine_Karte_fuehrt_dann_kommt_die_leere_Liste_und_keine_Zurueckweisung()
    {
        var dienst = new RohdatenService(new TestRohdatenRepository().MitLeeremBoard(BoardId));

        var ergebnis = dienst.Karten(BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(ergebnis.Wert, Is.Empty);
        });
    }

    [Test]
    public void Wenn_es_das_Board_der_Karten_nicht_gibt_dann_nennt_der_Befund_seine_Nummer_und_den_Weg_zur_Boardliste()
    {
        var dienst = new RohdatenService(new TestRohdatenRepository());

        var ergebnis = dienst.Karten(UnbekanntesBoard);

        Assert.That(ergebnis.IstErfolg, Is.False);
        var befund = EinzigerBefund(ergebnis.Befunde);
        Befundpruefung.ErwarteVollstaendigenBefund(befund, "board-unbekannt");
        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    [Test]
    public void Wenn_die_Zeiten_gelesen_werden_dann_kommen_laufende_und_abgeschlossene_Eintraege_unveraendert_zurueck()
    {
        var rohdaten = new TestRohdatenRepository().MitZeiteintraegen(BoardId, AbgeschlossenerEintrag(), LaufenderEintrag());
        var dienst = new RohdatenService(rohdaten);

        var ergebnis = dienst.Zeiten(BoardId);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert, Has.Count.EqualTo(2));
            Assert.That(ergebnis.Wert[0].Ende, Is.EqualTo(Ende));
            Assert.That(ergebnis.Wert[1].Ende, Is.Null);
        });
    }

    [Test]
    public void Wenn_das_Board_keinen_Zeiteintrag_fuehrt_dann_kommt_die_leere_Liste_und_keine_Zurueckweisung()
    {
        var dienst = new RohdatenService(new TestRohdatenRepository().MitLeeremBoard(BoardId));

        var ergebnis = dienst.Zeiten(BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.True);
            Assert.That(ergebnis.Wert, Is.Empty);
        });
    }

    [Test]
    public void Wenn_es_das_Board_der_Zeiten_nicht_gibt_dann_nennt_der_Befund_seine_Nummer_und_den_Weg_zur_Boardliste()
    {
        var dienst = new RohdatenService(new TestRohdatenRepository());

        var ergebnis = dienst.Zeiten(UnbekanntesBoard);

        Assert.That(ergebnis.IstErfolg, Is.False);
        var befund = EinzigerBefund(ergebnis.Befunde);
        Befundpruefung.ErwarteVollstaendigenBefund(befund, "board-unbekannt");
        Assert.That(befund.Meldung, Does.Contain("999"));
    }

    private static Fehlerbefund EinzigerBefund(Pruefbefunde befunde)
    {
        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        return befunde[0];
    }

    private static Rohdatenkarte KarteMitAllenFuenfListen()
    {
        var stefan = new Kontributor(1, "Stefan", Kontributorart.Mensch, null);
        return new Rohdatenkarte(
            Karte(24, "Rohdaten über die API abrufen"),
            7,
            "Erledigt",
            new Archivierung(false),
            new Kartenklasse(1, "WBS", "WBS", 24),
            ["Rohdaten"],
            [new Teilaufgabe(1, "Zwei Routen", 1, false)],
            [new Kommentar(1, "Vollständig oder sichtbar gescheitert.", stefan, Beginn)],
            [new Anhang(1, "bericht.pdf", 2048, stefan, Beginn)],
            [new Dateiverweis(1, @"\\ablage\bericht.pdf", stefan, Beginn)]);
    }

    private static Rohdatenkarte[] VierundzwanzigKarten()
    {
        var karten = new List<Rohdatenkarte>();
        foreach (var nummer in Enumerable.Range(1, 24))
        {
            karten.Add(new Rohdatenkarte(
                Karte(nummer, $"Karte {nummer}"),
                7,
                "Erledigt",
                new Archivierung(false),
                null,
                [],
                [],
                [],
                [],
                []));
        }

        return karten.ToArray();
    }

    private static Karte Karte(long karteId, string titel)
    {
        return new Karte(karteId, titel, 1, null, null, null, Kartenfarbe.Ohne, null, null);
    }

    private static Zeiteintrag AbgeschlossenerEintrag()
    {
        return new Zeiteintrag(1, 24, new Kontributor(1, "Stefan", Kontributorart.Mensch, null), Beginn, Ende);
    }

    private static Zeiteintrag LaufenderEintrag()
    {
        return new Zeiteintrag(2, 22, new Kontributor(1, "Stefan", Kontributorart.Mensch, null), Ende, null);
    }
}
