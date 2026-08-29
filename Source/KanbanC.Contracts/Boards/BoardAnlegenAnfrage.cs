namespace KanbanC.Contracts.Boards;

public record BoardAnlegenAnfrage(string Name, BoardArt Art, DateOnly? Starttermin, DateOnly? Zieltermin);
