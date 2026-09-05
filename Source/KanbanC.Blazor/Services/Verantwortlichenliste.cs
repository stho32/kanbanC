using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Services;

// Wer als Verantwortlicher einer Karte zur Wahl steht. Schwester von Identitaetsliste — dieselbe
// Stilllegungsregel, aber ein anderer Zweck, und deshalb eine eigene Operation statt einer
// zweiten Bedeutung an derselben: die Identitätswahl lässt nur aktive Menschen zu, hier sind
// **alle Arten** wählbar. Ein Abgebildeter kann sich nicht selbst anmelden, aber jemand kann für
// ihn eine Karte führen — genau dafür gibt es die Art.
//
// Sie liegt in der Oberfläche und nicht in der BL, weil „wählbar" eine Regel der Auswahl ist, die
// es auf dem Server nicht gibt — dort wird nur zurückgewiesen, wer nicht gesetzt werden darf
// (KartenService). Und weil KanbanC.Blazor keine Projektreferenz auf KanbanC.BL hat und keine
// bekommt (CLAUDE.md, „Die eine Regel, die den Aufbau trägt").
public static class Verantwortlichenliste
{
    public static IReadOnlyList<Kontributor> Waehlbare(IReadOnlyList<Kontributor> kontributoren)
    {
        return kontributoren.Where(IstAktiv).ToList();
    }

    // Der bereits gesetzte, inzwischen stillgelegte Verantwortliche: er steht weiter an der Karte,
    // aber nicht mehr in der Auswahl. Das ist die zweite Hälfte des Fertig-Kriteriums von I0009 —
    // „verschwindet aus der Auswahl, bleibt aber an alten Karten sichtbar".
    public static Kontributor? StillgelegterTraeger(IReadOnlyList<Kontributor> kontributoren, Kontributor? verantwortlicher)
    {
        if (verantwortlicher is null || IstAktiv(verantwortlicher))
        {
            return null;
        }

        return verantwortlicher;
    }

    private static bool IstAktiv(Kontributor kontributor)
    {
        return kontributor.StillgelegtAm is null;
    }
}
