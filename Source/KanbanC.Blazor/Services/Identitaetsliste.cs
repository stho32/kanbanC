using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Services;

// Wählbar ist allein die Art Mensch: ein Agent arbeitet über die API, ein abgebildeter
// Kontributor ist kein Akteur. Gefiltert wird in der Oberfläche, weil „wählbar" eine Regel der
// Identitätswahl ist, die es auf dem Server nicht gibt.
public static class Identitaetsliste
{
    public static IReadOnlyList<Kontributor> Waehlbare(IReadOnlyList<Kontributor> kontributoren)
    {
        return kontributoren.Where(IstEinMensch).ToList();
    }

    private static bool IstEinMensch(Kontributor kontributor)
    {
        return kontributor.Art == Kontributorart.Mensch;
    }
}
