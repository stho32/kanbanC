namespace KanbanC.Contracts.Import;

// Die Bilanz eines Laufs samt einer Zeile je Knoten. Geaendert und Unveraendert sind in diesem
// Slice immer 0 — sie füllt die Wiedererkennung, und ein weggelassenes Fach wäre eine Antwort,
// die ihre eigene Fortsetzung verschweigt.
public record Importbericht(
    int Angelegt,
    int Geaendert,
    int Unveraendert,
    int Uebersprungen,
    Kartenzahlen Kartenzahlen,
    IReadOnlyList<Importzeile> Zeilen); // stil-check: C09 wie Spalte.Karten
