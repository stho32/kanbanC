namespace KanbanC.Contracts.Karten;

// Die ganze Liste, nicht ein Zugang: die übergebene Liste ist danach exakt die Liste der Karte.
// Eine leere Liste ist gültig und nimmt der Karte alle Etiketten.
public record Kartenetiketten(IReadOnlyList<string> Etiketten);
