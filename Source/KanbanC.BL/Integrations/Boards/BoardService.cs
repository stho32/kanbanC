using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Operations.Boards;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Integrations.Boards;

public sealed class BoardService
{
    private readonly IBoardRepository _repository;

    public BoardService(IBoardRepository repository)
    {
        _repository = repository;
    }

    public Board LegeBoardAn(BoardAnlegenAnfrage anfrage)
    {
        var standardspalten = StandardspaltenVorlage.FuerNeuesBoard();
        return _repository.LegeAn(anfrage, standardspalten);
    }

    public IReadOnlyList<BoardUebersicht> LadeAlleBoards()
    {
        return _repository.LadeAlle();
    }

    public Board? LadeBoard(long boardId)
    {
        return _repository.Lade(boardId);
    }
}
