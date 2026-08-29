namespace KanbanC.BL.Models.Boards;

public record Spaltenvorlage(string Bezeichnung, int Position, bool IstAbschlussspalte, int? Anzeigegrenze);
