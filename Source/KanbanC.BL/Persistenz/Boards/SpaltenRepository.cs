using System.Data;
using Dapper;
using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Persistenz.Boards;

public sealed class SpaltenRepository : ISpaltenRepository
{
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public SpaltenRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public Spalte? LegeAn(long boardId, SpalteAnlegenAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var boardIstUnbekannt = !ExistiertBoard(verbindung, transaktion, boardId);
        if (boardIstUnbekannt)
        {
            return null;
        }

        var position = NaechstePosition(verbindung, transaktion, boardId);
        var spalteId = FuegeSpalteEin(verbindung, transaktion, boardId, anfrage, position);
        transaktion.Commit();
        return new Spalte(spalteId, anfrage.Bezeichnung, position, anfrage.IstAbschlussspalte, anfrage.Anzeigegrenze);
    }

    public Spalte? Aendere(long boardId, long spalteId, SpalteAendernAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var geaenderteZeilen = SchreibeAenderung(verbindung, transaktion, boardId, spalteId, anfrage);
        var spalteGehoertNichtZumBoard = geaenderteZeilen == 0;
        if (spalteGehoertNichtZumBoard)
        {
            return null;
        }

        var position = LiesPosition(verbindung, transaktion, spalteId);
        transaktion.Commit();
        return new Spalte(spalteId, anfrage.Bezeichnung, position, anfrage.IstAbschlussspalte, anfrage.Anzeigegrenze);
    }

    public IReadOnlyList<Spalte>? LadeAlle(long boardId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();

        var boardIstUnbekannt = !ExistiertBoard(verbindung, null, boardId);
        if (boardIstUnbekannt)
        {
            return null; // stil-check: C25 null heisst "Board unbekannt" (404); die leere Liste heisst "Board ohne Spalten"
        }

        return Spaltenleser.LiesSpaltenNachPosition(verbindung, null, boardId);
    }

    public IReadOnlyList<Spalte>? SetzeReihenfolge(long boardId, IReadOnlyList<long> reihenfolge)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var boardIstUnbekannt = !ExistiertBoard(verbindung, transaktion, boardId);
        if (boardIstUnbekannt)
        {
            return null; // stil-check: C25 null heisst "Board unbekannt" (404); die leere Liste heisst "Board ohne Spalten"
        }

        SchreibePositionen(verbindung, transaktion, boardId, reihenfolge);
        var spalten = Spaltenleser.LiesSpaltenNachPosition(verbindung, transaktion, boardId);
        transaktion.Commit();
        return spalten;
    }

    public bool Entferne(long boardId, long spalteId)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var geloeschteZeilen = LoescheSpalte(verbindung, transaktion, boardId, spalteId);
        var spalteGehoertNichtZumBoard = geloeschteZeilen == 0;
        if (spalteGehoertNichtZumBoard)
        {
            return false;
        }

        VerdichtePositionen(verbindung, transaktion, boardId);
        transaktion.Commit();
        return true;
    }

    private static int LoescheSpalte(IDbConnection verbindung, IDbTransaction transaktion, long boardId, long spalteId)
    {
        return verbindung.Execute(@"
            DELETE
              FROM Spalte
             WHERE SpalteId = @SpalteId
               AND Board = @Board", new { SpalteId = spalteId, Board = boardId }, transaktion);
    }

    private static void VerdichtePositionen(IDbConnection verbindung, IDbTransaction transaktion, long boardId)
    {
        var verbleibendeSpalten = Spaltenleser.LiesSpaltenNachPosition(verbindung, transaktion, boardId);
        var lueckenloseReihenfolge = verbleibendeSpalten.Select(spalte => spalte.SpalteId).ToList();
        SchreibePositionen(verbindung, transaktion, boardId, lueckenloseReihenfolge);
    }

    private static void SchreibePositionen(IDbConnection verbindung, IDbTransaction transaktion, long boardId, IReadOnlyList<long> reihenfolge)
    {
        for (var stelle = 0; stelle < reihenfolge.Count; stelle++)
        {
            var parameter = new { SpalteId = reihenfolge[stelle], Board = boardId, Position = stelle + 1 };
            verbindung.Execute(@"
                UPDATE Spalte
                   SET Position = @Position
                 WHERE SpalteId = @SpalteId
                   AND Board = @Board", parameter, transaktion);
        }
    }

    private static bool ExistiertBoard(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var anzahl = verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Board
             WHERE BoardId = @BoardId", new { BoardId = boardId }, transaktion);
        return anzahl > 0;
    }

    private static int NaechstePosition(IDbConnection verbindung, IDbTransaction transaktion, long boardId)
    {
        return verbindung.ExecuteScalar<int>(@"
            SELECT COALESCE(MAX(Position), 0) + 1
              FROM Spalte
             WHERE Board = @BoardId", new { BoardId = boardId }, transaktion);
    }

    private static long FuegeSpalteEin(IDbConnection verbindung, IDbTransaction transaktion, long boardId, SpalteAnlegenAnfrage anfrage, int position)
    {
        var parameter = new
        {
            Board = boardId,
            anfrage.Bezeichnung,
            Position = position,
            anfrage.IstAbschlussspalte,
            anfrage.Anzeigegrenze,
        };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Spalte (Board, Bezeichnung, Position, IstAbschlussspalte, Anzeigegrenze)
            VALUES (@Board, @Bezeichnung, @Position, @IstAbschlussspalte, @Anzeigegrenze);
            SELECT last_insert_rowid();", parameter, transaktion);
    }

    private static int SchreibeAenderung(IDbConnection verbindung, IDbTransaction transaktion, long boardId, long spalteId, SpalteAendernAnfrage anfrage)
    {
        var parameter = new
        {
            SpalteId = spalteId,
            Board = boardId,
            anfrage.Bezeichnung,
            anfrage.IstAbschlussspalte,
            anfrage.Anzeigegrenze,
        };
        return verbindung.Execute(@"
            UPDATE Spalte
               SET Bezeichnung = @Bezeichnung,
                   IstAbschlussspalte = @IstAbschlussspalte,
                   Anzeigegrenze = @Anzeigegrenze
             WHERE SpalteId = @SpalteId
               AND Board = @Board", parameter, transaktion);
    }

    private static int LiesPosition(IDbConnection verbindung, IDbTransaction transaktion, long spalteId)
    {
        return verbindung.ExecuteScalar<int>(@"
            SELECT Position
              FROM Spalte
             WHERE SpalteId = @SpalteId", new { SpalteId = spalteId }, transaktion);
    }
}
