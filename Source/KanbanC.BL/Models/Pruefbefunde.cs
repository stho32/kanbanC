using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Models;

public sealed class Pruefbefunde
{
    private readonly Fehlerbefund[] _befunde;

    public Pruefbefunde(IEnumerable<Fehlerbefund> befunde)
    {
        _befunde = befunde.ToArray();
    }

    public static Pruefbefunde Keine => new([]);

    public bool IstOhneBefund => _befunde.Length == 0;

    public int BefundAnzahl => _befunde.Length;

    public Fehlerbefund this[int index] => _befunde[index];

    public IEnumerator<Fehlerbefund> GetEnumerator()
    {
        return ((IEnumerable<Fehlerbefund>)_befunde).GetEnumerator();
    }
}
