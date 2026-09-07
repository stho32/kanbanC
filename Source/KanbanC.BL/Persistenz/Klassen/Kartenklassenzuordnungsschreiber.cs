using System.Data;
using Dapper;

namespace KanbanC.BL.Persistenz.Klassen;

// **Die eine Stelle, an der eine Kartennummer vergeben wird.** Zwei Wege führen hierher: das
// Zuordnen einer einzelnen Karte und der WBS-Import, der viele Karten in einem Zug anlegt. Beide
// müssen dieselbe Regel benutzen — eine zweite Schreibweise gäbe zwei Karten dieselbe Identität,
// sobald sich an der Regel etwas ändert.
// Verbindung und Transaktion kommen von außen: der Import hält **eine** Transaktion über den
// ganzen Lauf, das Zuordnen eine über seine drei Fälle.
internal static class Kartenklassenzuordnungsschreiber
{
    // Erhöhen und Zuordnen unter **einem** Schloss: zwischen „nächste Nummer lesen“ und „Nummer
    // vergeben“ gäbe ein Fenster zwei Karten dieselbe Identität, und eine Identität, die zweimal
    // vorkommt, ist keine. RETURNING liefert den neuen Stand, gelesen und erhöht in einer Anweisung.
    public static int VergibNaechstenZaehlerstand(IDbConnection verbindung, IDbTransaction transaktion, long kartenklasseId)
    {
        return verbindung.ExecuteScalar<int>(@"
            UPDATE Kartenklasse
               SET Zaehlerstand = Zaehlerstand + 1
             WHERE KartenklasseId = @KartenklasseId
            RETURNING Zaehlerstand", new { KartenklasseId = kartenklasseId }, transaktion);
    }

    public static long FuegeZuordnungEin(IDbConnection verbindung, IDbTransaction transaktion, long karteId, long kartenklasseId, int zaehlerstand)
    {
        var parameter = new { Karte = karteId, Kartenklasse = kartenklasseId, Zaehlerstand = zaehlerstand };
        return verbindung.ExecuteScalar<long>(@"
            INSERT INTO Kartenklassenzuordnung (Karte, Kartenklasse, Zaehlerstand)
            VALUES (@Karte, @Kartenklasse, @Zaehlerstand);
            SELECT last_insert_rowid();", parameter, transaktion);
    }
}
