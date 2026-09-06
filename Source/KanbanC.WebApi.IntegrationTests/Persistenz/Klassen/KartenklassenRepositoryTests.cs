using Dapper;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Klassen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Klassen;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using Microsoft.Data.Sqlite;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Klassen;

public class KartenklassenRepositoryTests
{
    [Test]
    public void Wenn_eine_Kartenklasse_angelegt_wird_dann_traegt_die_geschriebene_Zeile_den_Zaehlerstand_0()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var ergebnis = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.KartenklasseId, Is.GreaterThan(0));
            Assert.That(ergebnis.Wert.Name, Is.EqualTo("WBS"));
            Assert.That(ergebnis.Wert.Praefix, Is.EqualTo("WBS-"));
            Assert.That(ergebnis.Wert.Zaehlerstand, Is.EqualTo(0));
        });
        Assert.That(GespeicherteZaehlerstaende(datenbank, boardId), Is.EqualTo(new[] { 0L }));
    }

    // Auch ein Board, das schon Karten traegt, beginnt seinen Nummernkreis bei 0: der Zaehler
    // gehoert der Klasse, nicht dem Board.
    [Test]
    public void Wenn_das_Board_schon_Karten_traegt_dann_beginnt_der_Zaehlerstand_trotzdem_bei_0()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        LegeKarteAn(datenbank, boardId, "Migration schreiben");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var ergebnis = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis!.Wert.Zaehlerstand, Is.EqualTo(0));
    }

    [Test]
    public void Wenn_Name_und_Praefix_Raender_tragen_dann_kommen_sie_getrimmt_und_sonst_zeichengleich_zurueck()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("  Dokumentation  ", "  DOK-  "));

        var kartenklassen = repository.LadeAlle(boardId);
        Assert.Multiple(() =>
        {
            Assert.That(kartenklassen![0].Name, Is.EqualTo("Dokumentation"));
            Assert.That(kartenklassen[0].Praefix, Is.EqualTo("DOK-"));
        });
    }

    [Test]
    [TestCase("WBS_")]
    [TestCase("wbs-")]
    [TestCase("DOKU")]
    public void Wenn_das_Praefix_eine_eigene_Schreibweise_hat_dann_liegt_es_unveraendert_in_der_Ablage(string praefix)
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", praefix));

        Assert.That(GespeichertePraefixe(datenbank, boardId), Is.EqualTo(new[] { praefix }));
    }

    [Test]
    public void Wenn_drei_Kartenklassen_angelegt_wurden_dann_kommen_sie_in_Anlagereihenfolge_zurueck()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));
        repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("Bugmeldungen", "BUG-"));
        repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("Beschaffung", "BES-"));

        var kartenklassen = repository.LadeAlle(boardId);

        Assert.That(kartenklassen!.Select(kartenklasse => kartenklasse.Name), Is.EqualTo(new[] { "WBS", "Bugmeldungen", "Beschaffung" }));
    }

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_liefert_LadeAlle_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var kartenklassen = repository.LadeAlle(999);

        Assert.That(kartenklassen, Is.Null);
    }

    [Test]
    public void Wenn_das_Board_noch_keine_Kartenklasse_hat_dann_liefert_LadeAlle_eine_leere_Liste()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var kartenklassen = repository.LadeAlle(boardId);

        Assert.That(kartenklassen, Is.Not.Null);
        Assert.That(kartenklassen, Is.Empty);
    }

    [Test]
    public void Wenn_das_Board_unbekannt_ist_dann_liefert_LegeAn_null_und_schreibt_nichts()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var ergebnis = repository.LegeAn(999, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis, Is.Null);
        Assert.That(AlleKartenklassenAnzahl(datenbank), Is.EqualTo(0));
    }

    // Der eindeutige Index selbst: er sichert die Regel gegen jeden Weg, der am Dienst
    // vorbeischreibt — auch in abweichender Schreibweise.
    [Test]
    public void Wenn_ein_zweiter_INSERT_dasselbe_Praefix_in_anderer_Schreibweise_setzt_dann_scheitert_er_an_der_Datenbank()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        FuegeKartenklasseDirektEin(datenbank, boardId, "WBS", "WBS-");

        Assert.That(
            () => FuegeKartenklasseDirektEin(datenbank, boardId, "Arbeitspakete", "wbs-"),
            Throws.TypeOf<SqliteException>());
    }

    [Test]
    public void Wenn_dasselbe_Praefix_auf_einem_zweiten_Board_gesetzt_wird_dann_geht_es_durch()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var erstesBoard = LegeBoardAn(datenbank);
        var zweitesBoard = LegeBoardAn(datenbank);
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(erstesBoard, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        var ergebnis = repository.LegeAn(zweitesBoard, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis!.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(GespeichertePraefixe(datenbank, erstesBoard), Is.EqualTo(new[] { "WBS-" }));
            Assert.That(GespeichertePraefixe(datenbank, zweitesBoard), Is.EqualTo(new[] { "WBS-" }));
        });
    }

    // Der Aufrufer trifft nie auf eine nackte Datenbankmeldung: laeuft der Dienst dem Index in
    // die Quere, wird daraus eine Zurückweisung mit Befund.
    [Test]
    public void Wenn_das_Praefix_am_Dienst_vorbei_schon_vergeben_ist_dann_liefert_LegeAn_eine_Zurueckweisung_statt_einer_Ausnahme()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var boardId = LegeBoardAn(datenbank);
        FuegeKartenklasseDirektEin(datenbank, boardId, "WBS", "WBS-");
        var repository = new KartenklassenRepository(datenbank.Verbindungsfabrik);

        var ergebnis = repository.LegeAn(boardId, new KartenklasseAnlegenAnfrage("Arbeitspakete", "wbs-"));

        Assert.That(ergebnis, Is.Not.Null);
        Assert.That(ergebnis!.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("kartenklasse-praefix-vergeben"));
        Assert.That(GespeicherteKartenklassenAnzahl(datenbank, boardId), Is.EqualTo(1));
    }

    private static long LegeBoardAn(TemporaereDatenbank datenbank)
    {
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);
        return repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard()).BoardId;
    }

    private static void LegeKarteAn(TemporaereDatenbank datenbank, long boardId, string titel)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var spalteId = verbindung.QuerySingle<long>(@"
            SELECT SpalteId
              FROM Spalte
             WHERE Board = @BoardId
             ORDER BY Position
             LIMIT 1", new { BoardId = boardId });
        verbindung.Execute(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, 1)", new { Spalte = spalteId, Titel = titel });
    }

    private static void FuegeKartenklasseDirektEin(TemporaereDatenbank datenbank, long boardId, string name, string praefix)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Kartenklasse (Board, Name, Praefix)
            VALUES (@Board, @Name, @Praefix)", new { Board = boardId, Name = name, Praefix = praefix });
    }

    private static IReadOnlyList<string> GespeichertePraefixe(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT Praefix
              FROM Kartenklasse
             WHERE Board = @BoardId
             ORDER BY KartenklasseId", new { BoardId = boardId }).ToList();
    }

    private static IReadOnlyList<long> GespeicherteZaehlerstaende(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<long>(@"
            SELECT Zaehlerstand
              FROM Kartenklasse
             WHERE Board = @BoardId
             ORDER BY KartenklasseId", new { BoardId = boardId }).ToList();
    }

    private static long GespeicherteKartenklassenAnzahl(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Kartenklasse
             WHERE Board = @BoardId", new { BoardId = boardId });
    }

    private static long AlleKartenklassenAnzahl(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Kartenklasse");
    }
}
