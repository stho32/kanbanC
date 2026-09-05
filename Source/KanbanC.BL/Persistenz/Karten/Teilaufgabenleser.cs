using System.Data;
using Dapper;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Persistenz.Karten;

// Die Teilaufgaben einer Karte in Anzeigereihenfolge. Die TeilaufgabeId entscheidet bei gleicher
// Position: zwei Zeilen können dieselbe Positionszahl tragen, solange nichts verdichtet wird, und
// ohne den zweiten Schlüssel bestimmte die Datenbank die Reihenfolge.
// Ohne Archivfilter, wie das ganze Kartendetail: eine archivierte Karte behält ihre Adresse.
internal static class Teilaufgabenleser
{
    public static IReadOnlyList<Teilaufgabe> LiesTeilaufgabenDerKarte(IDbConnection verbindung, IDbTransaction? transaktion, long karteId)
    {
        var zeilen = verbindung.Query<Teilaufgabenzeile>(@"
            SELECT TeilaufgabeId, Text, Position, Abgehakt
              FROM Teilaufgabe
             WHERE Karte = @KarteId
             ORDER BY Position, TeilaufgabeId", new { KarteId = karteId }, transaktion);
        return zeilen.Select(AlsTeilaufgabe).ToList();
    }

    // Die Zeile führt Abgehakt als long und nicht als bool: Microsoft.Data.Sqlite meldet für die
    // INTEGER-Spalte den Typ Int64, und Dapper findet dann keinen passenden Konstruktor (belegt
    // in SqliteWahrheitswertProbeTests). Die Wandlung steht deshalb sichtbar hier.
    private static Teilaufgabe AlsTeilaufgabe(Teilaufgabenzeile zeile)
    {
        return new Teilaufgabe(zeile.TeilaufgabeId, zeile.Text, (int)zeile.Position, zeile.Abgehakt != 0);
    }

    private sealed record Teilaufgabenzeile(long TeilaufgabeId, string Text, long Position, long Abgehakt);
}
