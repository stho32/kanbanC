namespace KanbanC.Blazor.Services;

// Die Ablegefläche des Boardimports kennt **einen** Sperrgrund: ein Lauf läuft noch. Anders als
// die Fläche an der Karte braucht sie keine gewählte Identität — der Import hat keinen Urheber,
// die Personen entstehen erst mit der Datei.
// Die Sperre hält die laufende Übertragung unantastbar: ein zweites change-Ereignis ersetzt im
// Browser die Zuordnung von der Datei-Nummer auf die Datei, und die Bytes der ersten kämen dann
// nirgends an.
public static class Boardablegeflaeche
{
    public const string Ablegeaufforderung = "Boarddatei hierher ziehen oder wählen";

    public static bool IstGesperrt(string? laufendeDatei)
    {
        return laufendeDatei is not null;
    }

    // Während des Laufs sagt die Fläche, welche Datei gerade gelesen wird: eine Sperre ohne Grund
    // wäre nur grau.
    public static string Text(string? laufendeDatei)
    {
        if (laufendeDatei is null)
        {
            return Ablegeaufforderung;
        }

        return $"„{laufendeDatei}“ wird gelesen …";
    }
}
