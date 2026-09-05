namespace KanbanC.Contracts.Karten;

// Die Felder des Eigenschaftenblatts in einer Anfrage, weil sie in einem Blatt geändert werden.
// Beschreibung und Fälligkeit sind optional; null heißt dort „nicht gesetzt", nicht „unverändert".
// Der Verantwortliche reist als Fremdschlüssel und heißt nach der Tabelle, auf die er zeigt;
// null bedeutet „niemand" und ist ein gültiger Wert, kein Fehler.
public record KarteAendernAnfrage(string Titel, string? Beschreibung, DateOnly? FaelligAm, Kartenfarbe Farbe, long? Kontributor);
