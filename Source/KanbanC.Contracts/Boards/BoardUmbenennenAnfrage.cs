namespace KanbanC.Contracts.Boards;

// Nur der Name: Art und Termine gehören dem Anlegen. Was der Rumpf nicht trägt, wird nicht
// geändert — ein Agent, der umbenennt, überschreibt damit nichts, was er gar nicht kennt.
public record BoardUmbenennenAnfrage(string Name);
