namespace KanbanC.Contracts.Boards;

// Der gewünschte Zustand als Rumpf des Umschalt-Aufrufs; der Feldname sagt einem Agenten im
// JSON, was er setzt.
public record Kartenzahlanzeige(bool ZeigtKartenzahl);
