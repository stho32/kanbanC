namespace KanbanC.Contracts.Karten;

// ErledigtAm ist null, solange die Karte in keiner Abschlussspalte liegt — und bleibt es für
// Karten, die schon vor der Einführung des Feldes dort lagen: ein nachgetragenes Datum wäre von
// einem echten nicht zu unterscheiden.
public record Karte(long KarteId, string Titel, int Position, DateOnly? ErledigtAm);
