namespace KanbanC.Blazor.Components.Spalten;

// Die Fuge, auf die gerade gezielt wird: Bahn und Nummer der Fuge zwischen den Karten.
// „Kein Ziel“ ist ein null und nicht eine Kombination aus zwei Nullwerten.
public sealed record Ablageziel(long SpalteId, int Fuge);
