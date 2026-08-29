namespace KanbanC.Contracts.Boards;

public record Spalte(long SpalteId, string Bezeichnung, int Position, bool IstAbschlussspalte, int? Anzeigegrenze);
