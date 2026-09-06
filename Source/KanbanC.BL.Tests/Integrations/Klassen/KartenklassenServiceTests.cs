using KanbanC.BL.Integrations.Klassen;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.Integrations.Klassen;

public class KartenklassenServiceTests
{
    private const long BoardId = 2;

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
}
