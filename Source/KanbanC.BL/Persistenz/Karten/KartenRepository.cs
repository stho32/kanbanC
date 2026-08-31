using System.Data;
using Dapper;
using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Operations.Karten;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Persistenz.Karten;

public sealed class KartenRepository : IKartenRepository
{
    private readonly IDatenbankVerbindungsfabrik _verbindungsfabrik;

    public KartenRepository(IDatenbankVerbindungsfabrik verbindungsfabrik)
    {
        _verbindungsfabrik = verbindungsfabrik;
    }

    public Karte? LegeAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage)
    {
        using var verbindung = _verbindungsfabrik.Oeffne();
        using var transaktion = verbindung.BeginTransaction();

        var spalteGehoertNichtZumBoard = !GehoertSpalteZumBoard(verbindung, transaktion, boardId, spalteId);
        if (spalteGehoertNichtZumBoard)
        {
            return null; // stil-check: C25 null heisst "Spalte unbekannt oder fremd" (404)
        }

        var titel = Kartentitel.Normalisiert(anfrage.Titel);
        var position = NaechstePosition(verbindung, transaktion, spalteId);
        var karteId = FuegeKarteEin(verbindung, transaktion, spalteId, titel, position);
        transaktion.Commit();
        return new Karte(karteId, titel, position);
    }

    private static bool GehoertSpalteZumBoard(IDbConnection verbindung, IDbTransaction transaktion, long boardId, long spalteId)
    {
        var anzahl = verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Spalte
             WHERE SpalteId = @SpalteId
               AND Board = @Board", new { SpalteId = spalteId, Board = boardId }, transaktion);
        return anzahl > 0;
    }

    private static int NaechstePosition(IDbConnection verbindung, IDbTransaction transaktion, long spalteId)
    {
        return verbindung.ExecuteScalar<int>(@"
            SELECT COALESCE(MAX(Position), 0) + 1
              FROM Karte
             WHERE Spalte = @SpalteId", new { SpalteId = spalteId }, transaktion);
    }

    private static long FuegeKarteEin(IDbConnection verbindung, IDbTransaction transaktion, long spalteId, string titel, int position)
    {
        var parameter = new { Spalte = spalteId, Titel = titel, Position = position };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Karte (Spalte, Titel, Position)
            VALUES (@Spalte, @Titel, @Position);
            SELECT last_insert_rowid();", parameter, transaktion);
    }
}
