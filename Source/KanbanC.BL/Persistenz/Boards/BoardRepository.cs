using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Models.Boards;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Persistenz.Boards;

public sealed class BoardRepository : IBoardRepository
{
    private const string IsoDatumsformat = "yyyy-MM-dd";
    private static readonly IReadOnlyList<Karte> OhneKarten = [];
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public BoardRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public Board LegeAn(BoardAnlegenAnfrage anfrage, Spaltenvorlagen standardspalten)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var boardId = FuegeBoardEin(verbindung, transaktion, anfrage);
        var spalten = new List<Spalte>();
        foreach (var vorlage in standardspalten)
        {
            var spalteId = FuegeSpalteEin(verbindung, transaktion, boardId, vorlage);
            spalten.Add(new Spalte(spalteId, vorlage.Bezeichnung, vorlage.Position, vorlage.IstAbschlussspalte, vorlage.Anzeigegrenze, OhneKarten));
        }

        transaktion.Commit();
        // Ein neues Board bekommt weder Einstellungs- noch Archivzeile: die Abwesenheit der Zeile
        // ist die Voreinstellung, und die heißt aus beziehungsweise aktiv.
        return new Board(boardId, anfrage.Name, anfrage.Art, anfrage.Starttermin, anfrage.Zieltermin, spalten, false, false);
    }

    // Die Liste zeigt entweder die aktiven oder die archivierten Boards, nie beide; die
    // Reihenfolge bleibt in beiden Faellen dieselbe.
    public IReadOnlyList<BoardUebersicht> LadeAlle(Archivierung archivstand)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        var zeilen = verbindung.Query<BoardUebersichtZeile>(@"
            SELECT b.BoardId, b.Name, b.Art, b.Starttermin, b.Zieltermin
              FROM Board b
              LEFT JOIN Boardarchivierung a ON a.Board = b.BoardId
             WHERE CASE WHEN a.Board IS NULL THEN 0 ELSE 1 END = @IstArchiviert
             ORDER BY b.Name COLLATE NOCASE, b.BoardId", new { archivstand.IstArchiviert });
        return zeilen.Select(AlsUebersicht).ToList();
    }

    public Board? Lade(long boardId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        return LiesBoard(verbindung, null, boardId);
    }

    public Board? SetzeKartenzahlanzeige(long boardId, Kartenzahlanzeige anzeige)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        // Das Board wird in der Transaktion gelesen, bevor geschrieben wird: ein unbekanntes
        // bekommt keine Einstellungszeile. Der Fremdschlüssel würde es zwar ebenfalls abweisen,
        // aber mit einer Ausnahme — und die wäre an der API eine Antwort ohne Befund.
        var boardIstUnbekannt = LiesBoardzeile(verbindung, transaktion, boardId) is null;
        if (boardIstUnbekannt)
        {
            return null;
        }

        SchreibeKartenzahlanzeige(verbindung, transaktion, boardId, anzeige);
        var board = LiesBoard(verbindung, transaktion, boardId);
        transaktion.Commit();
        return board;
    }

    public Board? BenenneUm(long boardId, BoardUmbenennenAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var boardIstUnbekannt = LiesBoardzeile(verbindung, transaktion, boardId) is null;
        if (boardIstUnbekannt)
        {
            return null;
        }

        SchreibeNamen(verbindung, transaktion, boardId, anfrage.Name);
        var board = LiesBoard(verbindung, transaktion, boardId);
        transaktion.Commit();
        return board;
    }

    public Board? SetzeArchivierung(long boardId, Archivierung archivierung)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        // Wie beim Schalten der Kartenzahl wird das Board in der Transaktion gelesen, bevor
        // geschrieben wird: SQLite erzwingt den Fremdschluessel nicht, und eine Ausnahme waere an
        // der API eine Antwort ohne Befund.
        var boardIstUnbekannt = LiesBoardzeile(verbindung, transaktion, boardId) is null;
        if (boardIstUnbekannt)
        {
            return null;
        }

        SchreibeArchivierung(verbindung, transaktion, boardId, archivierung);
        var board = LiesBoard(verbindung, transaktion, boardId);
        transaktion.Commit();
        return board;
    }

    // Die Zeile selbst ist die Aussage: archivieren legt sie an, zurueckholen entfernt sie. Beides
    // laesst sich beliebig oft wiederholen, ohne dass sich etwas aendert.
    private static void SchreibeArchivierung(IDbConnection verbindung, IDbTransaction transaktion, long boardId, Archivierung archivierung)
    {
        var parameter = new { Board = boardId };
        if (archivierung.IstArchiviert)
        {
            verbindung.Execute(@"
                INSERT INTO Boardarchivierung (Board)
                VALUES (@Board)
                ON CONFLICT (Board) DO NOTHING", parameter, transaktion);
            return;
        }

        verbindung.Execute(@"
            DELETE FROM Boardarchivierung
             WHERE Board = @Board", parameter, transaktion);
    }

    private static void SchreibeNamen(IDbConnection verbindung, IDbTransaction transaktion, long boardId, string name)
    {
        var parameter = new { BoardId = boardId, Name = name };
        verbindung.Execute(@"
            UPDATE Board
               SET Name = @Name
             WHERE BoardId = @BoardId", parameter, transaktion);
    }

    private static void SchreibeKartenzahlanzeige(IDbConnection verbindung, IDbTransaction transaktion, long boardId, Kartenzahlanzeige anzeige)
    {
        var parameter = new { Board = boardId, anzeige.ZeigtKartenzahl };
        verbindung.Execute(@"
            INSERT INTO Boardeinstellung (Board, ZeigtKartenzahl)
            VALUES (@Board, @ZeigtKartenzahl)
            ON CONFLICT (Board) DO UPDATE SET ZeigtKartenzahl = excluded.ZeigtKartenzahl", parameter, transaktion);
    }

    private static Board? LiesBoard(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var boardZeile = LiesBoardzeile(verbindung, transaktion, boardId);
        if (boardZeile is null)
        {
            return null;
        }

        var kartenJeSpalte = Kartenleser.LiesKartenNachPosition(verbindung, transaktion, boardId);
        var spalten = Spaltenleser.LiesSpaltenNachPosition(verbindung, transaktion, boardId, kartenJeSpalte);
        return AlsBoard(boardZeile, spalten);
    }

    // Fehlt die Einstellungszeile, gilt die Voreinstellung aus; fehlt die Archivzeile, ist das
    // Board aktiv.
    private static BoardZeile? LiesBoardzeile(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        return verbindung.QuerySingleOrDefault<BoardZeile>(@"
            SELECT b.BoardId, b.Name, b.Art, b.Starttermin, b.Zieltermin,
                   COALESCE(e.ZeigtKartenzahl, 0) AS ZeigtKartenzahl,
                   CASE WHEN a.Board IS NULL THEN 0 ELSE 1 END AS IstArchiviert
              FROM Board b
              LEFT JOIN Boardeinstellung e ON e.Board = b.BoardId
              LEFT JOIN Boardarchivierung a ON a.Board = b.BoardId
             WHERE b.BoardId = @BoardId", new { BoardId = boardId }, transaktion);
    }

    private static long FuegeBoardEin(IDbConnection verbindung, IDbTransaction transaktion, BoardAnlegenAnfrage anfrage)
    {
        var parameter = new
        {
            anfrage.Name,
            Art = anfrage.Art.ToString(),
            Starttermin = AlsIsoText(anfrage.Starttermin),
            Zieltermin = AlsIsoText(anfrage.Zieltermin),
        };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Board (Name, Art, Starttermin, Zieltermin)
            VALUES (@Name, @Art, @Starttermin, @Zieltermin);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    private static long FuegeSpalteEin(IDbConnection verbindung, IDbTransaction transaktion, long boardId, Spaltenvorlage vorlage)
    {
        var parameter = new
        {
            Board = boardId,
            vorlage.Bezeichnung,
            vorlage.Position,
            vorlage.IstAbschlussspalte,
            vorlage.Anzeigegrenze,
        };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Spalte (Board, Bezeichnung, Position, IstAbschlussspalte, Anzeigegrenze)
            VALUES (@Board, @Bezeichnung, @Position, @IstAbschlussspalte, @Anzeigegrenze);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    private static string? AlsIsoText(DateOnly? termin)
    {
        if (termin is null)
        {
            return null;
        }

        return termin.Value.ToString(IsoDatumsformat, CultureInfo.InvariantCulture);
    }
    private static BoardUebersicht AlsUebersicht(BoardUebersichtZeile zeile)
    {
        return new BoardUebersicht(zeile.BoardId, zeile.Name, Enum.Parse<BoardArt>(zeile.Art), AlsTermin(zeile.Starttermin), AlsTermin(zeile.Zieltermin));
    }

    private static DateOnly? AlsTermin(string? isoText)
    {
        if (isoText is null)
        {
            return null;
        }

        return DateOnly.ParseExact(isoText, IsoDatumsformat, CultureInfo.InvariantCulture);
    }

    private sealed record BoardUebersichtZeile(long BoardId, string Name, string Art, string? Starttermin, string? Zieltermin);

    private sealed record BoardZeile(long BoardId, string Name, string Art, string? Starttermin, string? Zieltermin, long ZeigtKartenzahl, long IstArchiviert);

    private static Board AlsBoard(BoardZeile zeile, IReadOnlyList<Spalte> spalten)
    {
        var zeigtKartenzahl = zeile.ZeigtKartenzahl != 0;
        var istArchiviert = zeile.IstArchiviert != 0;
        return new Board(zeile.BoardId, zeile.Name, Enum.Parse<BoardArt>(zeile.Art), AlsTermin(zeile.Starttermin), AlsTermin(zeile.Zieltermin), spalten, zeigtKartenzahl, istArchiviert);
    }
}
