using System.Globalization;
using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Die Meldung an einer Karte, deren Knoten nicht mehr in der Datei steht. **Gelöscht wird nie** —
// sie trägt Zeiten, Kommentare und Anhänge, die die Datei nie hatte. Genannt werden deshalb
// Nummer, Spalte, erfasste Zeit und Kommentarzahl, und daneben steht der Weg: archivieren.
public static class Verwaistengrund
{
    private const string OhneNummer = "Die Karte";
    private const string Minutenformat = "00";

    public static string Fuer(Karteniststand stand)
    {
        return $"{Benennung(stand)} steht nicht mehr in der Datei — unberührt geblieben, in „{stand.Spaltenbezeichnung}“, "
            + $"{AlsDauer(stand.ErfassteZeit)} erfasste Zeit, {Kommentarwortlaut(stand.Kommentarzahl)}. "
            + "Wenn sie weg soll: archivieren.";
    }

    private static string Benennung(Karteniststand stand)
    {
        if (stand.Kartennummer is null)
        {
            return OhneNummer;
        }

        return $"„{stand.Kartennummer}“";
    }

    // „h:mm“ mit Stunden über 24 hinaus, wie die Zeitbilanz der Oberfläche sie zeigt: eine Karte
    // sammelt Arbeitszeit über Wochen, und eine gekappte Summe machte aus einem sichtbaren
    // Erfassungsfehler eine unsichtbare Lüge.
    private static string AlsDauer(TimeSpan dauer)
    {
        var dieSpanneLaeuftRueckwaerts = dauer < TimeSpan.Zero;
        if (dieSpanneLaeuftRueckwaerts)
        {
            return "0:00";
        }

        var stunden = ((int)dauer.TotalHours).ToString(CultureInfo.InvariantCulture);
        var minuten = dauer.Minutes.ToString(Minutenformat, CultureInfo.InvariantCulture);
        return $"{stunden}:{minuten}";
    }

    private static string Kommentarwortlaut(int kommentarzahl)
    {
        if (kommentarzahl == 1)
        {
            return "1 Kommentar";
        }

        return $"{kommentarzahl.ToString(CultureInfo.InvariantCulture)} Kommentare";
    }
}
