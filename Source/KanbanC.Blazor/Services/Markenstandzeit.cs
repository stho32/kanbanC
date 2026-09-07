namespace KanbanC.Blazor.Services;

// Wie lange eine Einflugmarke an einer Karte steht. „Etwa zehn Sekunden" ist eine **Größenordnung
// und kein Messwert** — eine Marke, die bleibt, ist keine Nachricht mehr, sondern ein Verlauf.
// Die Zahl steht an dieser einen Stelle, damit ein Testlauf sie kürzen kann: kein E2E-Lauf darf
// zehn Sekunden warten müssen, nur um zu sehen, dass eine Marke von selbst vergeht.
// Leer heißt „nicht gesetzt": der Schlüssel steht in appsettings.json, damit man ihn findet, und
// bleibt dort leer, damit der gewöhnliche Betrieb ohne Zutun die zehn Sekunden nimmt.
public sealed record Markenstandzeit(TimeSpan Dauer)
{
    public static readonly TimeSpan Vorgabe = TimeSpan.FromSeconds(10);

    public static Markenstandzeit Aus(string? sekunden)
    {
        return new Markenstandzeit(Sekundenwert.Aus(sekunden, Vorgabe));
    }
}
