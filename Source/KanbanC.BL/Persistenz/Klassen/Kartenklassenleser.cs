using System.Data;
using Dapper;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Persistenz.Klassen;

internal static class Kartenklassenleser
{
    // ORDER BY KartenklasseId ist die **Anlagereihenfolge**: eine alphabetische Ordnung ordnete
    // die Liste beim Anlegen unter der Hand um, und eine Positionsspalte hätte niemand, der sie
    // ändert.
    public static IReadOnlyList<Kartenklasse> LiesKartenklassenDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var zeilen = verbindung.Query<Kartenklassenzeile>(@"
            SELECT KartenklasseId, Name, Praefix, Zaehlerstand
              FROM Kartenklasse
             WHERE Board = @BoardId
             ORDER BY KartenklasseId", new { BoardId = boardId }, transaktion);
        return zeilen.Select(AlsKartenklasse).ToList();
    }

    public static Kartenklasse? LiesKartenklasseDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId, long kartenklasseId)
    {
        var zeile = verbindung.QuerySingleOrDefault<Kartenklassenzeile>(@"
            SELECT KartenklasseId, Name, Praefix, Zaehlerstand
              FROM Kartenklasse
             WHERE KartenklasseId = @KartenklasseId
               AND Board = @Board", new { KartenklasseId = kartenklasseId, Board = boardId }, transaktion);
        if (zeile is null)
        {
            return null;
        }

        return AlsKartenklasse(zeile);
    }

    private static Kartenklasse AlsKartenklasse(Kartenklassenzeile zeile)
    {
        return new Kartenklasse(zeile.KartenklasseId, zeile.Name, zeile.Praefix, (int)zeile.Zaehlerstand);
    }

    private sealed record Kartenklassenzeile(long KartenklasseId, string Name, string Praefix, long Zaehlerstand);
}
