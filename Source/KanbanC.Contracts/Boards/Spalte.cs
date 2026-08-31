using KanbanC.Contracts.Karten;

namespace KanbanC.Contracts.Boards;

public record Spalte(
    long SpalteId,
    string Bezeichnung,
    int Position,
    bool IstAbschlussspalte,
    int? Anzeigegrenze,
    IReadOnlyList<Karte> Karten); // stil-check: C09 wie Board.Spalten — die kartenlose Spalte teilt sich die leere Liste, dadurch bleibt die Werte-Gleichheit für sie erhalten
