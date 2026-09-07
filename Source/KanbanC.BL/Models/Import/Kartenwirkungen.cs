namespace KanbanC.BL.Models.Import;

// Die Wirkungen eines Laufs, nachschlagbar über die Knoten-ID: der Berichtsbildner trägt sie in
// die Zeile ein, die der Entwurfsbildner für diesen Knoten schon angelegt hat.
public sealed class Kartenwirkungen
{
    private readonly Dictionary<string, Kartenwirkung> _jeKnoten; // stil-check: C11 Wirkung je Knoten-ID, kein Domaenenbestand

    public Kartenwirkungen(IEnumerable<Kartenwirkung> wirkungen)
    {
        _jeKnoten = new Dictionary<string, Kartenwirkung>(StringComparer.Ordinal);
        foreach (var wirkung in wirkungen)
        {
            _jeKnoten[wirkung.KnotenId] = wirkung;
        }
    }

    // null heißt „zu diesem Knoten gehört keine Karte“ — er wurde Etikett, Teilaufgabe oder
    // übersprungen.
    public Kartenwirkung? Fuer(string knotenId)
    {
        if (_jeKnoten.TryGetValue(knotenId, out var wirkung))
        {
            return wirkung;
        }

        return null;
    }
}
