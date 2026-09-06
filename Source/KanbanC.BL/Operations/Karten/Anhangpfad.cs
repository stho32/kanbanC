using System.Globalization;
using Microsoft.Data.Sqlite;

namespace KanbanC.BL.Operations.Karten;

// Wo die Bytes eines Anhangs liegen. Gerechnet aus der Verbindungszeichenfolge und nirgends
// gespeichert: eine Spalte für den Ablagepfad wäre eine zweite Wahrheit und liefe beim ersten
// Verschieben der Datenbankdatei auseinander.
// Der Ordner heißt nach dem **vollen** Dateinamen der Datenbank samt „.db". Anhängen ist eine
// Rechnung ohne Regel, Abschneiden bräuchte eine („welche Endung?") — und „kanbanc.db" und ein
// späteres „kanbanc.sqlite" fielen sonst auf denselben Ordner.
// Pure Logik: diese Klasse fasst keine Datei an, sie rechnet nur einen Namen aus.
public static class Anhangpfad
{
    private const string Ordnerzusatz = "-Files";

    public static string FuerKarte(string verbindungszeichenfolge, long karteId)
    {
        var ablageordner = Datenbankdatei(verbindungszeichenfolge) + Ordnerzusatz;
        return Path.Combine(ablageordner, Nummerntext(karteId));
    }

    public static string FuerAnhang(string verbindungszeichenfolge, long karteId, long anhangId)
    {
        return Path.Combine(FuerKarte(verbindungszeichenfolge, karteId), Nummerntext(anhangId));
    }

    // Ohne „Data Source" gäbe es keinen Ort, neben dem der Ablageordner läge — ein Ordner im
    // Arbeitsverzeichnis wäre geraten, und niemand fände ihn wieder. Deshalb sichtbar scheitern.
    private static string Datenbankdatei(string verbindungszeichenfolge)
    {
        var bauer = new SqliteConnectionStringBuilder(verbindungszeichenfolge);
        var dieZeichenfolgeNenntKeineDatei = string.IsNullOrWhiteSpace(bauer.DataSource);
        if (dieZeichenfolgeNenntKeineDatei)
        {
            throw new InvalidOperationException($"Die Verbindungszeichenfolge „{verbindungszeichenfolge}“ nennt kein „Data Source“; der Ablageordner der Anhänge lässt sich daraus nicht rechnen.");
        }

        return bauer.DataSource;
    }

    private static string Nummerntext(long nummer)
    {
        return nummer.ToString(CultureInfo.InvariantCulture);
    }
}
