using KanbanC.BL.Models.Boards;

namespace KanbanC.BL.Models;

public sealed class Ergebnis<T>
{
    private readonly T? _wert;

    private Ergebnis(T? wert, Pruefbefunde befunde)
    {
        _wert = wert;
        Befunde = befunde;
    }

    public static Ergebnis<T> Erfolg(T wert)
    {
        return new Ergebnis<T>(wert, Pruefbefunde.Keine);
    }

    public static Ergebnis<T> Zurueckgewiesen(Pruefbefunde befunde)
    {
        if (befunde.IstOhneBefund)
        {
            throw new ArgumentException("Eine Zurückweisung braucht mindestens einen Befund.", nameof(befunde));
        }

        return new Ergebnis<T>(default, befunde);
    }

    public Pruefbefunde Befunde { get; }

    public bool IstErfolg => Befunde.IstOhneBefund;

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
}
