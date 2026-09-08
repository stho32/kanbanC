using KanbanC.Contracts.Boards;

namespace KanbanC.Contracts.Export;

// Das Board, wie die Datei es trägt: Name, Art, Termine, Kartenzahlanzeige und Archivstand.
// **Ohne Spalten- und ohne Zeitenliste**, anders als Board: die laufenden Zeiteinträge stehen in
// der Zeitenliste der Datei, und ein zweites Vorkommen daneben widerspräche ihr.
public record Exportboard(
    long BoardId,
    string Name,
    BoardArt Art,
    DateOnly? Starttermin,
    DateOnly? Zieltermin,
    bool ZeigtKartenzahl,
    bool IstArchiviert);
