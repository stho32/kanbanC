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
    private static readonly DateTimeOffset AchtUhrVier = new(2026, 9, 6, 8, 4, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NeunUhrZwoelf = new(2026, 9, 6, 9, 12, 0, TimeSpan.Zero);

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
