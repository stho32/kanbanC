namespace KanbanC.BL.Operations.Klassen;

// Das Präfix wird genommen, wie es getippt wurde — der Trenner ist Teil davon, die Anwendung
// hängt nichts an und schreibt nichts um; nur die Ränder fallen weg. Verglichen wird trotzdem
// ohne Rücksicht auf Groß-/Kleinschreibung: wbs- und WBS- auf einem Board erzeugten Nummern, die
// gleich aussehen und es nicht sind. Dasselbe Verhältnis wie bei Spaltenbezeichnung.
public static class Kartenklassenpraefix
{
    public static string Normalisiert(string praefix)
    {
        return praefix.Trim();
    }

    public static bool SindGleich(string eines, string anderes)
    {
        return string.Equals(Normalisiert(eines), Normalisiert(anderes), StringComparison.OrdinalIgnoreCase);
    }
}
