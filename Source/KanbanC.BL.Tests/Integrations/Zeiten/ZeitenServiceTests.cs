using KanbanC.BL.Integrations.Zeiten;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Tests.Integrations.Zeiten;

// Der Dienst prüft in der Reihenfolge, in der die Kompensationen ausführbar sind: erst den
// Kontributor, dann die Karte. Beide Regeln brauchen den Bestand — deshalb sitzen sie hier und
// nicht in einem Validator, den es für diesen Slice nicht gibt.
public class ZeitenServiceTests
{
    [Test]
    public void Wenn_der_Start_gelingt_dann_reicht_der_Dienst_den_Eintrag_ohne_Ende_durch()
    {
        var zeitenRepository = TestZeitenRepository.Leer();
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"));

        var ergebnis = service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3));

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Zeiteintrag.Karte, Is.EqualTo(14));
            Assert.That(ergebnis.Wert.Zeiteintrag.Kontributor.KontributorId, Is.EqualTo(3));
            Assert.That(ergebnis.Wert.Zeiteintrag.Ende, Is.Null);
            Assert.That(zeitenRepository.Schreibzugriffe, Is.EqualTo(1));
        });
    }

    // Der Kontributor reist als **ganzer** Kontributor und nicht als nackte Nummer — dieselbe
    // Gestalt wie Kommentar.Urheber.
    [Test]
    public void Wenn_der_Start_gelingt_dann_traegt_der_Eintrag_den_ganzen_Kontributor()
    {
        var service = new ZeitenService(TestZeitenRepository.Leer(), MitKontributor(3, "Stefan"));

        var ergebnis = service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3));

        Assert.That(ergebnis.Wert.Zeiteintrag.Kontributor.Name, Is.EqualTo("Stefan"));
        Assert.That(ergebnis.Wert.Zeiteintrag.Kontributor.Art, Is.EqualTo(Kontributorart.Mensch));
    }

    // IstNeu kommt aus dem Repository und wird **nicht** abgeleitet: einem Eintrag ist nicht
    // anzusehen, ob dieser Aufruf ihn angelegt hat.
    [Test]
    public void Wenn_derselbe_Kontributor_auf_derselben_Karte_ein_zweites_Mal_startet_dann_meldet_der_Dienst_denselben_Eintrag_als_nicht_neu()
    {
        var zeitenRepository = TestZeitenRepository.Leer();
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"));
        var erster = service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3));

        var zweiter = service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3));

        Assert.Multiple(() =>
        {
            Assert.That(erster.Wert.IstNeu, Is.True);
            Assert.That(zweiter.Wert.IstNeu, Is.False);
            Assert.That(zweiter.Wert.Zeiteintrag.ZeiteintragId, Is.EqualTo(erster.Wert.Zeiteintrag.ZeiteintragId));
            Assert.That(zweiter.Wert.Zeiteintrag.Beginn, Is.EqualTo(erster.Wert.Zeiteintrag.Beginn));
            Assert.That(zeitenRepository.Schreibzugriffe, Is.EqualTo(1));
        });
    }

    // Die auffälligste Entscheidung des Slice: die Obergrenze ist das Paar (Karte, Kontributor),
    // nicht der Kontributor.
    [Test]
    public void Wenn_derselbe_Kontributor_auf_einer_zweiten_Karte_startet_dann_entsteht_ein_zweiter_laufender_Eintrag()
    {
        var zeitenRepository = TestZeitenRepository.Leer();
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"));
        service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3));

        var zweiter = service.StarteZeitmessung(21, new ZeitmessungStartenAnfrage(3));

        Assert.Multiple(() =>
        {
            Assert.That(zweiter.Wert.IstNeu, Is.True);
            Assert.That(zeitenRepository.Schreibzugriffe, Is.EqualTo(2));
            Assert.That(zeitenRepository.Zeiteintraege.Count(eintrag => eintrag.Ende is null), Is.EqualTo(2));
        });
    }

    [Test]
    public void Wenn_ein_zweiter_Kontributor_auf_derselben_Karte_startet_dann_entsteht_ein_zweiter_laufender_Eintrag()
    {
        var zeitenRepository = TestZeitenRepository.Leer();
        var service = new ZeitenService(zeitenRepository, MitZweiKontributoren());
        service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(1));

        var zweiter = service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(2));

        Assert.Multiple(() =>
        {
            Assert.That(zweiter.Wert.IstNeu, Is.True);
            Assert.That(zeitenRepository.Zeiteintraege.Count(eintrag => eintrag.Karte == 14 && eintrag.Ende is null), Is.EqualTo(2));
        });
    }

    [Test]
    public void Wenn_es_den_Kontributor_nicht_gibt_dann_meldet_der_Dienst_kontributor_unbekannt_und_schreibt_nicht()
    {
        var zeitenRepository = TestZeitenRepository.Leer();
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"));

        var ergebnis = service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(999));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(zeitenRepository.Schreibzugriffe, Is.EqualTo(0));
            Assert.That(zeitenRepository.Zeiteintraege, Is.Empty);
        });
    }

    [Test]
    public void Wenn_der_Kontributor_stillgelegt_ist_dann_meldet_der_Dienst_kontributor_stillgelegt_und_schreibt_nicht()
    {
        var zeitenRepository = TestZeitenRepository.Leer();
        var service = new ZeitenService(zeitenRepository, MitStillgelegtemKontributor());

        var ergebnis = service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(1));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kontributor-stillgelegt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Zeit"));
            Assert.That(zeitenRepository.Schreibzugriffe, Is.EqualTo(0));
            Assert.That(zeitenRepository.Zeiteintraege, Is.Empty);
        });
    }

    [Test]
    public void Wenn_es_die_Karte_nicht_gibt_dann_meldet_der_Dienst_karte_unbekannt()
    {
        var service = new ZeitenService(TestZeitenRepository.OhneDieseKarte(), MitKontributor(3, "Stefan"));

        var ergebnis = service.StarteZeitmessung(999, new ZeitmessungStartenAnfrage(3));

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("999"));
        });
    }

    // Die Uhr wird hereingereicht: sonst wäre der Beginn im Test nicht setzbar, und das
    // Repository müsste sie selbst lesen.
    [Test]
    public void Wenn_der_Start_gelingt_dann_reicht_der_Dienst_einen_UTC_Beginn_ins_Repository()
    {
        var zeitenRepository = TestZeitenRepository.Leer();
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"));
        var vorDemAufruf = DateTimeOffset.UtcNow;

        service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3));

        var nachDemAufruf = DateTimeOffset.UtcNow;
        Assert.That(zeitenRepository.ErhaltenerBeginn, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(zeitenRepository.ErhaltenerBeginn!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(zeitenRepository.ErhaltenerBeginn!.Value, Is.InRange(vorDemAufruf, nachDemAufruf));
        });
    }

    private static TestKontributorenRepository MitKontributor(long kontributorId, string name)
    {
        var repository = new TestKontributorenRepository();
        for (var nummer = 1L; nummer <= kontributorId; nummer++)
        {
            repository.LegeAn(new KontributorAnlegenAnfrage(name, Kontributorart.Mensch));
        }

        return repository;
    }

    private static TestKontributorenRepository MitZweiKontributoren()
    {
        var repository = new TestKontributorenRepository();
        repository.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        repository.LegeAn(new KontributorAnlegenAnfrage("Claude-Agent", Kontributorart.Agent));
        return repository;
    }

    private static TestKontributorenRepository MitStillgelegtemKontributor()
    {
        var repository = new TestKontributorenRepository();
        repository.LegeAn(new KontributorAnlegenAnfrage("Maria Lenz", Kontributorart.Mensch));
        repository.SetzeStilllegung(1, new Stilllegung(true));
        return repository;
    }
}
