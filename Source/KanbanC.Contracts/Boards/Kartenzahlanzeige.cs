namespace KanbanC.Contracts.Boards;

// Der gewünschte Zustand als Rumpf des Umschalt-Aufrufs. Ein eigener Typ statt eines nackten
// bool, damit im JSON ein Feldname steht und ein Agent sieht, was er setzt.
public record Kartenzahlanzeige(bool ZeigtKartenzahl);
