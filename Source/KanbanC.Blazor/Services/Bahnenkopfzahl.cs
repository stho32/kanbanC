using System.Globalization;
using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Services;

// Zeigt die Bahn nicht alle ihre Karten, nennt der Kopf die Zahl der gezeigten mit einem Plus:
// eine genaue Zahl über einer gekürzten Liste wäre eine Aussage über etwas, das nicht dasteht.
public static class Bahnenkopfzahl
{
    private const string NochMehrDahinter = "+";

    public static string AlsText(Spalte spalte)
    {
        var dieBahnZeigtNichtAlleKarten = spalte.Karten.Count < spalte.Kartenzahl;
        if (dieBahnZeigtNichtAlleKarten)
        {
            return spalte.Karten.Count.ToString(CultureInfo.InvariantCulture) + NochMehrDahinter;
        }

        return spalte.Kartenzahl.ToString(CultureInfo.InvariantCulture);
    }
}
