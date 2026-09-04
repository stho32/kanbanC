namespace KanbanC.Contracts.Boards;

public record Board(
    long BoardId,
    string Name,
    BoardArt Art,
    DateOnly? Starttermin,
    DateOnly? Zieltermin,
    IReadOnlyList<Spalte> Spalten,
    bool ZeigtKartenzahl,
    bool IstArchiviert);
