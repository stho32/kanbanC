using System.Globalization;

namespace KanbanC.Blazor.Services;

// Eine Zeitspanne als „h:mm" — Stunde ohne führende Null, Minute zweistellig.
// **Die Stunden laufen über 24 hinaus** („26:03", „30:00") statt in Tage umzubrechen: eine Karte
// sammelt Arbeitszeit über Wochen, überlappende Einträge sind erlaubt, und „1 Tag 6:00" wäre eine
// dritte Zeitform in derselben Spalte. Eine gekappte Summe machte aus einem sichtbaren
// Erfassungsfehler eine unsichtbare Lüge.
// Eine negative Spanne wird nie ausgegeben: „0:00" statt eines Minuszeichens — Uhren, die
// auseinanderlaufen, sind keine Aussage über die geleistete Arbeit.
public static class Dauerform
{
    private const string KeineZeit = "0:00";
    private const string Minutenformat = "00";

    public static string AlsText(TimeSpan dauer)
    {
        var dieSpanneLaeuftRueckwaerts = dauer < TimeSpan.Zero;
        if (dieSpanneLaeuftRueckwaerts)
        {
            return KeineZeit;
        }

        var stunden = ((int)dauer.TotalHours).ToString(CultureInfo.InvariantCulture);
        var minuten = dauer.Minutes.ToString(Minutenformat, CultureInfo.InvariantCulture);
        return $"{stunden}:{minuten}";
    }
}
