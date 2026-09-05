namespace KanbanC.BL.Operations.Karten;

public static class Kommentartext
{
    // Nur die Randleerzeichen fallen weg, wie beim Etikett, beim Kartentitel und bei der
    // Teilaufgabe. Groß- und Kleinschreibung bleibt stehen, und die Leerzeichen im Text bleiben
    // es auch: was jemand zur Karte sagt, steht so da, wie er es geschrieben hat.
    public static string Normalisiert(string text)
    {
        return text.Trim();
    }
}
