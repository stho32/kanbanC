using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Operations.Boardimport;

// **Hier wird der Preis der Kontributoren-Entscheidung sichtbar gemacht, statt ihn zu
// verschweigen**: jeder Kontributor der Datei entsteht neu, auch wenn sein Name hier schon steht.
// Kontributor hat kein UNIQUE auf Name — zwei Menschen dürfen gleich heißen —, und ein Abgleich
// nach Namen wäre eine Vermutung, keine Identität: er schriebe Arbeit, Zeiten und Kommentare
// eines fremden Boards still einer hiesigen Person zu, und keine spätere Auswertung könnte das
// zurücknehmen.
// Verglichen wird ohne Rücksicht auf Groß- und Kleinschreibung, wie die Personenliste sie auch
// sortiert: „stefan“ neben „Stefan“ ist genau die Verwechslung, vor der der Hinweis warnt.
// Der Name steht **so oft, wie er entsteht** — zwei gleichnamige Personen in der Datei ergeben
// zwei Zeilen.
public static class Namensdubletten
{
    public static IReadOnlyList<string> Finde(IReadOnlyList<Kontributor> ausDerDatei, IReadOnlyList<Kontributor> vorhandene)
    {
        var vorhandeneNamen = vorhandene.Select(kontributor => kontributor.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var doppelte = new List<string>();
        foreach (var kontributor in ausDerDatei)
        {
            var denNamenGibtEsHierSchon = vorhandeneNamen.Contains(kontributor.Name);
            if (denNamenGibtEsHierSchon)
            {
                doppelte.Add(kontributor.Name);
            }
        }

        return doppelte;
    }
}
