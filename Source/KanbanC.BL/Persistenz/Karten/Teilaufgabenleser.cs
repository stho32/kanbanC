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

    // Alle Teilaufgaben des Boards in **einer** Abfrage, je KarteId ihre Liste: 431 Karten kosten
    // eine Abfrage und nicht 431. Die Ordnung innerhalb einer Karte bleibt dieselbe wie oben.
    public static IReadOnlyDictionary<long, IReadOnlyList<Teilaufgabe>> LiesTeilaufgabenDesBoards(IDbConnection verbindung, IDbTransaction? transaktion, long boardId)
    {
        var zeilen = verbindung.Query<Boardteilaufgabenzeile>(@"
            SELECT t.Karte, t.TeilaufgabeId, t.Text, t.Position, t.Abgehakt
              FROM Teilaufgabe t
              JOIN Karte k ON k.KarteId = t.Karte
              JOIN Spalte s ON s.SpalteId = k.Spalte
             WHERE s.Board = @BoardId
             ORDER BY t.Karte, t.Position, t.TeilaufgabeId", new { BoardId = boardId }, transaktion);

        var teilaufgabenJeKarte = new Dictionary<long, IReadOnlyList<Teilaufgabe>>(); // stil-check: C11 Zuordnung von KarteId zu Liste, kein Domaenenbestand
        foreach (var gruppe in zeilen.GroupBy(zeile => zeile.Karte))
        {
            teilaufgabenJeKarte[gruppe.Key] = gruppe.Select(AlsBoardteilaufgabe).ToList();
        }

        return teilaufgabenJeKarte;
    }

    private static Teilaufgabe AlsBoardteilaufgabe(Boardteilaufgabenzeile zeile)
    {
        return AlsTeilaufgabe(new Teilaufgabenzeile(zeile.TeilaufgabeId, zeile.Text, zeile.Position, zeile.Abgehakt));
    }

    private sealed record Boardteilaufgabenzeile(long Karte, long TeilaufgabeId, string Text, long Position, long Abgehakt);

    // Die Zeile führt Abgehakt als long und nicht als bool: Microsoft.Data.Sqlite meldet für die
    // INTEGER-Spalte den Typ Int64, und Dapper findet dann keinen passenden Konstruktor (belegt
    // in SqliteWahrheitswertProbeTests). Die Wandlung steht deshalb sichtbar hier.
    private static Teilaufgabe AlsTeilaufgabe(Teilaufgabenzeile zeile)
    {
        return new Teilaufgabe(zeile.TeilaufgabeId, zeile.Text, (int)zeile.Position, zeile.Abgehakt != 0);
    }

    private sealed record Teilaufgabenzeile(long TeilaufgabeId, string Text, long Position, long Abgehakt);
}
