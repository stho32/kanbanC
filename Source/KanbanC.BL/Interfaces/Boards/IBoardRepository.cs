using KanbanC.BL.Models.Boards;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Interfaces.Boards;

public interface IBoardRepository
{
    Board LegeAn(BoardAnlegenAnfrage anfrage, Spaltenvorlagen standardspalten);

    IReadOnlyList<BoardUebersicht> LadeAlle();

    Board? Lade(long boardId);

    Board? SetzeKartenzahlanzeige(long boardId, Kartenzahlanzeige anzeige);

    // null heißt an beiden Schreibzugriffen: dieses Board gibt es nicht.
    Board? BenenneUm(long boardId, BoardUmbenennenAnfrage anfrage);
}
