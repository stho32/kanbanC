using System.Globalization;

namespace KanbanC.Blazor.Services;

// Ab wie vielen nachgeholten Änderungen das Band nur noch zählt, statt jede geänderte Karte einzeln
// zu markieren. „Etwa zehn" ist eine **Größenordnung und kein Messwert** — wie die zehn Sekunden
// der Markenstandzeit. Ist die halbe Bahn markiert, ist die Marke keine Auskunft mehr, sondern
// Tapete, und die Zahl im Band sagt dasselbe kürzer.
// Die Zahl steht an dieser einen Stelle, damit ein Testlauf sie senken kann, statt als Literal im
// Renderzweig.
// Leer heißt „nicht gesetzt": der Schlüssel steht in appsettings.json, damit man ihn findet, und
// bleibt dort leer, damit der gewöhnliche Betrieb ohne Zutun die zehn nimmt.
public sealed record Aufschliessschwelle(int Anzahl)
{
    public const int Vorgabe = 10;

    public static Aufschliessschwelle Aus(string? anzahl)
    {
        var esWurdeKeineBrauchbareZahlGesetzt = !int.TryParse(anzahl, CultureInfo.InvariantCulture, out var gelesene) || gelesene <= 0;
        if (esWurdeKeineBrauchbareZahlGesetzt)
        {
            return new Aufschliessschwelle(Vorgabe);
        }

        return new Aufschliessschwelle(gelesene);
    }
}
