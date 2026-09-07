namespace KanbanC.Blazor.Services;

// Was in der Kopfzeile steht, solange die Ereignisleitung weg ist: „nicht live · Stand von 09:12".
// **null heißt „keine Marke":** steht die Leitung, bleibt die Stelle leer — dieselbe Regel wie bei
// Laufzaehler.Fuer. Der knappste Platz der Anwendung gehört dem, was gerade gilt, und Abwesenheit
// heißt „es steht".
// Ein **Zeitpunkt** statt einer mitzählenden Dauer: eine Zahl, die ohne Verbindung weiterläuft,
// wäre die eine, die sicher falsch ist — dieselbe Begründung wie bei Laufplakette. Die Ortszeitform
// kommt aus Zeitpunktform und wird nicht neu gerechnet.
public static class Verbindungsmarke
{
    private const string Wortlaut = "nicht live · Stand von";

    public static string? Fuer(Verbindungsstand stand)
    {
        if (stand.GetrenntSeit is null)
        {
            return null; // stil-check: C25 null heisst „keine Marke"
        }

        return $"{Wortlaut} {Zeitpunktform.AlsTageszeit(stand.GetrenntSeit.Value)}";
    }
}
