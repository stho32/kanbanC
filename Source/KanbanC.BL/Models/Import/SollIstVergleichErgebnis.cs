namespace KanbanC.BL.Models.Import;

// Die vier Fächer eines Soll-Ist-Vergleichs. Das vierte heißt **Verwaist** und nicht ZuLoeschen:
// aus ihm wird nie gelöscht, und ein Name, der eine Löschung verspricht und keine ausführt, wäre
// eine unehrliche Schnittstelle.
public record SollIstVergleichErgebnis<TSoll, TIst>(
    IReadOnlyList<TSoll> ZuErstellen, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<(TSoll Soll, TIst Ist)> ZuAktualisieren, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<(TSoll Soll, TIst Ist)> Unveraendert, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<TIst> Verwaist) // stil-check: C09 wie Spalte.Karten
{
    public bool HatAenderungen => ZuErstellen.Count > 0 || ZuAktualisieren.Count > 0;
}
