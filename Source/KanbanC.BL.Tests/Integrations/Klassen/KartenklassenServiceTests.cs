using KanbanC.BL.Integrations.Klassen;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.Integrations.Klassen;

public class KartenklassenServiceTests
{
    private const long BoardId = 2;

    // Die erste angelegte Kartenklasse des Testrepositorys traegt immer die 1.
    private const long WbsKartenklasseId = 1;

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_liefert_das_Anlegen_null()
    {
        var repository = TestKartenklassenRepository.MitBoardOhneKartenklassen(BoardId);
        var dienst = new KartenklassenService(repository);

        var ergebnis = dienst.LegeKartenklasseAn(999, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis, Is.Null);
        Assert.That(repository.WurdeAngelegt, Is.False);
    }

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_liefert_der_Abruf_null()
    {
        var repository = TestKartenklassenRepository.MitBoardOhneKartenklassen(BoardId);
        var dienst = new KartenklassenService(repository);

        var kartenklassen = dienst.LadeKartenklassen(999);

        Assert.That(kartenklassen, Is.Null);
    }

    // Ein Board ohne Kartenklasse ist kein Fehler: die leere Liste ist die Antwort, nicht null.
    [Test]
    public void Wenn_das_Board_noch_keine_Kartenklasse_hat_dann_liefert_der_Abruf_eine_leere_Liste()
    {
        var repository = TestKartenklassenRepository.MitBoardOhneKartenklassen(BoardId);
        var dienst = new KartenklassenService(repository);

        var kartenklassen = dienst.LadeKartenklassen(BoardId);

        Assert.That(kartenklassen, Is.Not.Null);
        Assert.That(kartenklassen, Is.Empty);
    }

    [Test]
    public void Wenn_die_Angaben_stimmen_dann_reicht_der_Dienst_die_angelegte_Kartenklasse_durch()
    {
        var repository = TestKartenklassenRepository.MitBoardOhneKartenklassen(BoardId);
        var dienst = new KartenklassenService(repository);

        var ergebnis = dienst.LegeKartenklasseAn(BoardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Name, Is.EqualTo("WBS"));
            Assert.That(ergebnis.Wert.Praefix, Is.EqualTo("WBS-"));
            Assert.That(ergebnis.Wert.Zaehlerstand, Is.EqualTo(0));
        });
    }

    [Test]
    public void Wenn_die_Anfrage_zurueckgewiesen_wird_dann_wurde_nicht_geschrieben()
    {
        var repository = TestKartenklassenRepository.MitBoardOhneKartenklassen(BoardId);
        var dienst = new KartenklassenService(repository);

        var ergebnis = dienst.LegeKartenklasseAn(BoardId, new KartenklasseAnlegenAnfrage("", "WBS-"));

        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.That(repository.WurdeAngelegt, Is.False);
        Assert.That(repository.Kartenklassen(BoardId), Is.Empty);
    }

    // Die vergebenen Praefixe kommen aus derselben Liste, die der Abruf liefert — nicht aus einer
    // zweiten Quelle.
    [Test]
    public void Wenn_das_Praefix_im_Bestand_des_Boards_steht_dann_weist_der_Dienst_die_Anfrage_zurueck()
    {
        var repository = TestKartenklassenRepository.MitKartenklassen(BoardId, ("WBS", "WBS-"));
        var dienst = new KartenklassenService(repository);

        var ergebnis = dienst.LegeKartenklasseAn(BoardId, new KartenklasseAnlegenAnfrage("Arbeitspakete", "wbs-"));

        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kartenklasse-praefix-vergeben"));
        Assert.That(repository.WurdeAngelegt, Is.False);
    }

    [Test]
    public void Wenn_dasselbe_Praefix_auf_einem_zweiten_Board_angelegt_wird_dann_geht_es_durch()
    {
        var repository = TestKartenklassenRepository.MitKartenklassen(BoardId, ("WBS", "WBS-")).MitZusaetzlichemBoard(7);
        var dienst = new KartenklassenService(repository);

        var ergebnis = dienst.LegeKartenklasseAn(7, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.That(repository.Kartenklassen(7).Select(kartenklasse => kartenklasse.Praefix), Is.EqualTo(new[] { "WBS-" }));
    }

    [Test]
    public void Wenn_drei_Kartenklassen_angelegt_wurden_dann_stehen_sie_in_Anlagereihenfolge()
    {
        var repository = TestKartenklassenRepository.MitBoardOhneKartenklassen(BoardId);
        var dienst = new KartenklassenService(repository);
        dienst.LegeKartenklasseAn(BoardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));
        dienst.LegeKartenklasseAn(BoardId, new KartenklasseAnlegenAnfrage("Bugmeldungen", "BUG-"));
        dienst.LegeKartenklasseAn(BoardId, new KartenklasseAnlegenAnfrage("Beschaffung", "BES-"));

        var kartenklassen = dienst.LadeKartenklassen(BoardId);

        Assert.That(kartenklassen!.Select(kartenklasse => kartenklasse.Name), Is.EqualTo(new[] { "WBS", "Bugmeldungen", "Beschaffung" }));
    }

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_meldet_der_Kartenabruf_board_unbekannt()
    {
        var repository = TestKartenklassenRepository.MitKartenklassen(BoardId, ("WBS", "WBS-"));
        var dienst = new KartenklassenService(repository);

        var ergebnis = dienst.LadeKartenDerKartenklasse(999, WbsKartenklasseId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
        Assert.That(repository.WurdenKartenGelesen, Is.False);
    }

    [Test]
    public void Wenn_es_die_Kartenklasse_nirgends_gibt_dann_meldet_der_Kartenabruf_kartenklasse_unbekannt()
    {
        var repository = TestKartenklassenRepository.MitKartenklassen(BoardId, ("WBS", "WBS-"));
        var dienst = new KartenklassenService(repository);

        var ergebnis = dienst.LadeKartenDerKartenklasse(BoardId, 999, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kartenklasse-unbekannt"));
        Assert.That(repository.WurdenKartenGelesen, Is.False);
    }

    // Die Unterscheidung ist der ganze Zweck des zweiten Codes: es gibt sie, nur nicht hier.
    [Test]
    public void Wenn_die_Kartenklasse_einem_anderen_Board_gehoert_dann_meldet_der_Kartenabruf_kartenklasse_fremd()
    {
        var repository = TestKartenklassenRepository.MitKartenklassen(BoardId, ("WBS", "WBS-")).MitZusaetzlichemBoard(7);
        var dienst = new KartenklassenService(repository);

        var ergebnis = dienst.LadeKartenDerKartenklasse(7, WbsKartenklasseId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kartenklasse-fremd"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("7").And.Contain($"{BoardId}"));
        });
        Assert.That(repository.WurdenKartenGelesen, Is.False);
    }

    [Test]
    public void Wenn_die_Kartenklasse_Karten_traegt_dann_reicht_der_Dienst_sie_unveraendert_durch()
    {
        var repository = TestKartenklassenRepository.MitKartenklassen(BoardId, ("WBS", "WBS-"))
            .MitKartenDerKartenklasse(WbsKartenklasseId, Klassenkarte("Erste", "WBS-01"), Klassenkarte("Zweite", "WBS-02"));
        var dienst = new KartenklassenService(repository);

        var ergebnis = dienst.LadeKartenDerKartenklasse(BoardId, WbsKartenklasseId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Select(klassenkarte => klassenkarte.Karte.Kartennummer), Is.EqualTo(new[] { "WBS-01", "WBS-02" }));
    }

    // Eine Kartenklasse ohne Karten ist kein Fehler: die leere Liste ist die Antwort.
    [Test]
    public void Wenn_die_Kartenklasse_noch_keine_Karte_traegt_dann_liefert_der_Dienst_eine_leere_Liste()
    {
        var repository = TestKartenklassenRepository.MitKartenklassen(BoardId, ("WBS", "WBS-"));
        var dienst = new KartenklassenService(repository);

        var ergebnis = dienst.LadeKartenDerKartenklasse(BoardId, WbsKartenklasseId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert, Is.Empty);
    }

    // Der Rennfall: zwischen der Zugehörigkeitsprüfung des Dienstes und dem Lesen der Karten
    // fällt die Kartenklasse weg. Ohne den Befund käme eine leere Liste als Erfolg heraus.
    [Test]
    public void Wenn_die_Kartenklasse_zwischen_Pruefung_und_Lesen_verschwindet_dann_meldet_der_Abruf_kartenklasse_unbekannt()
    {
        var repository = TestKartenklassenRepository.MitKartenklassen(BoardId, ("WBS", "WBS-")).MitVerschwundenerKartenklasse();
        var dienst = new KartenklassenService(repository);

        var ergebnis = dienst.LadeKartenDerKartenklasse(BoardId, WbsKartenklasseId, new Archivierung(false));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kartenklasse-unbekannt"));
        Assert.That(repository.WurdenKartenGelesen, Is.True);
    }

    private static Klassenkarte Klassenkarte(string titel, string kartennummer)
    {
        var karte = new Karte(1, titel, 1, null, null, null, Kartenfarbe.Ohne, null, kartennummer);
        return new Klassenkarte(karte, Spalte: 5, Spaltenbezeichnung: "Rückstand");
    }
}
