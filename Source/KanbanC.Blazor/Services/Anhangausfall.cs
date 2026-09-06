namespace KanbanC.Blazor.Services;

// Was auf der Seite steht, wenn eine abgelegte Datei nicht angekommen ist. Muster WebApiAusfall,
// aber ausdrücklich ein anderer Text als dessen „Die WebApi ist nicht erreichbar": sie war
// erreichbar, die Datei war es nicht.
// Jede Meldung nennt den Dateinamen, sagt, dass die Datei nicht angehängt wurde, und nennt die
// Kompensationsaktion — derselbe Anspruch, den R00020 an die Fehlerantworten der API stellt, hier
// für die Oberfläche.
public static class Anhangausfall
{
    private const string ErneutAblegen = "Bitte die Datei erneut ablegen.";

    public static string Abbruchmeldung(string dateiname)
    {
        return $"„{dateiname}“ wurde nicht angehängt: die Übertragung ist abgebrochen. {ErneutAblegen}";
    }

    // Über die Ablegefläche entsteht dieser Fall nicht — sie ist während eines Anhängens gesperrt.
    // Wer sie umgeht, bekommt trotzdem eine Spur seines Vorgangs.
    public static string Sperrmeldung(string dateiname, string laufendeDatei)
    {
        return $"„{dateiname}“ wurde nicht angehängt: „{laufendeDatei}“ wird gerade angehängt. {ErneutAblegen}";
    }
}
