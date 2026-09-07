using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// **Die eine Rechenstelle des Solls:** das Sollband einer Karte ist die Summe ihres Teilbaums —
// der eigene Aufwand plus der aller Nachfahren. Ohne diesen Schritt bliebe die Spalte auf
// Interaction-Schnitt leer, weil eine Interaction-Zeile in der WBS keinen eigenen Aufwand trägt;
// geschätzt wird auf Bubble-Ebene.
// Gerechnet wird auf dem **gefilterten** Baum: der Umfangsfilter hat verworfen, option und ausbau
// samt Teilbaum bereits entfernt, und die Summierung erbt den Ausschluss. Ihn hier ein zweites Mal
// zu formulieren wäre dieselbe Regel an zwei Orten.
// Eine Karte, unter der keine Zeile einen Aufwand schätzt, steht **ohne** Band in der Antwort und
// nicht bei 0,0–0,0 Stunden.
public static class Sollbandrechner
{
    public static IReadOnlyDictionary<string, Sollband> RechneJeKartenknoten(Wbsbaum baum, IReadOnlyList<Wbsknoten> kartenknoten)
    {
        var jeKartenknoten = new Dictionary<string, Sollband>(StringComparer.Ordinal); // stil-check: C11 Sollband je Kartenkennung, kein Domaenenbestand
        foreach (var knoten in kartenknoten)
        {
            var band = Sollband.Summe(BaenderDesTeilbaums(baum, knoten));
            if (band is not null)
            {
                jeKartenknoten[knoten.Id] = band;
            }
        }

        return jeKartenknoten;
    }

    private static IReadOnlyList<Sollband> BaenderDesTeilbaums(Wbsbaum baum, Wbsknoten knoten)
    {
        var baender = new List<Sollband>();
        SammleBand(knoten, baender);
        foreach (var nachfahr in baum.NachfahrenInDateireihenfolge(knoten.Id))
        {
            SammleBand(nachfahr, baender);
        }

        return baender;
    }

    private static void SammleBand(Wbsknoten knoten, List<Sollband> baender)
    {
        var band = Aufwandsband.Lies(knoten.Aufwand);
        if (band is not null)
        {
            baender.Add(band);
        }
    }
}
