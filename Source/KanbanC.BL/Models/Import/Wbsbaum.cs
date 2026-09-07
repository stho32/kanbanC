namespace KanbanC.BL.Models.Import;

// Die Knoten einer WBS-Datei in **Dateireihenfolge**, mit ihren Eltern-Kind-Beziehungen. Die
// Reihenfolge ist die der Datei und keine sortierte: die Datei ist die Wahrheit über den Umfang,
// und eine umsortierte Liste wäre eine zweite Aussage über sie.
public sealed class Wbsbaum
{
    private readonly Wbsknoten[] _knoten;
    private readonly Dictionary<string, Wbsknoten> _knotenJeId; // stil-check: C11 Nachschlagewerk der Baumbildung, kein Domaenenbestand
    private readonly Dictionary<string, List<Wbsknoten>> _kinderJeEltern; // stil-check: C11 Nachschlagewerk der Baumbildung, kein Domaenenbestand

    public Wbsbaum(IEnumerable<Wbsknoten> knoten)
    {
        _knoten = knoten.ToArray();
        _knotenJeId = _knoten.ToDictionary(vorhandener => vorhandener.Id, StringComparer.Ordinal);
        _kinderJeEltern = [];
        foreach (var kind in _knoten)
        {
            if (!_kinderJeEltern.TryGetValue(kind.Eltern, out var geschwister))
            {
                geschwister = [];
                _kinderJeEltern[kind.Eltern] = geschwister;
            }

            geschwister.Add(kind);
        }
    }

    public int KnotenAnzahl => _knoten.Length;

    public Wbsknoten this[int index] => _knoten[index];

    public IEnumerator<Wbsknoten> GetEnumerator()
    {
        return ((IEnumerable<Wbsknoten>)_knoten).GetEnumerator();
    }

    public IReadOnlyList<Wbsknoten> Kinder(string knotenId)
    {
        if (_kinderJeEltern.TryGetValue(knotenId, out var kinder))
        {
            return kinder;
        }

        return [];
    }

    // In Dateireihenfolge und über alle Ebenen hinweg **flach**: eine Teilaufgabenliste kennt
    // keine Einrückung, und die ID vorn sagt ohnehin, welche Ebene der Schritt hatte.
    public IReadOnlyList<Wbsknoten> NachfahrenInDateireihenfolge(string knotenId)
    {
        var gesammelte = new List<Wbsknoten>();
        SammleNachfahren(knotenId, gesammelte);
        gesammelte.Sort((links, rechts) => links.Zeilennummer.CompareTo(rechts.Zeilennummer));
        return gesammelte;
    }

    private void SammleNachfahren(string knotenId, List<Wbsknoten> gesammelte)
    {
        foreach (var kind in Kinder(knotenId))
        {
            gesammelte.Add(kind);
            SammleNachfahren(kind.Id, gesammelte);
        }
    }

    // Die Frage, an der die Schnittebene zur Untergrenze wird: ein Knoten oberhalb ohne Nachfahren
    // auf der Schnittebene wird selbst zur Karte, statt mit seinem Teilbaum still zu verschwinden.
    public bool HatNachfahrenAuf(string knotenId, Wbsebene ebene)
    {
        foreach (var kind in Kinder(knotenId))
        {
            if (kind.Ebene == ebene)
            {
                return true;
            }

            if (HatNachfahrenAuf(kind.Id, ebene))
            {
                return true;
            }
        }

        return false;
    }

    // Die Vorfahren vom nächsten Elternteil aufwärts, in Leserichtung von oben: der Dialog steht
    // vor der Interaction, damit die Etiketten einer Karte so herum stehen, wie ein Mensch den
    // Baum liest.
    public IReadOnlyList<Wbsknoten> VorfahrenVonObenNachUnten(Wbsknoten knoten)
    {
        var vorfahren = new List<Wbsknoten>();
        var elternId = knoten.Eltern;
        while (ElternKnoten(elternId) is { } eltern)
        {
            vorfahren.Add(eltern);
            elternId = eltern.Eltern;
        }

        vorfahren.Reverse();
        return vorfahren;
    }

    private Wbsknoten? ElternKnoten(string elternId)
    {
        if (_knotenJeId.TryGetValue(elternId, out var eltern))
        {
            return eltern;
        }

        return null;
    }

    public bool KenntKnoten(string knotenId)
    {
        return _knotenJeId.ContainsKey(knotenId);
    }

    // null heißt „diesen Knoten führt die Datei nicht“.
    public Wbsknoten? Knoten(string knotenId)
    {
        if (_knotenJeId.TryGetValue(knotenId, out var knoten))
        {
            return knoten;
        }

        return null;
    }
}
