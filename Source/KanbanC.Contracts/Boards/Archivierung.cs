namespace KanbanC.Contracts.Boards;

// Der gewünschte Archivstand als Rumpf des Aufrufs und als Filter der Liste. Ein benanntes Feld
// statt eines nackten bool, damit ein Agent im JSON sieht, was er setzt — und damit dieselbe
// Route zurückholt, mit der er archiviert hat.
public record Archivierung(bool IstArchiviert);
