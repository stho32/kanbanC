using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Operations.Auswertungen;

// **Wo die erfasste Zeit zum Sollband liegt — drei Lagen statt einer Differenz.** Gegen eine
// Spanne gerechnet gibt es innerhalb des Bandes keinen Abstand, den die Schätzung behauptet
// hätte; erst über der Obergrenze ist ein Überschuss eine ehrliche Zahl.
// Die Ränder gehören ins Band: genau die Untergrenze und genau die Obergrenze heißen `im Band`.
// **Ohne Sollband gibt es keine Abweichung** — nicht `im Band` und nicht `0`.
public static class Abweichungsrechner
{
    private const decimal MinutenJeStunde = 60m;

    public static Abweichung? Rechne(TimeSpan erfassteZeit, Zeitband? sollband)
    {
        if (sollband is null)
        {
            return null; // stil-check: C25 null heisst „ohne Soll gibt es nichts zu vergleichen“
        }

        var erfassteStunden = AlsStunden(erfassteZeit);
        var dieZeitBleibtUnterDerUntergrenze = erfassteStunden < sollband.VonStunden;
        if (dieZeitBleibtUnterDerUntergrenze)
        {
            return new Abweichung(Abweichungslage.UnterDemBand, null);
        }

        var dieZeitUeberschreitetDieObergrenze = erfassteStunden > sollband.BisStunden;
        if (dieZeitUeberschreitetDieObergrenze)
        {
            return new Abweichung(Abweichungslage.UeberDemBand, erfassteStunden - sollband.BisStunden);
        }

        return new Abweichung(Abweichungslage.ImBand, null);
    }

    // Gerundet wird auf die Minute genau in Stunden: die erfasste Zeit entsteht aus Zeitpunkten,
    // das Soll aus Zehntelstunden — ohne diese Umrechnung stünden zwei Größen gegeneinander, die
    // nie gleich sein können.
    private static decimal AlsStunden(TimeSpan erfassteZeit)
    {
        return (decimal)Math.Round(erfassteZeit.TotalMinutes) / MinutenJeStunde;
    }
}
