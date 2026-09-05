using System.Data;
using System.Globalization;
using Dapper;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Persistenz.Karten;

internal static class Kartenleser
{
    private const string IsoDatumsformat = "yyyy-MM-dd";

    public static IReadOnlyDictionary<long, IReadOnlyList<Karte>> LiesKartenNachPosition(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var zeilen = verbindung.Query<Kartenzeile>(@"
            SELECT k.KarteId, k.Spalte, k.Titel, k.Position, e.ErledigtAm
              FROM Karte k
              JOIN Spalte s ON s.SpalteId = k.Spalte
              LEFT JOIN Karteerledigung e ON e.Karte = k.KarteId
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
            SELECT k.KarteId, k.Spalte, k.Titel, k.Position, e.ErledigtAm
              FROM Karte k
              LEFT JOIN Karteerledigung e ON e.Karte = k.KarteId
             WHERE k.Spalte = @SpalteId
             ORDER BY k.Position", new { SpalteId = spalteId }, transaktion);
        return zeilen.Select(AlsKarte).ToList();
    }

    private static Karte AlsKarte(Kartenzeile zeile)
    {
        return new Karte(zeile.KarteId, zeile.Titel, (int)zeile.Position, AlsErledigungsdatum(zeile.ErledigtAm));
    }

    private static DateOnly? AlsErledigungsdatum(string? isoText)
    {
        if (isoText is null)
        {
            return null;
        }

        return DateOnly.ParseExact(isoText, IsoDatumsformat, CultureInfo.InvariantCulture);
    }

    private sealed record Kartenzeile(long KarteId, long Spalte, string Titel, long Position, string? ErledigtAm);
}
