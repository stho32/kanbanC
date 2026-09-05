namespace KanbanC.BL.Operations.Karten;

public static class Teilaufgabentext
{
    // Nur die Randleerzeichen fallen weg, wie beim Etikett und beim Kartentitel. Groß- und
    // Kleinschreibung bleibt stehen, und die Leerzeichen im Text bleiben es auch: was jemand als
    // Schritt notiert, steht so da, wie er es geschrieben hat.
    public static string Normalisiert(string text)
    {
        return text.Trim();
    }
}
