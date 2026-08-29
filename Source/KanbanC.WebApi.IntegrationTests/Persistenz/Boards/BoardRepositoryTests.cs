using Dapper;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.Contracts.Boards;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Boards;

public class BoardRepositoryTests
{
    [Test]
    public void Wenn_das_erste_Board_angelegt_wird_dann_hat_es_die_BoardId_1_und_drei_gespeicherte_Spalten()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);

        var board = repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());

        Assert.Multiple(() =>
        {
            Assert.That(board.BoardId, Is.EqualTo(1));
            Assert.That(board.Name, Is.EqualTo("Entwicklung"));
            Assert.That(board.Art, Is.EqualTo(BoardArt.Linie));
            Assert.That(board.Spalten.Select(s => s.SpalteId), Is.EqualTo(new long[] { 1, 2, 3 }));
        });
        Assert.That(GespeicherteSpaltenbezeichnungen(datenbank, board.BoardId),
            Is.EqualTo(new[] { "Zu erledigen", "In Arbeit", "Erledigt" }));
    }

    [Test]
    public void Wenn_zwei_Boards_gleichen_Namens_angelegt_werden_dann_bekommen_sie_die_BoardIds_1_und_2()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);

        var erstes = repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());
        var zweites = repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());

        Assert.That(erstes.BoardId, Is.EqualTo(1));
        Assert.That(zweites.BoardId, Is.EqualTo(2));
        Assert.That(GespeicherteBoardAnzahl(datenbank), Is.EqualTo(2));
    }

    [Test]
    public void Wenn_Termine_angegeben_sind_dann_stehen_sie_nach_dem_Reload_unveraendert_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var anfrage = new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31));

        var board = repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());

        Assert.That(board.Starttermin, Is.EqualTo(new DateOnly(2026, 9, 1)));
        Assert.That(board.Zieltermin, Is.EqualTo(new DateOnly(2026, 12, 31)));
        Assert.That(GespeicherteTermine(datenbank, board.BoardId), Is.EqualTo(("2026-09-01", "2026-12-31")));
    }

    [Test]
    public void Wenn_keine_Termine_angegeben_sind_dann_bleiben_beide_Felder_leer()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var anfrage = new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null);

        var board = repository.LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());

        Assert.That(board.Starttermin, Is.Null);
        Assert.That(board.Zieltermin, Is.Null);
        Assert.That(GespeicherteTermine(datenbank, board.BoardId), Is.EqualTo(((string?)null, (string?)null)));
    }

    [Test]
    public void Wenn_zwei_Boards_gespeichert_sind_dann_liefert_LadeAlle_beide_in_Reihenfolge_der_BoardId()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        repository.LegeAn(new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, null, null), StandardspaltenVorlage.FuerNeuesBoard());

        var boards = new BoardRepository(datenbank.Verbindungsfabrik).LadeAlle();

        Assert.That(boards, Is.EqualTo(new[]
        {
            new BoardUebersicht(1, "Entwicklung", BoardArt.Linie, null, null),
            new BoardUebersicht(2, "KanbanC 1.0", BoardArt.Projekt, null, null),
        }));
    }

    [Test]
    public void Wenn_keine_Boards_gespeichert_sind_dann_liefert_LadeAlle_eine_leere_Liste()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();

        var boards = new BoardRepository(datenbank.Verbindungsfabrik).LadeAlle();

        Assert.That(boards, Is.Empty);
    }

    [Test]
    public void Wenn_ein_Board_mit_Terminen_geladen_wird_dann_kommen_Spalten_und_Termine_wie_gespeichert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var anfrage = new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31));
        var gespeichert = new BoardRepository(datenbank.Verbindungsfabrik).LegeAn(anfrage, StandardspaltenVorlage.FuerNeuesBoard());

        var geladen = new BoardRepository(datenbank.Verbindungsfabrik).Lade(gespeichert.BoardId);

        Assert.That(geladen, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(geladen.BoardId, Is.EqualTo(gespeichert.BoardId));
            Assert.That(geladen.Name, Is.EqualTo("KanbanC 1.0"));
            Assert.That(geladen.Art, Is.EqualTo(BoardArt.Projekt));
            Assert.That(geladen.Starttermin, Is.EqualTo(new DateOnly(2026, 9, 1)));
            Assert.That(geladen.Zieltermin, Is.EqualTo(new DateOnly(2026, 12, 31)));
            Assert.That(geladen.Spalten, Is.EqualTo(gespeichert.Spalten));
        });
    }

    [Test]
    public void Wenn_die_BoardId_nicht_vergeben_ist_dann_liefert_Lade_null()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());

        var geladen = repository.Lade(99);

        Assert.That(geladen, Is.Null);
    }

    private static List<string> GespeicherteSpaltenbezeichnungen(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT Bezeichnung
              FROM Spalte
             WHERE Board = @BoardId
             ORDER BY Position", new { BoardId = boardId }).ToList();
    }

    private static long GespeicherteBoardAnzahl(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Board");
    }

    private static (string? Starttermin, string? Zieltermin) GespeicherteTermine(TemporaereDatenbank datenbank, long boardId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.QuerySingle<(string?, string?)>(@"
            SELECT Starttermin, Zieltermin
              FROM Board
             WHERE BoardId = @BoardId", new { BoardId = boardId });
    }
}
