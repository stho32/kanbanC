namespace KanbanC.Contracts.Export;

// Die Spalte, wie die Datei sie trägt. **Ohne Kartenliste und ohne Kartenzahl**, anders als
// Spalte: der Ort jeder Karte steht an der Karte, und eine „Kartenzahl: 0" neben den Karten
// derselben Datei wäre genau die stille Lüge, die Spalte.Kartenzahl verhindern soll.
// Die Anzeigegrenze wird **berichtet**, nicht **angewendet** — gekürzt wird in dieser Datei
// nichts.
public record Exportspalte(
    long SpalteId,
    string Bezeichnung,
    int Position,
    bool IstAbschlussspalte,
    int? Anzeigegrenze);
