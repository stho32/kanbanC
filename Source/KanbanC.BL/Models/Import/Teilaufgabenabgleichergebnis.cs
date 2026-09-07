namespace KanbanC.BL.Models.Import;

// Was mit den Teilaufgaben einer wiedererkannten Karte geschieht. **Fremd** ist das Fach, das den
// Menschen schützt: eine Teilaufgabe ohne Knoten-ID vorn hat er selbst angelegt, sie steht
// außerhalb des Vergleichs und wird nie geändert und nie entfernt.
public record Teilaufgabenabgleichergebnis(
    IReadOnlyList<Teilaufgabenentwurf> Anzulegen, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<Teilaufgabenaenderung> ZuAendern, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<Teilaufgabenstand> Unveraendert, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<Teilaufgabenstand> ZuEntfernen, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<Teilaufgabenstand> Fremd); // stil-check: C09 wie Spalte.Karten
