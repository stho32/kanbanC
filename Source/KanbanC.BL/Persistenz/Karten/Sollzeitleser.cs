using System.Data;
using Dapper;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Persistenz.Karten;

// Die Sollbänder eines ganzen Boards in **einer** Abfrage — nicht eine je Karte; derselbe Schnitt
// über Karte JOIN Spalte wie bei den fünf n-Lesern.
// Die Tabelle Kartensollzeit wird vom WBS-Import geschrieben und sonst nur im Soll-Ist-Join
// gelesen, und dort als gerechnete Abweichung statt als gespeicherte Zeile. Fehlt die Zeile,
// fehlt das Band — kein Ersatzwert.
internal static class Sollzeitleser
{
    public static IReadOnlyDictionary<long, Zeitband> LiesSollbaenderDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var zeilen = verbindung.Query<Sollzeitzeile>(@"
            SELECT o.Karte, o.SollzeitVonStunden, o.SollzeitBisStunden
              FROM Kartensollzeit o
              JOIN Karte k ON k.KarteId = o.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board = @BoardId", new { BoardId = boardId }, transaktion);

        var baenderJeKarte = new Dictionary<long, Zeitband>(); // stil-check: C11 Sollband je KarteId, kein Domaenenbestand
        foreach (var zeile in zeilen)
        {
            baenderJeKarte[zeile.Karte] = new Zeitband((decimal)zeile.SollzeitVonStunden, (decimal)zeile.SollzeitBisStunden);
        }

        return baenderJeKarte;
    }

    private sealed record Sollzeitzeile(long Karte, double SollzeitVonStunden, double SollzeitBisStunden);
}
