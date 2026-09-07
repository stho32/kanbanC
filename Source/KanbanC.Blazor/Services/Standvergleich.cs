using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Services;

// Welche Karten sich zwischen zwei Bildern desselben Boards bewegt haben. **Hier fällt die Zahl
// „4 Änderungen" an** — nicht aus nachgespielten Ereignissen: es gibt kein Ereignisjournal, und
// aus einem Vergleich fällt genau das an, was ein Vergleich weiß.
// **Verglichen wird die Lage** — Spalte und Position —, weil das Fertig-Kriterium die Bewegung
// meint. Eine neu erschienene und eine verschwundene Karte zählen mit, eine unverändert liegende
// nicht.
// Rein rechnend und ohne Uhr, damit der Vergleich ohne Kreislauf prüfbar ist.
public static class Standvergleich
{
    public static Standunterschied Geaenderte(Board alt, Board neu)
    {
        var alteStellen = StellenDerKarten(alt);
        var neueStellen = StellenDerKarten(neu);
        var unveraenderteKarten = alteStellen.Intersect(neueStellen).Select(stelle => stelle.KarteId);
        var beruehrteStellen = alteStellen.Concat(neueStellen);
        var beruehrteKarten = beruehrteStellen.Select(stelle => stelle.KarteId).Distinct();
        return new Standunterschied(beruehrteKarten.Except(unveraenderteKarten));
    }

    private static IReadOnlyList<Kartenstelle> StellenDerKarten(Board board)
    {
        return board.Spalten.SelectMany(AlsStellen).ToList();
    }

    private static IEnumerable<Kartenstelle> AlsStellen(Spalte spalte)
    {
        return spalte.Karten.Select(karte => new Kartenstelle(karte.KarteId, spalte.SpalteId, karte.Position));
    }

    // Wo eine Karte liegt, als vergleichbarer Wert: der Wertevergleich des records ist der
    // Vergleich der Lage.
    private sealed record Kartenstelle(long KarteId, long SpalteId, int Position);
}
