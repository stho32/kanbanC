namespace KanbanC.BL.Models.Boards;

public sealed class Pruefbefunde
{
    private readonly string[] _meldungen;

    public Pruefbefunde(IEnumerable<string> meldungen)
    {
        _meldungen = meldungen.ToArray();
    }

    public static Pruefbefunde Keine => new([]);

    public bool IstOhneBefund => _meldungen.Length == 0;

    public int BefundAnzahl => _meldungen.Length;

    public string this[int index] => _meldungen[index];

    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)_meldungen).GetEnumerator();
    }
}
