using Dapper;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.BL.Persistenz.Zeiten;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Zeiten;

// Das Repository und der Zeitenleser fassen die Datenbank an und sind deshalb Integrationstests.
// Der Zeitenleser wird über die zwei Wege geprüft, auf denen er im Betrieb läuft: das
// Kartendetail und die Boardantwort.
public class ZeitenRepositoryTests
{
    private const string IsoZeitpunktformat = "O";
    private static readonly DateTimeOffset AchtUhrVier = new(2026, 9, 6, 8, 4, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NeunUhrZwoelf = new(2026, 9, 6, 9, 12, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NeunUhrVierzig = new(2026, 9, 6, 9, 40, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NeunUhrVierzigMitteleuropaeisch = new(2026, 9, 6, 11, 40, 0, TimeSpan.FromHours(2));
    private static readonly DateTimeOffset ElfUhrFuenfzehn = new(2026, 9, 6, 11, 15, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EinehalbeMinuteVorAchtUhrVier = new(2026, 9, 6, 8, 3, 30, TimeSpan.Zero);

    [Test]
    public void Wenn_eine_Zeitmessung_startet_dann_entsteht_ein_Eintrag_ohne_Ende()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);

        var start = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier);

        Assert.That(start, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(start!.IstNeu, Is.True);
            Assert.That(start.Zeiteintrag.Karte, Is.EqualTo(aufbau.ErsteKarteId));
            Assert.That(start.Zeiteintrag.Kontributor.KontributorId, Is.EqualTo(aufbau.StefanId));
            Assert.That(start.Zeiteintrag.Kontributor.Name, Is.EqualTo("Stefan"));
            Assert.That(start.Zeiteintrag.Beginn, Is.EqualTo(AchtUhrVier));
            Assert.That(start.Zeiteintrag.Ende, Is.Null);
        });
        Assert.That(Endetexte(datenbank), Is.EqualTo(new string?[] { null }));
    }

    // Der Beginn geht als ISO-8601 in UTC durch die TEXT-Spalte: nur bei einheitlichem Versatz
    // sortiert Text lexikografisch wie chronologisch.
    [Test]
    public void Wenn_eine_Zeitmessung_startet_dann_steht_der_Beginn_als_ISO_Text_in_UTC_in_der_Spalte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var beginnMitVersatz = new DateTimeOffset(2026, 9, 6, 10, 4, 0, TimeSpan.FromHours(2));

        repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, beginnMitVersatz);

        Assert.That(Beginntexte(datenbank), Is.EqualTo(new[] { "2026-09-06T08:04:00.0000000+00:00" }));
    }

    // Der zweite Start desselben Paares gibt **denselben** Eintrag zurück und schreibt nichts.
    [Test]
    public void Wenn_derselbe_Kontributor_auf_derselben_Karte_ein_zweites_Mal_startet_dann_kommt_derselbe_Eintrag_zurueck_und_nichts_wird_geschrieben()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var erster = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier);

        var zweiter = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, NeunUhrZwoelf);

        Assert.Multiple(() =>
        {
            Assert.That(zweiter!.IstNeu, Is.False);
            Assert.That(zweiter.Zeiteintrag.ZeiteintragId, Is.EqualTo(erster!.Zeiteintrag.ZeiteintragId));
            Assert.That(zweiter.Zeiteintrag.Beginn, Is.EqualTo(AchtUhrVier));
        });
        Assert.That(Zeiteintragszeilen(datenbank), Has.Length.EqualTo(1));
    }

    [Test]
    public void Wenn_derselbe_Kontributor_auf_einer_zweiten_Karte_startet_dann_entsteht_ein_zweiter_offener_Eintrag()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier);

        var zweiter = repository.StarteZeitmessung(aufbau.ZweiteKarteId, aufbau.StefanId, NeunUhrZwoelf);

        Assert.That(zweiter!.IstNeu, Is.True);
        Assert.That(Zeiteintragszeilen(datenbank), Has.Length.EqualTo(2));
    }

    [Test]
    public void Wenn_ein_zweiter_Kontributor_auf_derselben_Karte_startet_dann_entsteht_ein_zweiter_offener_Eintrag()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier);

        var zweiter = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.AgentId, NeunUhrZwoelf);

        Assert.That(zweiter!.IstNeu, Is.True);
        Assert.That(Zeiteintragszeilen(datenbank), Has.Length.EqualTo(2));
    }

    [Test]
    public void Wenn_es_die_Karte_nicht_gibt_dann_liefert_der_Start_null_und_schreibt_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);

        var start = repository.StarteZeitmessung(999, aufbau.StefanId, AchtUhrVier);

        Assert.That(start, Is.Null);
        Assert.That(Zeiteintragszeilen(datenbank), Is.Empty);
    }

    // Der Zeitenleser über das Kartendetail: die Eintraege der Karte in Beginn-Folge.
    [Test]
    public void Wenn_eine_Karte_zwei_Zeiteintraege_traegt_dann_stehen_sie_im_Kartendetail_in_Beginn_Folge()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.AgentId, NeunUhrZwoelf);
        repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier);

        var detail = new KartenRepository(datenbank.Verbindungsfabrik).LiesKartendetail(aufbau.ErsteKarteId);

        Assert.That(detail!.Zeiteintraege.Select(eintrag => eintrag.Kontributor.Name), Is.EqualTo(new[] { "Stefan", "Claude-Agent" }));
        Assert.That(detail.Zeiteintraege.Select(eintrag => eintrag.Beginn), Is.EqualTo(new[] { AchtUhrVier, NeunUhrZwoelf }));
    }

    [Test]
    public void Wenn_eine_Karte_keinen_Zeiteintrag_traegt_dann_ist_die_Liste_im_Kartendetail_leer()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);

        var detail = new KartenRepository(datenbank.Verbindungsfabrik).LiesKartendetail(aufbau.ErsteKarteId);

        Assert.That(detail!.Zeiteintraege, Is.Empty);
    }

    // Ein stillgelegter Kontributor fällt nicht heraus: seine erfasste Zeit bleibt seine, und die
    // Zeile zeigt den Zusatz ohne zweiten Abruf.
    [Test]
    public void Wenn_der_Kontributor_stillgelegt_wurde_dann_traegt_sein_Zeiteintrag_weiterhin_ihn_mit_Stilllegungsdatum()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        new ZeitenRepository(datenbank.Verbindungsfabrik).StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier);
        LegeKontributorStill(datenbank, aufbau.StefanId, "2026-09-06");

        var detail = new KartenRepository(datenbank.Verbindungsfabrik).LiesKartendetail(aufbau.ErsteKarteId);

        Assert.That(detail!.Zeiteintraege, Has.Count.EqualTo(1));
        Assert.That(detail.Zeiteintraege[0].Kontributor.StillgelegtAm, Is.EqualTo(new DateOnly(2026, 9, 6)));
    }

    // Der Zeitenleser über die Boardantwort: flach über alle Spalten, nur die offenen.
    [Test]
    public void Wenn_auf_zwei_Karten_zweier_Spalten_Timer_laufen_dann_traegt_das_Board_beide_flach_in_Beginn_Folge()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        repository.StarteZeitmessung(aufbau.KarteDerZweitenSpalteId, aufbau.StefanId, NeunUhrZwoelf);
        repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier);

        var board = new BoardRepository(datenbank.Verbindungsfabrik).Lade(aufbau.BoardId);

        Assert.That(board!.LaufendeZeiteintraege.Select(eintrag => eintrag.Karte),
            Is.EqualTo(new[] { aufbau.ErsteKarteId, aufbau.KarteDerZweitenSpalteId }));
        Assert.That(board.LaufendeZeiteintraege.Select(eintrag => eintrag.Ende), Is.EqualTo(new DateTimeOffset?[] { null, null }));
    }

    [Test]
    public void Wenn_auf_dem_Board_kein_Timer_laeuft_dann_ist_die_Liste_leer_und_nicht_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);

        var board = new BoardRepository(datenbank.Verbindungsfabrik).Lade(aufbau.BoardId);

        Assert.That(board!.LaufendeZeiteintraege, Is.Not.Null);
        Assert.That(board.LaufendeZeiteintraege, Is.Empty);
    }

    // Ein abgeschlossener Eintrag zaehlt nicht als laufend. Geschlossen wird er hier am Dienst
    // vorbei — in diesem Slice gibt es keinen Weg dorthin, und genau das soll der Test zeigen.
    [Test]
    public void Wenn_ein_Eintrag_ein_Ende_traegt_dann_faellt_er_aus_den_laufenden_des_Boards_heraus()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var geschlossener = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier);
        repository.StarteZeitmessung(aufbau.ZweiteKarteId, aufbau.StefanId, NeunUhrZwoelf);
        SetzeEnde(datenbank, geschlossener!.Zeiteintrag.ZeiteintragId, "2026-09-06T09:00:00.0000000+00:00");

        var board = new BoardRepository(datenbank.Verbindungsfabrik).Lade(aufbau.BoardId);

        Assert.That(board!.LaufendeZeiteintraege.Select(eintrag => eintrag.Karte), Is.EqualTo(new[] { aufbau.ZweiteKarteId }));
    }

    // Das Kartendetail zeigt auch den geschlossenen: „alle Eintraege dieser Karte" heisst alle.
    [Test]
    public void Wenn_ein_Eintrag_ein_Ende_traegt_dann_steht_er_weiter_im_Kartendetail_und_traegt_sein_Ende()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var start = new ZeitenRepository(datenbank.Verbindungsfabrik).StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier);
        SetzeEnde(datenbank, start!.Zeiteintrag.ZeiteintragId, "2026-09-06T09:12:00.0000000+00:00");

        var detail = new KartenRepository(datenbank.Verbindungsfabrik).LiesKartendetail(aufbau.ErsteKarteId);

        Assert.That(detail!.Zeiteintraege[0].Ende, Is.EqualTo(NeunUhrZwoelf));
    }

    // Nach dem Schliessen macht der partielle Index wieder Platz: das ist der Pfad, den I0024
    // braucht, und er ist im Repository bisher unbelegt.
    [Test]
    public void Wenn_der_laufende_Eintrag_geschlossen_wurde_dann_legt_derselbe_Kontributor_auf_derselben_Karte_einen_neuen_an()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var erster = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier);
        SetzeEnde(datenbank, erster!.Zeiteintrag.ZeiteintragId, "2026-09-06T09:00:00.0000000+00:00");

        var zweiter = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, NeunUhrZwoelf);

        Assert.That(zweiter!.IstNeu, Is.True);
        Assert.That(zweiter.Zeiteintrag.ZeiteintragId, Is.Not.EqualTo(erster.Zeiteintrag.ZeiteintragId));
    }

    // Der Archivstand filtert hier nicht: ein laufender Timer auf einer archivierten Karte ist ein
    // Befund, den I0027 sehen soll, kein Rauschen.
    [Test]
    public void Wenn_die_Karte_archiviert_ist_dann_bleibt_ihr_laufender_Eintrag_in_den_laufenden_des_Boards()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        new ZeitenRepository(datenbank.Verbindungsfabrik).StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier);
        ArchiviereKarte(datenbank, aufbau.ErsteKarteId);

        var board = new BoardRepository(datenbank.Verbindungsfabrik).Lade(aufbau.BoardId);

        Assert.That(board!.LaufendeZeiteintraege.Select(eintrag => eintrag.Karte), Is.EqualTo(new[] { aufbau.ErsteKarteId }));
    }

    // Die Liste des Boards trägt nur Eintraege zu Karten **dieses** Boards.
    [Test]
    public void Wenn_auf_einem_zweiten_Board_ein_Timer_laeuft_dann_steht_er_nicht_in_den_laufenden_des_ersten()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var nachbarboard = LegeBoardAn(datenbank);
        var nachbarkarte = LegeKarteAn(datenbank, nachbarboard.BoardId, nachbarboard.Spalten[0].SpalteId, "Fremde Karte");
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        repository.StarteZeitmessung(nachbarkarte, aufbau.StefanId, AchtUhrVier);
        repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, NeunUhrZwoelf);

        var board = new BoardRepository(datenbank.Verbindungsfabrik).Lade(aufbau.BoardId);

        Assert.That(board!.LaufendeZeiteintraege.Select(eintrag => eintrag.Karte), Is.EqualTo(new[] { aufbau.ErsteKarteId }));
    }

    [Test]
    public void Wenn_eine_Zeitmessung_beendet_wird_dann_steht_das_Ende_und_Beginn_Karte_und_Kontributor_bleiben_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var gestarteter = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier)!.Zeiteintrag;

        var beendeter = repository.BeendeZeitmessung(aufbau.ErsteKarteId, gestarteter.ZeiteintragId, NeunUhrVierzig);

        Assert.That(beendeter, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(beendeter!.ZeiteintragId, Is.EqualTo(gestarteter.ZeiteintragId));
            Assert.That(beendeter.Karte, Is.EqualTo(aufbau.ErsteKarteId));
            Assert.That(beendeter.Kontributor.KontributorId, Is.EqualTo(aufbau.StefanId));
            Assert.That(beendeter.Beginn, Is.EqualTo(AchtUhrVier));
            Assert.That(beendeter.Ende, Is.EqualTo(NeunUhrVierzig));
            Assert.That(beendeter.Ende!.Value - beendeter.Beginn, Is.EqualTo(TimeSpan.FromMinutes(96)));
        });
    }

    // Das Ende geht als ISO-8601 in UTC durch dieselbe TEXT-Spalte wie der Beginn — und kommt als
    // nullable DateTimeOffset zurück, den Dapper aus der Spalte nicht selbst materialisiert.
    [Test]
    public void Wenn_eine_Zeitmessung_beendet_wird_dann_steht_das_Ende_als_ISO_Text_in_UTC_in_der_Spalte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var gestarteter = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier)!.Zeiteintrag;

        repository.BeendeZeitmessung(aufbau.ErsteKarteId, gestarteter.ZeiteintragId, NeunUhrVierzigMitteleuropaeisch);

        Assert.That(Endetexte(datenbank), Is.EqualTo(new[] { "2026-09-06T09:40:00.0000000+00:00" }));
    }

    // Der Wiederhol-Schutz sitzt im UPDATE selbst: der zweite Stopp trifft keine Zeile, das Ende
    // bleibt stehen, und die gemessene Dauer wächst nicht von 1:36 auf 3:11.
    [Test]
    public void Wenn_derselbe_Eintrag_ein_zweites_Mal_beendet_wird_dann_bleibt_das_Ende_des_ersten_Stopps_stehen()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var gestarteter = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier)!.Zeiteintrag;
        repository.BeendeZeitmessung(aufbau.ErsteKarteId, gestarteter.ZeiteintragId, NeunUhrVierzig);

        var zweiterStopp = repository.BeendeZeitmessung(aufbau.ErsteKarteId, gestarteter.ZeiteintragId, ElfUhrFuenfzehn);

        Assert.That(zweiterStopp, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(zweiterStopp!.Ende, Is.EqualTo(NeunUhrVierzig));
            Assert.That(zweiterStopp.Ende!.Value - zweiterStopp.Beginn, Is.EqualTo(TimeSpan.FromMinutes(96)));
        });
        Assert.That(Endetexte(datenbank), Is.EqualTo(new[] { "2026-09-06T09:40:00.0000000+00:00" }));
        Assert.That(Zeiteintragszeilen(datenbank), Has.Length.EqualTo(1));
    }

    // Beginn 08:04:00, Uhr beim Stopp 08:03:30 — geschrieben wird 08:04:00, still und ohne Meldung.
    [Test]
    public void Wenn_die_Uhr_hinter_den_Beginn_zurueckgesprungen_ist_dann_steht_der_Beginn_als_Ende_in_der_Spalte()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var gestarteter = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier)!.Zeiteintrag;

        var beendeter = repository.BeendeZeitmessung(aufbau.ErsteKarteId, gestarteter.ZeiteintragId, EinehalbeMinuteVorAchtUhrVier);

        Assert.That(beendeter!.Ende, Is.EqualTo(AchtUhrVier));
        Assert.That(beendeter.Ende!.Value - beendeter.Beginn, Is.EqualTo(TimeSpan.Zero));
        Assert.That(Endetexte(datenbank), Is.EqualTo(new[] { "2026-09-06T08:04:00.0000000+00:00" }));
    }

    [Test]
    public void Wenn_die_Zeiteintragsnummer_unbekannt_ist_dann_meldet_das_Repository_nichts_gefunden()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);

        var beendeter = repository.BeendeZeitmessung(aufbau.ErsteKarteId, 999, NeunUhrVierzig);

        Assert.That(beendeter, Is.Null);
    }

    // Ein Eintrag, den es gibt — nur an einer anderen Karte. Er wird wie ein unbekannter behandelt
    // und bleibt dabei unberührt.
    [Test]
    public void Wenn_der_Zeiteintrag_an_einer_anderen_Karte_liegt_dann_meldet_das_Repository_nichts_gefunden_und_schreibt_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var gestarteter = repository.StarteZeitmessung(aufbau.ZweiteKarteId, aufbau.StefanId, AchtUhrVier)!.Zeiteintrag;

        var beendeter = repository.BeendeZeitmessung(aufbau.ErsteKarteId, gestarteter.ZeiteintragId, NeunUhrVierzig);

        Assert.That(beendeter, Is.Null);
        Assert.That(Endetexte(datenbank), Is.EqualTo(new string?[] { null }));
    }

    // Die Mechanik hinter „stoppen und neu starten": der partielle UNIQUE-Index aus 018 gibt das
    // Paar (Karte, Kontributor) frei, sobald Ende steht.
    [Test]
    public void Wenn_nach_dem_Stopp_dasselbe_Paar_erneut_startet_dann_entsteht_ein_zweiter_Eintrag_ohne_dass_der_Index_anschlaegt()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var erster = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier)!.Zeiteintrag;
        repository.BeendeZeitmessung(aufbau.ErsteKarteId, erster.ZeiteintragId, NeunUhrVierzig);

        var zweiter = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, ElfUhrFuenfzehn);

        Assert.That(zweiter, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(zweiter!.IstNeu, Is.True);
            Assert.That(zweiter.Zeiteintrag.ZeiteintragId, Is.Not.EqualTo(erster.ZeiteintragId));
            Assert.That(zweiter.Zeiteintrag.Ende, Is.Null);
        });
        Assert.That(Endetexte(datenbank), Is.EqualTo(new string?[] { "2026-09-06T09:40:00.0000000+00:00", null }));
    }

    // Missing-Doc aus R00027: dass `AND Ende IS NULL` im UPDATE bei **zwei gleichzeitigen**
    // Aufrufen genau eine Zeile trifft, war im Repository nirgends vorgemacht. Beide Stopper
    // laufen an, keiner scheitert, und danach steht **ein** Ende — das eine oder das andere,
    // nie beide nacheinander.
    [Test]
    public async Task Wenn_zwei_Aufrufe_denselben_Eintrag_gleichzeitig_beenden_dann_setzt_genau_einer_das_Ende()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var gestarteter = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier)!.Zeiteintrag;

        var beideStopps = await Task.WhenAll(
            Task.Run(() => repository.BeendeZeitmessung(aufbau.ErsteKarteId, gestarteter.ZeiteintragId, NeunUhrVierzig)),
            Task.Run(() => repository.BeendeZeitmessung(aufbau.ErsteKarteId, gestarteter.ZeiteintragId, ElfUhrFuenfzehn)));

        var endeInDerSpalte = Endetexte(datenbank).Single();
        Assert.That(endeInDerSpalte, Is.AnyOf("2026-09-06T09:40:00.0000000+00:00", "2026-09-06T11:15:00.0000000+00:00"));
        Assert.Multiple(() =>
        {
            Assert.That(beideStopps[0]!.Ende, Is.EqualTo(beideStopps[1]!.Ende));
            Assert.That(beideStopps[0]!.Ende!.Value.ToString(IsoZeitpunktformat, System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(endeInDerSpalte));
        });
    }

    // Ein beendeter Eintrag ist kein laufender mehr — die Bahnenplakette fällt von selbst heraus.
    [Test]
    public void Wenn_der_Timer_beendet_ist_dann_fuehrt_das_Board_ihn_nicht_mehr_unter_den_laufenden()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var gestarteter = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.StefanId, AchtUhrVier)!.Zeiteintrag;
        repository.BeendeZeitmessung(aufbau.ErsteKarteId, gestarteter.ZeiteintragId, NeunUhrVierzig);

        var board = new BoardRepository(datenbank.Verbindungsfabrik).Lade(aufbau.BoardId);

        Assert.That(board!.LaufendeZeiteintraege, Is.Empty);
    }

    // Ein stillgelegter Kontributor hindert den Stopp nicht: das Repository fragt gar nicht danach.
    [Test]
    public void Wenn_der_Kontributor_stillgelegt_ist_dann_laesst_sich_sein_laufender_Eintrag_beenden()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var aufbau = Aufbau(datenbank);
        var repository = new ZeitenRepository(datenbank.Verbindungsfabrik);
        var gestarteter = repository.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.AgentId, AchtUhrVier)!.Zeiteintrag;
        LegeKontributorStill(datenbank, aufbau.AgentId, "2026-09-06");

        var beendeter = repository.BeendeZeitmessung(aufbau.ErsteKarteId, gestarteter.ZeiteintragId, NeunUhrVierzig);

        Assert.That(beendeter, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(beendeter!.Ende, Is.EqualTo(NeunUhrVierzig));
            Assert.That(beendeter.Kontributor.KontributorId, Is.EqualTo(aufbau.AgentId));
            Assert.That(beendeter.Kontributor.StillgelegtAm, Is.EqualTo(new DateOnly(2026, 9, 6)));
        });
    }

    private static Testaufbau Aufbau(TemporaereDatenbank datenbank)
    {
        var board = LegeBoardAn(datenbank);
        var ersteKarteId = LegeKarteAn(datenbank, board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var zweiteKarteId = LegeKarteAn(datenbank, board.BoardId, board.Spalten[0].SpalteId, "Kartenform zeichnen");
        var karteDerZweitenSpalteId = LegeKarteAn(datenbank, board.BoardId, board.Spalten[1].SpalteId, "Lizenz klären");
        var stefanId = LegeKontributorAn(datenbank, "Stefan", Kontributorart.Mensch);
        var agentId = LegeKontributorAn(datenbank, "Claude-Agent", Kontributorart.Agent);
        return new Testaufbau(board.BoardId, ersteKarteId, zweiteKarteId, karteDerZweitenSpalteId, stefanId, agentId);
    }

    private static Board LegeBoardAn(TemporaereDatenbank datenbank)
    {
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        return repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
    }

    private static long LegeKarteAn(TemporaereDatenbank datenbank, long boardId, long spalteId, string titel)
    {
        var repository = new KartenRepository(datenbank.Verbindungsfabrik);
        var karte = repository.LegeAn(boardId, spalteId, new KarteAnlegenAnfrage(titel));
        return karte!.KarteId;
    }

    private static long LegeKontributorAn(TemporaereDatenbank datenbank, string name, Kontributorart art)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Kontributor (Name, Kontributorart)
            VALUES (@Name, @Kontributorart);
            SELECT last_insert_rowid();", new { Name = name, Kontributorart = art.ToString() });
    }

    private static void LegeKontributorStill(TemporaereDatenbank datenbank, long kontributorId, string stillgelegtAm)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kontributorstilllegung (Kontributor, StillgelegtAm)
            VALUES (@Kontributor, @StillgelegtAm)", new { Kontributor = kontributorId, StillgelegtAm = stillgelegtAm });
    }

    private static void ArchiviereKarte(TemporaereDatenbank datenbank, long karteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kartenarchivierung (Karte)
            VALUES (@Karte)", new { Karte = karteId });
    }

    // Am Dienst vorbei: in diesem Slice gibt es keinen Weg, der ein Ende schreibt — genau deshalb
    // muss der Test es selbst tun, um die Filterung auf „laufend" ehrlich zu prüfen.
    private static void SetzeEnde(TemporaereDatenbank datenbank, long zeiteintragId, string ende)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            UPDATE Zeiteintrag
               SET Ende = @Ende
             WHERE ZeiteintragId = @ZeiteintragId", new { ZeiteintragId = zeiteintragId, Ende = ende });
    }

    private static (long ZeiteintragId, long Karte, long Kontributor, string Beginn, string? Ende)[] Zeiteintragszeilen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<(long ZeiteintragId, long Karte, long Kontributor, string Beginn, string? Ende)>(@"
            SELECT ZeiteintragId, Karte, Kontributor, Beginn, Ende
              FROM Zeiteintrag
             ORDER BY ZeiteintragId").ToArray();
    }

    private static string[] Beginntexte(TemporaereDatenbank datenbank)
    {
        return Zeiteintragszeilen(datenbank).Select(zeile => zeile.Beginn).ToArray();
    }

    private static string?[] Endetexte(TemporaereDatenbank datenbank)
    {
        return Zeiteintragszeilen(datenbank).Select(zeile => zeile.Ende).ToArray();
    }

    private sealed record Testaufbau(long BoardId, long ErsteKarteId, long ZweiteKarteId, long KarteDerZweitenSpalteId, long StefanId, long AgentId);

}
