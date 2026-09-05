namespace KanbanC.Contracts.Karten;

// Ein Etikettentext, den auf diesem Board schon jemand vergeben hat, mit der Zahl der Karten,
// die ihn tragen. Die Zahl macht abweichende Schreibweisen sichtbar — sie verhindert sie nicht.
public record Etikettvorschlag(string Text, int Kartenzahl);
