namespace KanbanC.BL.Models.Import;

// Die Anlagen eines Laufs, nachschlagbar über den Dateiverweis: „welche Karte entstand für diesen
// Verweis?". Sie ersetzt die Anzahl, die der Schreiblauf früher zurückgab — ein zweiter Weg zu
// derselben Zahl wäre eine zweite Wahrheit.
public sealed class Kartenanlageergebnisse
{
    private readonly Kartenanlageergebnis[] _ergebnisse;
    private readonly Dictionary<string, Kartenanlageergebnis> _jeDateiverweis; // stil-check: C11 Anlage je Dateiverweis, kein Domaenenbestand

    public Kartenanlageergebnisse(IEnumerable<Kartenanlageergebnis> ergebnisse)
    {
        _ergebnisse = ergebnisse.ToArray();
        _jeDateiverweis = new Dictionary<string, Kartenanlageergebnis>(StringComparer.Ordinal);
        foreach (var ergebnis in _ergebnisse)
        {
            _jeDateiverweis[ergebnis.Dateiverweis] = ergebnis;
        }
    }

    public Kartenanlageergebnis this[int stelle] => _ergebnisse[stelle];

    public int Anlagenzahl => _ergebnisse.Length;

    // null heißt „für diesen Dateiverweis ist in diesem Lauf keine Karte entstanden“ — die Zeile
    // wurde wiedererkannt, übersprungen oder ist das Zielboard.
    public Kartenanlageergebnis? Fuer(string dateiverweis)
    {
        if (_jeDateiverweis.TryGetValue(dateiverweis, out var ergebnis))
        {
            return ergebnis;
        }

        return null;
    }

    public IEnumerator<Kartenanlageergebnis> GetEnumerator()
    {
        foreach (var ergebnis in _ergebnisse)
        {
            yield return ergebnis;
        }
    }
}
