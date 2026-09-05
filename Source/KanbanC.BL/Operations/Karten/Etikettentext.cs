namespace KanbanC.BL.Operations.Karten;

public static class Etikettentext
{
    // Nur die Randleerzeichen fallen weg. Groß- und Kleinschreibung bleibt stehen: „Refactoring"
    // und „refactoring" sind zwei Etiketten. Die Vervollständigung macht abweichende
    // Schreibweisen sichtbar — sie verhindert sie nicht.
    public static string Normalisiert(string text)
    {
        return text.Trim();
    }
}
