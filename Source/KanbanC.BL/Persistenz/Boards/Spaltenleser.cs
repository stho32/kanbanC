using System.Data;
using Dapper;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Persistenz.Boards;

internal static class Spaltenleser
{
    private static readonly IReadOnlyList<Karte> OhneKarten = [];

    public static IReadOnlyList<Spalte> LiesSpaltenNachPosition(
        IDbConnection verbindung,
        IDbTransaction? transaktion,
        long boardId,
        IReadOnlyDictionary<long, IReadOnlyList<Karte>> kartenJeSpalte)
    {
        var zeilen = verbindung.Query<Spaltenzeile>(@"
            SELECT SpalteId, Bezeichnung, Position, IstAbschlussspalte, Anzeigegrenze
              FROM Spalte
             WHERE Board = @BoardId
             ORDER BY Position", new { BoardId = boardId }, transaktion);
        return zeilen.Select(zeile => AlsSpalte(zeile, KartenDerSpalte(kartenJeSpalte, zeile.SpalteId))).ToList();
    }

    public static IReadOnlyList<long> LiesSpalteIdsNachPosition(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var spalteIds = verbindung.Query<long>(@"
            SELECT SpalteId
              FROM Spalte
             WHERE Board = @BoardId
             ORDER BY Position", new { BoardId = boardId }, transaktion);
        return spalteIds.ToList();
    }

    public static Spalte? LiesSpalteDesBoards(
        IDbConnection verbindung,
        IDbTransaction? transaktion,
        long boardId,
        long spalteId,
        IReadOnlyList<Karte> karten)
    {
        var zeile = verbindung.QuerySingleOrDefault<Spaltenzeile>(@"
            SELECT SpalteId, Bezeichnung, Position, IstAbschlussspalte, Anzeigegrenze
              FROM Spalte
             WHERE SpalteId = @SpalteId
               AND Board = @Board", new { SpalteId = spalteId, Board = boardId }, transaktion);
        if (zeile is null)
        {
            return null;
        }

        return AlsSpalte(zeile, karten);
    }

    private static IReadOnlyList<Karte> KartenDerSpalte(IReadOnlyDictionary<long, IReadOnlyList<Karte>> kartenJeSpalte, long spalteId)
    {
        var spalteTraegtKarten = kartenJeSpalte.TryGetValue(spalteId, out var karten);
        if (spalteTraegtKarten)
        {
            return karten!;
        }

        return OhneKarten;
    }

    private static Spalte AlsSpalte(Spaltenzeile zeile, IReadOnlyList<Karte> karten)
    {
        var istAbschlussspalte = zeile.IstAbschlussspalte != 0;
        return new Spalte(zeile.SpalteId, zeile.Bezeichnung, (int)zeile.Position, istAbschlussspalte, (int?)zeile.Anzeigegrenze, karten);
    }

    private sealed record Spaltenzeile(long SpalteId, string Bezeichnung, long Position, long IstAbschlussspalte, long? Anzeigegrenze);
}
