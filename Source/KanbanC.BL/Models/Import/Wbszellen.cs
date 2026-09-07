namespace KanbanC.BL.Models.Import;

// Die Zellen **einer** Tabellenzeile, so wie das Zerlegen sie gefunden hat — auch dann, wenn es
// nicht zwölf sind. Der Zerleger zerlegt, der Knotenleser urteilt: eine Zeile mit abweichender
// Zellenzahl wird gemeldet und nicht stillschweigend aufgefüllt.
public sealed class Wbszellen
{
    private readonly string[] _zellen;

    public Wbszellen(IEnumerable<string> zellen)
    {
        _zellen = zellen.ToArray();
    }

    public int Zellenanzahl => _zellen.Length;

    public string this[int index] => _zellen[index];

    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)_zellen).GetEnumerator();
    }
}
