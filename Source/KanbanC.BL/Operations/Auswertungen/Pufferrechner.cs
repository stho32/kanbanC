using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Operations.Auswertungen;

// **Was eine Karte an Puffer mitbringt und was sie davon verbraucht hat.** Der Puffer ist die
// Spanne ihres Bandes, der Verbrauch die Überschreitung ihrer Untergrenze.
// Zwei Versuchungen werden ausdrücklich nicht ergriffen. **Kein Deckel nach oben**: über der
// Obergrenze bleibt der Verbrauch die volle Überschreitung, sonst liefe der Anteil der Kette nie
// über 100 % und die rote Zone der Fieberkurve verschwände. **Keine Gegenrechnung nach unten**:
// eine Karte unter ihrer Untergrenze verbraucht 0,0 und nicht negativ, sonst verdeckte die
// Untätigkeit der einen den Überzug der anderen.
// **Ohne Sollband gibt es weder Puffer noch Verbrauch** — dieselbe Regel wie beim
// Abweichungsrechner.
public static class Pufferrechner
{
    private const decimal MinutenJeStunde = 60m;

    public static Kartenpuffer? Rechne(TimeSpan erfassteZeit, Zeitband? sollband)
    {
        if (sollband is null)
        {
            return null; // stil-check: C25 null heisst „ohne Soll gibt es keinen Puffer zu verbrauchen"
        }

        var pufferStunden = sollband.BisStunden - sollband.VonStunden;
        var ueberschreitungDerUntergrenze = AlsStunden(erfassteZeit) - sollband.VonStunden;
        var dieZeitBleibtUnterDerUntergrenze = ueberschreitungDerUntergrenze < 0m;
        if (dieZeitBleibtUnterDerUntergrenze)
        {
            return new Kartenpuffer(pufferStunden, 0m);
        }

        return new Kartenpuffer(pufferStunden, ueberschreitungDerUntergrenze);
    }

    // Dieselbe Umrechnung wie im Abweichungsrechner: die erfasste Zeit entsteht aus Zeitpunkten,
    // das Soll aus Zehntelstunden — ohne sie stünden zwei Größen gegeneinander, die nie gleich
    // sein können.
    private static decimal AlsStunden(TimeSpan erfassteZeit)
    {
        return (decimal)Math.Round(erfassteZeit.TotalMinutes) / MinutenJeStunde;
    }
}
