using System.Globalization;

namespace KanbanC.Blazor.Services;

// Eine Zahl aus der Konfiguration als Zeitspanne. Leer, unlesbar oder nicht positiv heißt „nicht
// gesetzt": dann gilt die Vorgabe. Eine Anwendung, die wegen einer Standzeit nicht startet, wäre
// die schlechtere Antwort.
public static class Sekundenwert
{
    public static TimeSpan Aus(string? sekunden, TimeSpan vorgabe)
    {
        var esWurdeKeineBrauchbareZahlGesetzt = !double.TryParse(sekunden, CultureInfo.InvariantCulture, out var gelesene) || gelesene <= 0;
        if (esWurdeKeineBrauchbareZahlGesetzt)
        {
            return vorgabe;
        }

        return TimeSpan.FromSeconds(gelesene);
    }
}
