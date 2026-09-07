namespace KanbanC.Contracts.Ereignisse;

// Dass sich eine Karte bewegt hat — samt Zielspalte, Urheber, Weg und Zeitpunkt. **Signal statt
// Nutzlast:** die neue Karte steht nicht darin, jede Sicht holt sie über ihren gewohnten Weg. So
// entsteht kein zweiter Weg, auf dem eine Bahn zustande kommt.
// Vom Urheber steht nur die Id da: den Namen löst die Sicht über die Kontributorenliste auf, damit
// ein Umbenennen von selbst nachzieht. Ohne Urheber — ein Agent ohne Identität — steht dort null,
// und die Marke nennt nur Weg und Zeitpunkt.
public record Kartenereignis(long Board, long Karte, long SpalteId, long? Urheber, Ereignisweg Weg, DateTimeOffset Zeitpunkt);
