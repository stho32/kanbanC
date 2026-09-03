namespace KanbanC.Blazor.Components.Karten;

// Der abgeschlossene Zug: welche Karte auf welcher Stelle welcher Bahn abgelegt wurde.
public sealed record Kartenablage(long KarteId, long SpalteId, int Position);
