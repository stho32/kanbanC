using KanbanC.BL.Models.Boards;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Interfaces.Boards;

public interface IBoardRepository
{
    Board LegeAn(BoardAnlegenAnfrage anfrage, Spaltenvorlagen standardspalten);

    IReadOnlyList<BoardUebersicht> LadeAlle();

    Board? Lade(long boardId);
}
