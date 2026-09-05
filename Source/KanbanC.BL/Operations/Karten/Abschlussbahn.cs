using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Operations.Karten;

// Die Abschlussspalte waechst monoton: alles, was je fertig wurde, sammelt sich dort. Sie zeigt
// deshalb die N neuesten, geordnet nach Erledigungsdatum. Karten ohne Datum stehen zuletzt und
// fallen als Erste heraus — sie stammen aus dem Bestand vor dieser Ordnung.
public static class Abschlussbahn
{
    public static IReadOnlyList<Spalte> Gekuerzt(IReadOnlyList<Spalte> spalten)
    {
        return spalten.Select(Gekuerzt).ToList();
    }

    private static Spalte Gekuerzt(Spalte spalte)
    {
        if (!spalte.IstAbschlussspalte)
        {
            return spalte;
        }

        var nachErledigung = NachErledigungsdatumAbsteigend(spalte.Karten);
        var dieBahnPasstInIhreGrenze = spalte.Anzeigegrenze is null || nachErledigung.Count <= spalte.Anzeigegrenze.Value;
        if (dieBahnPasstInIhreGrenze)
        {
            return spalte with { Karten = nachErledigung };
        }

        return spalte with { Karten = nachErledigung.Take(spalte.Anzeigegrenze!.Value).ToList() };
    }

    // Ein Nullable sortiert aufsteigend mit null zuerst; absteigend stehen die Karten ohne Datum
    // damit hinten. Innerhalb desselben Tages ordnet die Position der Spalte.
    private static IReadOnlyList<Karte> NachErledigungsdatumAbsteigend(IReadOnlyList<Karte> karten)
    {
        return karten.OrderByDescending(karte => karte.ErledigtAm).ThenBy(karte => karte.Position).ToList();
    }
}
