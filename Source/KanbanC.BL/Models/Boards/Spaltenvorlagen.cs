namespace KanbanC.BL.Models.Boards;

public sealed class Spaltenvorlagen
{
    private readonly Spaltenvorlage[] _spalten;

    public Spaltenvorlagen(IEnumerable<Spaltenvorlage> spalten)
    {
        _spalten = spalten.ToArray();
    }

    public int SpaltenAnzahl => _spalten.Length;

    public Spaltenvorlage this[int index] => _spalten[index];

    public IEnumerator<Spaltenvorlage> GetEnumerator()
    {
        return ((IEnumerable<Spaltenvorlage>)_spalten).GetEnumerator();
    }
}
