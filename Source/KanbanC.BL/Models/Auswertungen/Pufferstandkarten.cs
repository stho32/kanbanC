namespace KanbanC.BL.Models.Auswertungen;

// Die Karten eines Bestands in Kartennummernfolge. Was aus ihnen für die Kette folgt, beantwortet
// die Pufferkette — hier steht nur die Menge selbst.
public sealed class Pufferstandkarten
{
    private readonly Pufferstandkarte[] _karten;

    public Pufferstandkarten(IEnumerable<Pufferstandkarte> karten)
    {
        _karten = karten.ToArray();
    }

    public int Kartenanzahl => _karten.Length;

    public Pufferstandkarte this[int index] => _karten[index];

    public IEnumerator<Pufferstandkarte> GetEnumerator()
    {
        return ((IEnumerable<Pufferstandkarte>)_karten).GetEnumerator();
    }
}
