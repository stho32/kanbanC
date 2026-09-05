using System.Data;
using Dapper;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Persistenz.Karten;

// Etiketten einer Karte und der Bestand ihres Boards. Der Bestand ist eine Ableitung aus den
// Karten und keine eigene Ablage: ein Text ohne Karte verschwindet damit von selbst, ohne dass
// jemand aufräumt.
internal static class Etikettenleser
{
    public static IReadOnlyList<string> LiesEtikettenDerKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)
    {
        var texte = verbindung.Query<string>(@"
            SELECT Text
              FROM Etikett
             WHERE Karte = @KarteId
             ORDER BY Text COLLATE NOCASE", new { KarteId = karteId }, transaktion);
        return texte.ToList();
    }

    // Die Kartenzahl ist eine COUNT-Spalte und kein gezählter Client: die Gruppierung gehört in
    // die Abfrage, die ohnehin über drei Tabellen geht. Vorschläge stammen nur aus dem Board der
    // Karte — Etiketten anderer Boards haben hier nichts zu suchen.
    public static IReadOnlyList<Etikettvorschlag> LiesVorschlaegeDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        // Als Tupel gelesen und nicht in einen Record: fuer eine gerechnete Spalte meldet
        // Microsoft.Data.Sqlite keinen Spaltentyp, und Dapper versucht sie dann als Byte[] in den
        // Record zu schreiben. Ein Tupel liest ueber die Position und den tatsaechlichen Wert.
        var zeilen = verbindung.Query<(string Text, long Kartenzahl)>(@"
            SELECT e.Text, COUNT(*) AS Kartenzahl
              FROM Etikett e
              JOIN Karte k ON k.KarteId = e.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board = @BoardId
             GROUP BY e.Text
             ORDER BY COUNT(*) DESC, e.Text COLLATE NOCASE", new { BoardId = boardId }, transaktion);
        return zeilen.Select(zeile => new Etikettvorschlag(zeile.Text, (int)zeile.Kartenzahl)).ToList();
    }
}
