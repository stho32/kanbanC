using System.Data;
using Dapper;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Persistenz.Boards;

internal static class Spaltenleser
{
    public static IReadOnlyList<Spalte> LiesSpaltenNachPosition(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var zeilen = verbindung.Query<Spaltenzeile>(@"
            SELECT SpalteId, Bezeichnung, Position, IstAbschlussspalte, Anzeigegrenze
              FROM Spalte
             WHERE Board = @BoardId
             ORDER BY Position", new { BoardId = boardId }, transaktion);
        return zeilen.Select(AlsSpalte).ToList();
    }

    private static Spalte AlsSpalte(Spaltenzeile zeile)
    {
        var istAbschlussspalte = zeile.IstAbschlussspalte != 0;
        return new Spalte(zeile.SpalteId, zeile.Bezeichnung, (int)zeile.Position, istAbschlussspalte, (int?)zeile.Anzeigegrenze);
    }

    private sealed record Spaltenzeile(long SpalteId, string Bezeichnung, long Position, long IstAbschlussspalte, long? Anzeigegrenze);
}
