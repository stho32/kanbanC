using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Services;

// Wie eine Art in der Liste erscheint: Beschriftung der Plakette und die Farbrolle, an der die
// drei Arten auseinanderzuhalten sind — Mensch olivgrün, Agent terrakotta, abgebildet neutral.
public static class Kontributorartform
{
    public static string Beschriftung(Kontributorart art)
    {
        switch (art)
        {
            case Kontributorart.Mensch:
                return "Mensch";
            case Kontributorart.Agent:
                return "Agent";
            case Kontributorart.Abgebildet:
                return "abgebildet";
            default:
                throw new ArgumentOutOfRangeException(nameof(art), art, "Diese Kontributorart hat keine Beschriftung.");
        }
    }

    public static string Plakettenklasse(Kontributorart art)
    {
        switch (art)
        {
            case Kontributorart.Mensch:
                return "tag-accent-2";
            case Kontributorart.Agent:
                return "tag-accent";
            case Kontributorart.Abgebildet:
                return "tag-neutral";
            default:
                throw new ArgumentOutOfRangeException(nameof(art), art, "Diese Kontributorart hat keine Plakette.");
        }
    }

    public static string Kuerzelklasse(Kontributorart art)
    {
        switch (art)
        {
            case Kontributorart.Mensch:
                return "kuerzel-mensch";
            case Kontributorart.Agent:
                return "kuerzel-agent";
            case Kontributorart.Abgebildet:
                return "kuerzel-abgebildet";
            default:
                throw new ArgumentOutOfRangeException(nameof(art), art, "Diese Kontributorart hat kein Kürzel.");
        }
    }

    // Die Initialen der ersten beiden Namensteile; ein einteiliger Name gibt seine ersten beiden
    // Buchstaben her.
    public static string Kuerzel(string name)
    {
        var namensteile = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var derNameIstLeer = namensteile.Length == 0;
        if (derNameIstLeer)
        {
            return string.Empty;
        }

        var derNameIstEinteilig = namensteile.Length == 1;
        if (derNameIstEinteilig)
        {
            var anfang = namensteile[0];
            if (anfang.Length == 1)
            {
                return anfang.ToUpperInvariant();
            }

            return anfang[..2].ToUpperInvariant();
        }

        return $"{namensteile[0][0]}{namensteile[1][0]}".ToUpperInvariant();
    }
}
