namespace KanbanC.Contracts.Boards;

public record BoardUebersicht(long BoardId, string Name, BoardArt Art, DateOnly? Starttermin, DateOnly? Zieltermin);
