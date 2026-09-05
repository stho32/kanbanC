namespace KanbanC.Contracts.Karten;

// Die Felder des Eigenschaftenblatts in einer Anfrage, weil sie in einem Blatt geändert werden.
// Beschreibung und Fälligkeit sind optional; null heißt dort „nicht gesetzt", nicht „unverändert".
public record KarteAendernAnfrage(string Titel, string? Beschreibung, DateOnly? FaelligAm, Kartenfarbe Farbe);
