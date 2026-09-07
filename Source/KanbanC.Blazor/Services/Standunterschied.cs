namespace KanbanC.Blazor.Services;

// Die Karten, die zwischen dem alten und dem frisch geholten Bild ihre Lage gewechselt haben, samt
// ihrer Zahl — daher kommt die Zahl im Aufschließband.
// Kein record: der synthetisierte Vergleich sähe nur die Referenz der inneren Liste (C09).
public sealed class Standunterschied
{
    private readonly long[] _geaenderteKarten;

    public Standunterschied(IEnumerable<long> geaenderteKarten)
    {
        _geaenderteKarten = geaenderteKarten.ToArray();
    }

    public int Anzahl => _geaenderteKarten.Length;

    public IEnumerator<long> GetEnumerator()
    {
        return ((IEnumerable<long>)_geaenderteKarten).GetEnumerator();
    }
}
