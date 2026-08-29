using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Services;

public sealed class ApiErgebnis<T>
{
    private readonly T? _wert;
    private readonly Zurueckweisung? _zurueckweisung;

    private ApiErgebnis(T? wert, Zurueckweisung? zurueckweisung)
    {
        _wert = wert;
        _zurueckweisung = zurueckweisung;
    }

    public static ApiErgebnis<T> Erfolg(T wert)
    {
        return new ApiErgebnis<T>(wert, null);
    }

    public static ApiErgebnis<T> Zurueckgewiesen(Zurueckweisung zurueckweisung)
    {
        return new ApiErgebnis<T>(default, zurueckweisung);
    }

    public bool WurdeZurueckgewiesen => _zurueckweisung is not null;

    public T Wert
    {
        get
        {
            if (_wert is null)
            {
                throw new InvalidOperationException("Ein zurückgewiesenes Ergebnis hat keinen Wert.");
            }

            return _wert;
        }
    }

    public Zurueckweisung Zurueckweisung
    {
        get
        {
            if (_zurueckweisung is null)
            {
                throw new InvalidOperationException("Ein erfolgreiches Ergebnis hat keine Zurückweisung.");
            }

            return _zurueckweisung;
        }
    }
}
