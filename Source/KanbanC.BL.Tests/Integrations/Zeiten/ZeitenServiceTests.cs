using KanbanC.BL.Integrations.Zeiten;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Tests.Integrations.Zeiten;

// Der Dienst prüft in der Reihenfolge, in der die Kompensationen ausführbar sind: erst den
// Kontributor, dann die Karte. Beide Regeln brauchen den Bestand — deshalb sitzen sie hier und
// nicht in einem Validator, den es für diesen Slice nicht gibt.
public class ZeitenServiceTests
{
    private static readonly DateTimeOffset AchtUhrVier = new(2026, 9, 6, 8, 4, 0, TimeSpan.Zero);

    [Test]
    public void Wenn_der_Start_gelingt_dann_reicht_der_Dienst_den_Eintrag_ohne_Ende_durch()
    {
        var zeitenRepository = TestZeitenRepository.Leer();
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"), MitDieserKarte());

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
        var service = new ZeitenService(TestZeitenRepository.Leer(), MitKontributor(3, "Stefan"), MitDieserKarte());

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
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"), MitDieserKarte());
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
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"), MitDieserKarte());
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
        var service = new ZeitenService(zeitenRepository, MitZweiKontributoren(), MitDieserKarte());
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
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"), MitDieserKarte());

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
        var service = new ZeitenService(zeitenRepository, MitStillgelegtemKontributor(), MitDieserKarte());

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
        var service = new ZeitenService(TestZeitenRepository.OhneDieseKarte(), MitKontributor(3, "Stefan"), MitDieserKarte());

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
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"), MitDieserKarte());
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

    // Der Stopp fragt **keinen** Kontributor: der Eintrag weiß selbst, wem er gehört. Zurück kommt
    // derselbe Eintrag mit gesetztem Ende.
    [Test]
    public void Wenn_der_Stopp_gelingt_dann_reicht_der_Dienst_den_beendeten_Eintrag_mit_unveraendertem_Beginn_und_Kontributor_durch()
    {
        var zeitenRepository = TestZeitenRepository.Leer();
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"), MitDieserKarte());
        var gestarteter = service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3)).Wert.Zeiteintrag;

        var ergebnis = service.BeendeZeitmessung(14, gestarteter.ZeiteintragId);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.ZeiteintragId, Is.EqualTo(gestarteter.ZeiteintragId));
            Assert.That(ergebnis.Wert.Karte, Is.EqualTo(14));
            Assert.That(ergebnis.Wert.Beginn, Is.EqualTo(gestarteter.Beginn));
            Assert.That(ergebnis.Wert.Kontributor.KontributorId, Is.EqualTo(3));
            Assert.That(ergebnis.Wert.Kontributor.Name, Is.EqualTo("Stefan"));
            Assert.That(ergebnis.Wert.Ende, Is.Not.Null);
        });
    }

    // Der zweite Stopp schreibt nicht: die gemessene Zeit darf nicht nach hinten wachsen.
    [Test]
    public void Wenn_derselbe_Eintrag_ein_zweites_Mal_gestoppt_wird_dann_gelingt_der_Aufruf_und_das_Ende_bleibt_stehen()
    {
        var zeitenRepository = TestZeitenRepository.Leer();
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"), MitDieserKarte());
        var gestarteter = service.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3)).Wert.Zeiteintrag;
        var ersterStopp = service.BeendeZeitmessung(14, gestarteter.ZeiteintragId);
        var schreibzugriffeNachDemErstenStopp = zeitenRepository.Schreibzugriffe;

        var zweiterStopp = service.BeendeZeitmessung(14, gestarteter.ZeiteintragId);

        Assert.That(zweiterStopp.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(zweiterStopp.Wert.Ende, Is.EqualTo(ersterStopp.Wert.Ende));
            Assert.That(zeitenRepository.Schreibzugriffe, Is.EqualTo(schreibzugriffeNachDemErstenStopp));
            Assert.That(zeitenRepository.Zeiteintraege, Has.Count.EqualTo(1));
        });
    }

    // Ein stillgelegter Kontributor darf keinen Timer mehr starten — sein laufender muss trotzdem
    // beendet werden können, sonst liefe er für immer.
    [Test]
    public void Wenn_der_Kontributor_stillgelegt_ist_dann_laesst_sich_sein_laufender_Timer_trotzdem_beenden()
    {
        var zeitenRepository = TestZeitenRepository.Leer().MitLaufendemEintrag(14, kontributorId: 1, AchtUhrVier);
        var service = new ZeitenService(zeitenRepository, MitStillgelegtemKontributor(), MitDieserKarte());

        var ergebnis = service.BeendeZeitmessung(14, 1);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Ende, Is.Not.Null);
    }

    // Der Stopp fragt den Kontributorenbestand gar nicht erst ab: jeder darf stoppen, auch einen
    // fremden Timer.
    [Test]
    public void Wenn_ein_fremder_Timer_gestoppt_wird_dann_liest_der_Dienst_keinen_Kontributor()
    {
        var zeitenRepository = TestZeitenRepository.Leer().MitLaufendemEintrag(14, kontributorId: 9, AchtUhrVier);
        var kontributorenRepository = new TestKontributorenRepository();
        var service = new ZeitenService(zeitenRepository, kontributorenRepository, MitDieserKarte());

        var ergebnis = service.BeendeZeitmessung(14, 1);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Kontributor.KontributorId, Is.EqualTo(9));
    }

    // Zweistufiger Befund, erste Stufe: gibt es schon die Karte nicht, schickte ein Befund über
    // den Zeiteintrag den Aufrufer auf eine Kartenadresse, die selbst 404 antwortet.
    [Test]
    public void Wenn_es_die_Karte_beim_Stopp_nicht_gibt_dann_meldet_der_Dienst_karte_unbekannt()
    {
        var service = new ZeitenService(TestZeitenRepository.Leer(), MitKontributor(3, "Stefan"), OhneDieseKarte());

        var ergebnis = service.BeendeZeitmessung(999, 7);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("999"));
        });
    }

    // Zweistufiger Befund, zweite Stufe: die Karte gibt es, den Eintrag an ihr nicht.
    [Test]
    public void Wenn_es_den_Zeiteintrag_an_dieser_Karte_nicht_gibt_dann_meldet_der_Dienst_zeiteintrag_unbekannt_mit_beiden_Nummern()
    {
        var service = new ZeitenService(TestZeitenRepository.Leer(), MitKontributor(3, "Stefan"), MitDieserKarte());

        var ergebnis = service.BeendeZeitmessung(14, 777);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("zeiteintrag-unbekannt"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("777"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("14"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("/api/karten/14"));
        });
    }

    // Ein Eintrag, den es gibt — nur an einer anderen Karte. Dieselbe Regel wie bei Anhang und
    // Dateiverweis: er wird wie ein unbekannter behandelt.
    [Test]
    public void Wenn_der_Zeiteintrag_an_einer_anderen_Karte_liegt_dann_wird_er_wie_ein_unbekannter_behandelt()
    {
        var zeitenRepository = TestZeitenRepository.Leer().MitLaufendemEintrag(21, kontributorId: 3, AchtUhrVier);
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"), MitDieserKarte());

        var ergebnis = service.BeendeZeitmessung(14, 1);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.IstErfolg, Is.False);
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("zeiteintrag-unbekannt"));
            Assert.That(zeitenRepository.Schreibzugriffe, Is.EqualTo(0));
        });
    }

    // Die Uhr wird hereingereicht: sonst wäre das Ende im Test nicht setzbar.
    [Test]
    public void Wenn_der_Stopp_gelingt_dann_reicht_der_Dienst_eine_UTC_Uhrzeit_ins_Repository()
    {
        var zeitenRepository = TestZeitenRepository.Leer().MitLaufendemEintrag(14, kontributorId: 3, AchtUhrVier);
        var service = new ZeitenService(zeitenRepository, MitKontributor(3, "Stefan"), MitDieserKarte());
        var vorDemAufruf = DateTimeOffset.UtcNow;

        service.BeendeZeitmessung(14, 1);

        var nachDemAufruf = DateTimeOffset.UtcNow;
        Assert.That(zeitenRepository.ErhalteneUhrzeitBeimStopp, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(zeitenRepository.ErhalteneUhrzeitBeimStopp!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(zeitenRepository.ErhalteneUhrzeitBeimStopp!.Value, Is.InRange(vorDemAufruf, nachDemAufruf));
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

    // Nur für den zweistufigen 404 gebraucht: der Dienst fragt die Karte, sonst nichts.
    private static TestKartenRepository MitDieserKarte()
    {
        return TestKartenRepository.Leer().MitKartendetail(EineKarte());
    }

    private static TestKartenRepository OhneDieseKarte()
    {
        return TestKartenRepository.Leer().OhneDieseKarte();
    }

    private static Kartendetail EineKarte()
    {
        var karte = new Karte(14, "Migration schreiben", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null);
        return new Kartendetail(karte, 1, "Entwicklung", 1, "Backlog", null, [], [], [], [], [], [], null, []);
    }
}
