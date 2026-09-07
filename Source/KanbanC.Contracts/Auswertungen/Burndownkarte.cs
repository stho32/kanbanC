namespace KanbanC.Contracts.Auswertungen;

// Eine Karte, die an einem Kalendertag der Reihe erledigt wurde — mit Nummer und Titel, damit die
// Tageszeile lesbar ist, ohne dass jemand die Karte nachschlägt.
public record Burndownkarte(long KarteId, string? Kartennummer, string Titel);
