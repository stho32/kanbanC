using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Services;

// Der Regler des zweiten Schritts: vier Stellungen, und neben jeder die Zahl der Karten, die sie
// ergäbe. **Die Zahl gehört ins Bild, bevor jemand sie erzeugt** — dieselbe Datei ergibt neun oder
// vierhundertfünfundvierzig Karten, und eine Zahl, die man erst nach dem Schreiben sieht, ist
// keine Auskunft mehr, sondern ein Befund.
public static class Schnittebenenwahl
{
    public static IReadOnlyList<Schnittebene> AlleEbenen { get; } =
    [
        Schnittebene.Dialog,
        Schnittebene.Interaction,
        Schnittebene.Feature,
        Schnittebene.Bubble,
    ];

    public static int Kartenzahl(Kartenzahlen zahlen, Schnittebene schnittebene)
    {
        return schnittebene switch
        {
            Schnittebene.Dialog => zahlen.Dialog,
            Schnittebene.Interaction => zahlen.Interaction,
            Schnittebene.Feature => zahlen.Feature,
            Schnittebene.Bubble => zahlen.Bubble,
            _ => throw new InvalidOperationException($"Die Schnittebene {schnittebene} ist nicht behandelt."),
        };
    }

    // „41 Karten" und nicht „41 Karte": eine Zeile, die bei eins falsch steht, liest sich wie ein
    // Fehler.
    public static string Kartenwortlaut(int kartenzahl)
    {
        if (kartenzahl == 1)
        {
            return "1 Karte";
        }

        return $"{kartenzahl} Karten";
    }

    // **Beide Zahlen**, nicht nur die angelegten: ein reiner Aktualisierungslauf hieße sonst
    // „0 Karten anlegen" und wäre über den Schirm nicht auszulösen.
    public static string Schreibbeschriftung(int angelegt, int geaendert)
    {
        if (geaendert == 0)
        {
            return $"{Kartenwortlaut(angelegt)} anlegen";
        }

        if (angelegt == 0)
        {
            return $"{geaendert} ändern";
        }

        return $"{angelegt} anlegen, {geaendert} ändern";
    }
}
