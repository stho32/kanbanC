using System.Data;
using Dapper;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Persistenz.Karten;

internal static class Kartenleser
{
    public static IReadOnlyDictionary<long, IReadOnlyList<Karte>> LiesKartenNachPosition(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var zeilen = verbindung.Query<Kartenzeile>(@"
            SELECT k.KarteId, k.Spalte, k.Titel, k.Position
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board = @BoardId
             ORDER BY k.Spalte, k.Position", new { BoardId = boardId }, transaktion);

        var kartenJeSpalte = new Dictionary<long, IReadOnlyList<Karte>>();
        foreach (var gruppe in zeilen.GroupBy(zeile => zeile.Spalte))
        {
            kartenJeSpalte[gruppe.Key] = gruppe.Select(AlsKarte).ToList();
        }

        return kartenJeSpalte;
    }

    public static IReadOnlyList<Karte> LiesKartenEinerSpalte(IDbConnection verbindung, IDbTransaction? transaktion, long spalteId)
    {
        var zeilen = verbindung.Query<Kartenzeile>(@"
            SELECT KarteId, Spalte, Titel, Position
              FROM Karte
             WHERE Spalte = @SpalteId
             ORDER BY Position", new { SpalteId = spalteId }, transaktion);
        return zeilen.Select(AlsKarte).ToList();
    }

    private static Karte AlsKarte(Kartenzeile zeile)
    {
        return new Karte(zeile.KarteId, zeile.Titel, (int)zeile.Position);
    }

    private sealed record Kartenzeile(long KarteId, long Spalte, string Titel, long Position);
}
