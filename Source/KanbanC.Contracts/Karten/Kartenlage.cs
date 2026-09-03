namespace KanbanC.Contracts.Karten;

// Wohin eine Karte soll: Zielspalte und Zielposition. Die Karte selbst steht in der Route.
public record Kartenlage(long SpalteId, int Position);
