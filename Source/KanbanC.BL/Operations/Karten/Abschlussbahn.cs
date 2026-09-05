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

    // Dieselbe Ordnung ohne die Kürzung: wer die Bahn vollständig abruft, bekommt sie in der
    // Reihenfolge, in der sie angezeigt wird — sonst stünde die nachgeladene Bahn anders da als
    // die gekürzte, aus der sie hervorging.
    public static Spalte InAnzeigereihenfolge(Spalte spalte)
    {
        if (!spalte.IstAbschlussspalte)
        {
            return spalte;
        }

        return spalte with { Karten = NachErledigungsdatumAbsteigend(spalte.Karten) };
    }

    private static Spalte Gekuerzt(Spalte spalte)
    {
        var geordnete = InAnzeigereihenfolge(spalte);
        if (!spalte.IstAbschlussspalte)
        {
            return geordnete;
        }

        var dieBahnPasstInIhreGrenze = spalte.Anzeigegrenze is null || geordnete.Karten.Count <= spalte.Anzeigegrenze.Value;
        if (dieBahnPasstInIhreGrenze)
        {
            return geordnete;
        }

        return geordnete with { Karten = geordnete.Karten.Take(spalte.Anzeigegrenze!.Value).ToList() };
    }

    // Ein Nullable sortiert aufsteigend mit null zuerst; absteigend stehen die Karten ohne Datum
    // damit hinten. Innerhalb desselben Tages ordnet die Position der Spalte.
    private static IReadOnlyList<Karte> NachErledigungsdatumAbsteigend(IReadOnlyList<Karte> karten)
    {
        return karten.OrderByDescending(karte => karte.ErledigtAm).ThenBy(karte => karte.Position).ToList();
    }
}
