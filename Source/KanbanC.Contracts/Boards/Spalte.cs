using KanbanC.Contracts.Karten;

namespace KanbanC.Contracts.Boards;

public record Spalte(
    long SpalteId,
    string Bezeichnung,
    int Position,
    bool IstAbschlussspalte,
    int? Anzeigegrenze,
    IReadOnlyList<Karte> Karten); // stil-check: C09 wie Board.Spalten

// Innerhalb der BL teilen sich kartenlose Spalten dieselbe leere Liste, dadurch bleibt die
// Werte-Gleichheit des Records für sie erhalten. Nach dem Weg über JSON gilt das nicht:
// die Oberfläche baut je Abruf frische Listen, und der Record vergleicht diesen Member
// per Referenz. Wer sich auf Gleichheit verlässt, muss wissen, auf welcher Seite er steht.
