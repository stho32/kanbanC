namespace KanbanC.BL.Models.Import;

// Die Karten eines Laufs in der Reihenfolge, in der ihre Knoten in der Datei stehen. Die
// Reihenfolge ist zugleich die Reihenfolge der Kartennummern: wer die Datei liest, findet die
// Nummern in derselben Folge wieder.
public sealed class Kartenentwuerfe
{
    private readonly Kartenentwurf[] _entwuerfe;

    public Kartenentwuerfe(IEnumerable<Kartenentwurf> entwuerfe)
    {
        _entwuerfe = entwuerfe.ToArray();
    }

    public int Kartenanzahl => _entwuerfe.Length;

    public Kartenentwurf this[int index] => _entwuerfe[index];

    public IEnumerator<Kartenentwurf> GetEnumerator()
    {
        return ((IEnumerable<Kartenentwurf>)_entwuerfe).GetEnumerator();
    }

    public int Teilaufgabenanzahl
    {
        get
        {
            var anzahl = 0;
            foreach (var entwurf in _entwuerfe)
            {
                anzahl += entwurf.Teilaufgaben.Count;
            }

            return anzahl;
        }
    }
}
