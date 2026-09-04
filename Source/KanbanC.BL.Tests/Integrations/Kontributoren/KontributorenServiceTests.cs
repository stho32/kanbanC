using KanbanC.BL.Integrations.Kontributoren;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.Integrations.Kontributoren;

public class KontributorenServiceTests
{
    [Test]
    public void Wenn_ein_Kontributor_angelegt_wird_dann_erhaelt_das_Repository_die_Anfrage_und_der_Kontributor_bekommt_eine_KontributorId()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);
        var anfrage = new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch);

        var ergebnis = service.LegeKontributorAn(anfrage);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(repository.ErhalteneAnfrage, Is.EqualTo(anfrage));
            Assert.That(ergebnis.Wert, Is.EqualTo(new Kontributor(1, "Stefan", Kontributorart.Mensch, StillgelegtAm: null)));
            Assert.That(service.LadeAlleKontributoren(), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Wenn_alle_drei_Arten_angelegt_werden_dann_kommt_jede_unveraendert_zurueck()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);

        service.LegeKontributorAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        service.LegeKontributorAn(new KontributorAnlegenAnfrage("Codex-Agent", Kontributorart.Agent));
        service.LegeKontributorAn(new KontributorAnlegenAnfrage("Nina Barth", Kontributorart.Abgebildet));

        Assert.That(service.LadeAlleKontributoren().Select(kontributor => kontributor.Art), Is.EqualTo(new[]
        {
            Kontributorart.Mensch,
            Kontributorart.Agent,
            Kontributorart.Abgebildet,
        }));
    }

    [Test]
    public void Wenn_die_Kontributoren_geladen_werden_dann_reicht_der_Service_die_Reihenfolge_des_Repositories_unveraendert_durch()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);
        repository.LegeAn(new KontributorAnlegenAnfrage("stefan", Kontributorart.Mensch));
        repository.LegeAn(new KontributorAnlegenAnfrage("Codex-Agent", Kontributorart.Agent));

        var kontributoren = service.LadeAlleKontributoren();

        Assert.That(kontributoren.Select(kontributor => kontributor.Name), Is.EqualTo(new[] { "stefan", "Codex-Agent" }));
    }

    [Test]
    public void Wenn_die_Anfrage_einen_leeren_Namen_hat_dann_wird_sie_zurueckgewiesen_und_das_Repository_nicht_aufgerufen()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);
        var anfrage = new KontributorAnlegenAnfrage("   ", Kontributorart.Mensch);

        var ergebnis = service.LegeKontributorAn(anfrage);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-name-leer"));
            Assert.That(repository.ErhalteneAnfrage, Is.Null);
            Assert.That(service.LadeAlleKontributoren(), Is.Empty);
        });
        Assert.That(() => ergebnis.Wert, Throws.InvalidOperationException);
    }

    [Test]
    public void Wenn_die_Art_unbekannt_ist_dann_wird_die_Anfrage_zurueckgewiesen_und_nichts_gespeichert()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);

        var ergebnis = service.LegeKontributorAn(new KontributorAnlegenAnfrage("Stefan", (Kontributorart)7));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-art-unbekannt"));
            Assert.That(repository.ErhalteneAnfrage, Is.Null);
            Assert.That(service.LadeAlleKontributoren(), Is.Empty);
        });
    }

    [Test]
    public void Wenn_nach_einer_Zurueckweisung_ein_gueltiger_Name_kommt_dann_entsteht_der_Kontributor()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);
        service.LegeKontributorAn(new KontributorAnlegenAnfrage("", Kontributorart.Mensch));

        var ergebnis = service.LegeKontributorAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(service.LadeAlleKontributoren(), Has.Count.EqualTo(1));
    }

    [Test]
    public void Wenn_ein_Kontributor_geaendert_wird_dann_erhaelt_das_Repository_Nummer_und_Anfrage_und_der_neue_Stand_kommt_zurueck()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);
        var bert = repository.LegeAn(new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));
        var anfrage = new KontributorAendernAnfrage("Zora", Kontributorart.Mensch);

        var ergebnis = service.AendereKontributor(bert.KontributorId, anfrage);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(repository.GeaenderteKontributorId, Is.EqualTo(bert.KontributorId));
            Assert.That(repository.ErhalteneAenderung, Is.EqualTo(anfrage));
            Assert.That(ergebnis.Wert, Is.EqualTo(new Kontributor(1, "Zora", Kontributorart.Mensch, StillgelegtAm: null)));
            Assert.That(service.LadeAlleKontributoren(), Is.EqualTo(new[] { new Kontributor(1, "Zora", Kontributorart.Mensch, StillgelegtAm: null) }));
        });
    }

    [Test]
    public void Wenn_ein_Kontributor_geaendert_wird_dann_bleibt_ein_zweiter_unberuehrt()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);
        var anna = repository.LegeAn(new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));
        var bert = repository.LegeAn(new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));

        service.AendereKontributor(bert.KontributorId, new KontributorAendernAnfrage("Zora", Kontributorart.Mensch));

        Assert.That(service.LadeAlleKontributoren(), Is.EqualTo(new[]
        {
            new Kontributor(anna.KontributorId, "Anna", Kontributorart.Mensch, StillgelegtAm: null),
            new Kontributor(bert.KontributorId, "Zora", Kontributorart.Mensch, StillgelegtAm: null),
        }));
    }

    [Test]
    public void Wenn_die_KontributorId_unbekannt_ist_dann_wird_die_Aenderung_mit_dem_Nichtgefunden_Befund_zurueckgewiesen()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);
        repository.LegeAn(new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));

        var ergebnis = service.AendereKontributor(999, new KontributorAendernAnfrage("Zora", Kontributorart.Mensch));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(service.LadeAlleKontributoren(), Is.EqualTo(new[] { new Kontributor(1, "Bert", Kontributorart.Agent, StillgelegtAm: null) }));
        });
    }

    [Test]
    public void Wenn_die_Aenderung_einen_leeren_Namen_hat_dann_wird_sie_zurueckgewiesen_und_das_Repository_nicht_aufgerufen()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);
        var bert = repository.LegeAn(new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));

        var ergebnis = service.AendereKontributor(bert.KontributorId, new KontributorAendernAnfrage("   ", Kontributorart.Mensch));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-name-leer"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain($"PUT /api/kontributoren/{bert.KontributorId}"));
            Assert.That(repository.ErhalteneAenderung, Is.Null);
            Assert.That(service.LadeAlleKontributoren(), Is.EqualTo(new[] { new Kontributor(1, "Bert", Kontributorart.Agent, StillgelegtAm: null) }));
        });
    }

    [Test]
    public void Wenn_die_Art_der_Aenderung_unbekannt_ist_dann_wird_sie_zurueckgewiesen_und_nichts_geschrieben()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);
        var bert = repository.LegeAn(new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));

        var ergebnis = service.AendereKontributor(bert.KontributorId, new KontributorAendernAnfrage("Zora", (Kontributorart)7));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-art-unbekannt"));
            Assert.That(repository.ErhalteneAenderung, Is.Null);
        });
    }

    [Test]
    public void Wenn_die_KontributorId_unbekannt_und_der_Name_leer_ist_dann_gewinnt_die_Pruefung_vor_dem_Datenzugriff()
    {
        var repository = new TestKontributorenRepository();
        var service = new KontributorenService(repository);

        var ergebnis = service.AendereKontributor(999, new KontributorAendernAnfrage("", Kontributorart.Mensch));

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-name-leer"));
            Assert.That(repository.ErhalteneAenderung, Is.Null);
        });
    }
}
