using KanbanC.Contracts.Zeiten;

namespace KanbanC.Blazor.Services;

// Die **eine** Rechenstelle des Zeitenblocks: Summen je Kontributor und Gesamtsumme aus einem
// Aufruf. Gerechnet in der Oberflächenschicht wie Teilaufgabenfortschritt und Laufplakette — ein
// Summenfeld am Kartendetail wäre eine zweite Wahrheit neben der Liste, die sie trägt, und ein
// SUM in SQL ein zweiter Leseweg, an dem „nur abgeschlossene" ein zweites Mal stünde.
// Summiert wird nur, was ein Ende hat: eine mitlaufende Dauer wäre ohne Live-Kanal ab der ersten
// Sekunde falsch. Wer bloß einen laufenden Timer hat, bekommt keine Zeile — eine „0:00" zu lesen
// ist keine Auskunft.
// Eine Komposition benannter Werte wie Kartendetail, keine gekapselte Sammlung: die Bilanz wird
// gerendert und nie auf Gleichheit verglichen.
// stil-check: C09 Komposition benannter Werte, nie auf Gleichheit verglichen
public sealed record Zeitbilanz(IReadOnlyList<Kontributorenzeitsumme> Kontributorensummen, TimeSpan Gesamtsumme)
{
    public static Zeitbilanz Fuer(IReadOnlyList<Zeiteintrag> zeiteintraege)
    {
        var jeKontributor = zeiteintraege.GroupBy(eintrag => eintrag.Kontributor.KontributorId);
        var mitAbgeschlossenem = jeKontributor.Where(HatAbgeschlossenenEintrag);
        var summen = mitAbgeschlossenem.Select(AlsZeitsumme).ToList();
        var geordnete = NachSummeAbsteigend(summen);
        return new Zeitbilanz(geordnete, Gesamt(geordnete));
    }

    private static bool HatAbgeschlossenenEintrag(IEnumerable<Zeiteintrag> eintraegeEinesKontributors)
    {
        return eintraegeEinesKontributors.Any(eintrag => eintrag.Ende is not null);
    }

    private static Kontributorenzeitsumme AlsZeitsumme(IEnumerable<Zeiteintrag> eintraegeEinesKontributors)
    {
        var eintraege = eintraegeEinesKontributors.ToList();
        var summe = new TimeSpan(eintraege.Sum(Gemessene));
        var laufende = eintraege.Count(eintrag => eintrag.Ende is null);
        return new Kontributorenzeitsumme(eintraege[0].Kontributor, summe, laufende);
    }

    // Ein laufender Eintrag trägt nichts bei, und eine Spanne, deren Ende vor ihrem Beginn läge,
    // ebenso wenig: negative Arbeitszeit gibt es nicht.
    private static long Gemessene(Zeiteintrag eintrag)
    {
        if (eintrag.Ende is null)
        {
            return 0;
        }

        var dauer = eintrag.Ende.Value - eintrag.Beginn;
        var dieSpanneLaeuftRueckwaerts = dauer < TimeSpan.Zero;
        if (dieSpanneLaeuftRueckwaerts)
        {
            return 0;
        }

        return dauer.Ticks;
    }

    // Die größte Summe oben — wer am meisten an der Karte gearbeitet hat, steht zuerst. Bei
    // Gleichstand entscheidet der Name, damit die Reihenfolge über zwei Aufrufe dieselbe bleibt.
    private static IReadOnlyList<Kontributorenzeitsumme> NachSummeAbsteigend(IReadOnlyList<Kontributorenzeitsumme> summen)
    {
        var nachSumme = summen.OrderByDescending(zeile => zeile.Summe);
        return nachSumme.ThenBy(zeile => zeile.Kontributor.Name, StringComparer.Ordinal).ToList();
    }

    private static TimeSpan Gesamt(IReadOnlyList<Kontributorenzeitsumme> summen)
    {
        return new TimeSpan(summen.Sum(zeile => zeile.Summe.Ticks));
    }
}
