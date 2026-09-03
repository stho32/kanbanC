namespace KanbanC.Blazor.Components.Karten;

// Der laufende Zug: welche Karte aus welcher Bahn. Ein eigener Typ, damit „kein Zug läuft“
// ein null ist und nicht eine Kombination aus zwei Nullwerten.
public sealed record Kartenzug(long KarteId, long SpalteId);
