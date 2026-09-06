namespace KanbanC.BL.Operations.Karten;

public static class Anhangname
{
    // Mehr als die Randleerzeichen des Kommentar- und Teilaufgabentextes: ein Browser meldet je
    // nach Plattform „C:\Temp\a.md" oder „ordner/a.md", und die Zeile soll den Dateinamen zeigen
    // und nicht den Weg eines fremden Rechners. Beide Trenner fallen weg, weil der meldende
    // Rechner ein anderer sein kann als der lesende.
    // Das ist keine Sicherheitsmaßnahme: die Datei auf der Platte heißt nach ihrer Nummer, der
    // gemeldete Name berührt das Dateisystem nie.
    public static string Normalisiert(string dateiname)
    {
        var ohneRandleerzeichen = dateiname.Trim();
        var letzterTrenner = ohneRandleerzeichen.LastIndexOfAny(['/', '\\']);
        var derNameTraegtEinenWeg = letzterTrenner >= 0;
        if (derNameTraegtEinenWeg)
        {
            return ohneRandleerzeichen[(letzterTrenner + 1)..].Trim();
        }

        return ohneRandleerzeichen;
    }
}
