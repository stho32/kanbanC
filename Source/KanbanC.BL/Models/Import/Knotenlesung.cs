namespace KanbanC.BL.Models.Import;

// Zwei Lagen einer gelesenen Zeile, nicht eine mit null daneben: aus zwölf Zellen wird ein Knoten
// **oder** eine übersprungene Zeile mit Grund. Dieselbe Form wie Ergebnis und
// Dateiverweiseintragung — die Auskunft steckt im Typ und nicht in einer Nullprüfung.
public sealed class Knotenlesung
{
    private readonly Wbsknoten? _knoten;
    private readonly Uebersprungenezeile? _uebersprungene;

    private Knotenlesung(Wbsknoten? knoten, Uebersprungenezeile? uebersprungene)
    {
        _knoten = knoten;
        _uebersprungene = uebersprungene;
    }

    public static Knotenlesung Gelesen(Wbsknoten knoten)
    {
        return new Knotenlesung(knoten, null);
    }

    public static Knotenlesung Uebersprungen(Uebersprungenezeile uebersprungene)
    {
        return new Knotenlesung(null, uebersprungene);
    }

    public bool WurdeUebersprungen => _uebersprungene is not null;

    public Wbsknoten Knoten
    {
        get
        {
            if (_knoten is null)
            {
                throw new InvalidOperationException("Eine übersprungene Zeile hat keinen Knoten.");
            }

            return _knoten;
        }
    }

    public Uebersprungenezeile Uebersprungene
    {
        get
        {
            if (_uebersprungene is null)
            {
                throw new InvalidOperationException("Eine gelesene Zeile hat keinen Übersprungsgrund.");
            }

            return _uebersprungene;
        }
    }
}
