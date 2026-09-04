using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Services;

// Wählbar ist allein der aktive Mensch: ein Agent arbeitet über die API, ein abgebildeter
// Kontributor ist kein Akteur, und ein stillgelegter arbeitet gar nicht mehr mit. Gefiltert wird
// in der Oberfläche, weil „wählbar" eine Regel der Identitätswahl ist, die es auf dem Server
// nicht gibt; der Stilllegungsstand dagegen kommt vom Server und steht in jeder Antwortzeile.
// Ein Stillgelegter steht in keiner der beiden Listen — weder wählbar noch gesperrt.
public static class Identitaetsliste
{
    public static IReadOnlyList<Kontributor> Waehlbare(IReadOnlyList<Kontributor> kontributoren)
    {
        return Aktive(kontributoren).Where(IstEinMensch).ToList();
    }

    public static IReadOnlyList<Kontributor> Gesperrte(IReadOnlyList<Kontributor> kontributoren)
    {
        return Aktive(kontributoren).Where(IstKeinMensch).ToList();
    }

    private static IEnumerable<Kontributor> Aktive(IReadOnlyList<Kontributor> kontributoren)
    {
        return kontributoren.Where(IstAktiv);
    }

    private static bool IstAktiv(Kontributor kontributor)
    {
        return kontributor.StillgelegtAm is null;
    }

    private static bool IstKeinMensch(Kontributor kontributor)
    {
        return !IstEinMensch(kontributor);
    }

    private static bool IstEinMensch(Kontributor kontributor)
    {
        return kontributor.Art == Kontributorart.Mensch;
    }
}
