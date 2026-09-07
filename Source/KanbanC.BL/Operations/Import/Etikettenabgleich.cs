using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// **Die Klammer um die ganze Nachzieherei bei den Etiketten.** Der Import zieht nur nach, was aus
// der Datei stammen kann: ein Etikett der Karte, das kein Knotenname der Datei ist, hat ein Mensch
// gesetzt (`I0015`) und bleibt stehen.
public static class Etikettenabgleich
{
    public static Etikettenabgleichergebnis Gleiche(IReadOnlyList<string> entwuerfe, IReadOnlyList<string> vorhandene, IReadOnlySet<string> dateietiketten)
    {
        var sollmenge = new HashSet<string>(entwuerfe, StringComparer.Ordinal);
        var vorhandeneMenge = new HashSet<string>(vorhandene, StringComparer.Ordinal);

        var fremd = new List<string>();
        var zuEntfernen = new List<string>();
        var unveraendert = new List<string>();
        foreach (var etikett in vorhandene)
        {
            var dieDateiKannDiesesEtikettNichtErzeugen = !dateietiketten.Contains(etikett);
            if (dieDateiKannDiesesEtikettNichtErzeugen)
            {
                fremd.Add(etikett);
                continue;
            }

            if (sollmenge.Contains(etikett))
            {
                unveraendert.Add(etikett);
                continue;
            }

            zuEntfernen.Add(etikett);
        }

        var anzulegen = new List<string>();
        foreach (var etikett in entwuerfe)
        {
            var dieKarteTraegtDiesesEtikettNochNicht = !vorhandeneMenge.Contains(etikett);
            if (dieKarteTraegtDiesesEtikettNochNicht)
            {
                anzulegen.Add(etikett);
            }
        }

        return new Etikettenabgleichergebnis(anzulegen, zuEntfernen, unveraendert, fremd);
    }
}
