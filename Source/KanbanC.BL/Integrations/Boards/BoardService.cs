using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Models;
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

    public Ergebnis<Board> LegeBoardAn(BoardAnlegenAnfrage anfrage)
    {
        var befunde = BoardAnlegenValidator.Pruefe(anfrage);
        var anfrageIstUngueltig = !befunde.IstOhneBefund;
        if (anfrageIstUngueltig)
        {
            return Ergebnis<Board>.Zurueckgewiesen(befunde);
        }

        var standardspalten = StandardspaltenVorlage.FuerNeuesBoard();
        var board = _repository.LegeAn(anfrage, standardspalten);
        return Ergebnis<Board>.Erfolg(board);
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
