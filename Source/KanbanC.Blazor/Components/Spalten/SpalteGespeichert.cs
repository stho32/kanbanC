namespace KanbanC.Blazor.Components.Spalten;

public record SpalteGespeichert(long SpalteId, string Bezeichnung, bool IstAbschlussspalte, int? Anzeigegrenze);
