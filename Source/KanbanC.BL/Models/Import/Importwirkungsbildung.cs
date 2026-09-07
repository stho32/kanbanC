using KanbanC.Contracts.Import;

namespace KanbanC.BL.Models.Import;

// Was aus dem Soll-Ist-Vergleich für den Bericht **und** für den Schreiblauf folgt: die Wirkung je
// Knoten, die Zeilen der verwaisten Karten und die Aufträge, mit denen wiedererkannte Karten
// nachgezogen werden.
// **Vorschau und Schreiben rechnen dasselbe** — eine Vorschau, die anders rechnet als der dritte
// Schritt, verspräche etwas, das sie nicht hält.
public record Importwirkungsbildung(
    Kartenwirkungen Wirkungen,
    IReadOnlyList<Importzeile> Verwaistenzeilen, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<Kartenaktualisierungsauftrag> Aktualisierungen); // stil-check: C09 wie Spalte.Karten
