using KanbanC.BL.Integrations.Boards;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Tests.Integrations.Boards;

public class SpaltenServiceTests
{
    [Test]
    public void Wenn_eine_Spalte_angelegt_wird_dann_liegt_sie_danach_im_Repository()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        var service = new SpaltenService(repository);

        var ergebnis = service.LegeSpalteAn(1, new SpalteAnlegenAnfrage("Eingang", false, null));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Position, Is.EqualTo(1));
        Assert.That(repository.Spalten(1).Select(s => s.Bezeichnung), Is.EqualTo(new[] { "Eingang" }));
    }

    [Test]
    public void Wenn_die_Bezeichnung_leer_ist_dann_erreicht_die_Anfrage_das_Repository_nicht()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        var service = new SpaltenService(repository);

        var ergebnis = service.LegeSpalteAn(1, new SpalteAnlegenAnfrage("   ", false, null));

        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
            Assert.That(repository.WurdeAngelegt, Is.False);
            Assert.That(repository.Spalten(1), Is.Empty);
        });
    }

    [Test]
    public void Wenn_eine_Markierung_ohne_Anzeigegrenze_kommt_dann_entsteht_keine_Spalte()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        var service = new SpaltenService(repository);

        var ergebnis = service.LegeSpalteAn(1, new SpalteAnlegenAnfrage("Abgenommen", true, null));

        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0], Does.Contain("Anzeigegrenze"));
        Assert.That(repository.Spalten(1), Is.Empty);
    }

    [Test]
    public void Wenn_die_BoardId_unbekannt_ist_dann_liefert_LegeSpalteAn_null()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        var service = new SpaltenService(repository);

        var ergebnis = service.LegeSpalteAn(2, new SpalteAnlegenAnfrage("Eingang", false, null));

        Assert.That(ergebnis, Is.Null);
        Assert.That(repository.Spalten(1), Is.Empty);
    }

    [Test]
    public void Wenn_eine_Spalte_geaendert_wird_dann_traegt_sie_danach_die_neue_Bezeichnung()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        var angelegt = repository.LegeAn(1, new SpalteAnlegenAnfrage("In Arbeit", false, null));
        var service = new SpaltenService(repository);

        var ergebnis = service.AendereSpalte(1, angelegt!.SpalteId, new SpalteAendernAnfrage("In Umsetzung", false, null));

        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Position, Is.EqualTo(1));
        Assert.That(repository.Spalten(1)[0].Bezeichnung, Is.EqualTo("In Umsetzung"));
    }

    [Test]
    public void Wenn_die_Aenderung_ungueltig_ist_dann_bleibt_die_Spalte_unveraendert()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        var angelegt = repository.LegeAn(1, new SpalteAnlegenAnfrage("In Arbeit", false, null));
        var service = new SpaltenService(repository);

        var ergebnis = service.AendereSpalte(1, angelegt!.SpalteId, new SpalteAendernAnfrage("", true, null));

        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(2));
            Assert.That(repository.WurdeGeaendert, Is.False);
            Assert.That(repository.Spalten(1)[0].Bezeichnung, Is.EqualTo("In Arbeit"));
        });
    }

    [Test]
    public void Wenn_die_SpalteId_zu_einem_anderen_Board_gehoert_dann_liefert_AendereSpalte_null()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        repository.LegeAn(1, new SpalteAnlegenAnfrage("In Arbeit", false, null));
        var service = new SpaltenService(repository);

        var ergebnis = service.AendereSpalte(1, 99, new SpalteAendernAnfrage("Gekapert", false, null));

        Assert.That(ergebnis, Is.Null);
        Assert.That(repository.Spalten(1)[0].Bezeichnung, Is.EqualTo("In Arbeit"));
    }

    [Test]
    public void Wenn_die_Reihenfolge_vollstaendig_ist_dann_liegen_die_Spalten_danach_in_der_neuen_Ordnung()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        var erste = repository.LegeAn(1, new SpalteAnlegenAnfrage("Zu erledigen", false, null));
        var zweite = repository.LegeAn(1, new SpalteAnlegenAnfrage("In Arbeit", false, null));
        var service = new SpaltenService(repository);

        var ergebnis = service.SetzeReihenfolge(1, [zweite!.SpalteId, erste!.SpalteId]);

        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Select(s => s.Bezeichnung), Is.EqualTo(new[] { "In Arbeit", "Zu erledigen" }));
        Assert.That(repository.Spalten(1).Select(s => s.Position), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Wenn_die_Reihenfolge_unvollstaendig_ist_dann_erreicht_sie_das_Repository_nicht()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        var erste = repository.LegeAn(1, new SpalteAnlegenAnfrage("Zu erledigen", false, null));
        repository.LegeAn(1, new SpalteAnlegenAnfrage("In Arbeit", false, null));
        var service = new SpaltenService(repository);

        var ergebnis = service.SetzeReihenfolge(1, [erste!.SpalteId]);

        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(repository.WurdeUmsortiert, Is.False);
            Assert.That(repository.Spalten(1).Select(s => s.Bezeichnung), Is.EqualTo(new[] { "Zu erledigen", "In Arbeit" }));
        });
    }

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_liefert_SetzeReihenfolge_null()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        var service = new SpaltenService(repository);

        var ergebnis = service.SetzeReihenfolge(2, [1]);

        Assert.That(ergebnis, Is.Null);
        Assert.That(repository.WurdeUmsortiert, Is.False);
    }

    [Test]
    public void Wenn_eine_Spalte_entfernt_wird_dann_liegt_sie_danach_nicht_mehr_im_Repository()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        var erste = repository.LegeAn(1, new SpalteAnlegenAnfrage("Zu erledigen", false, null));
        repository.LegeAn(1, new SpalteAnlegenAnfrage("In Arbeit", false, null));
        var service = new SpaltenService(repository);

        var wurdeEntfernt = service.EntferneSpalte(1, erste!.SpalteId);

        Assert.That(wurdeEntfernt, Is.True);
        Assert.That(repository.Spalten(1).Select(s => s.Bezeichnung), Is.EqualTo(new[] { "In Arbeit" }));
        Assert.That(repository.Spalten(1)[0].Position, Is.EqualTo(1));
    }

    [Test]
    public void Wenn_die_SpalteId_unbekannt_ist_dann_meldet_EntferneSpalte_false_und_der_Bestand_bleibt()
    {
        var repository = TestSpaltenRepository.MitBoardOhneSpalten(1);
        repository.LegeAn(1, new SpalteAnlegenAnfrage("Zu erledigen", false, null));
        var service = new SpaltenService(repository);

        var wurdeEntfernt = service.EntferneSpalte(1, 99);

        Assert.That(wurdeEntfernt, Is.False);
        Assert.That(repository.Spalten(1), Has.Count.EqualTo(1));
    }
}
