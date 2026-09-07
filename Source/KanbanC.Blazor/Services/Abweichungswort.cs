using KanbanC.Contracts.Auswertungen;

namespace KanbanC.Blazor.Services;

// Die Abweichung als Satz: „unter dem Band", „im Band", „über dem Band um 6,0 h".
// **Ohne Sollband gibt es keine Abweichung** — dort steht derselbe Gedankenstrich wie in der
// Soll-Spalte und nicht „im Band".
public static class Abweichungswort
{
    public static string AlsText(Abweichung? abweichung)
    {
        if (abweichung is null)
        {
            return Zeitbandform.OhneBand;
        }

        return abweichung.Lage switch
        {
            Abweichungslage.UnterDemBand => "unter dem Band",
            Abweichungslage.ImBand => "im Band",
            Abweichungslage.UeberDemBand => $"über dem Band um {Zeitbandform.Stunden(abweichung.UeberschussStunden!.Value)} h",
            _ => throw new InvalidOperationException($"Die Abweichungslage {abweichung.Lage} ist nicht behandelt."),
        };
    }
}
