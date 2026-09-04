using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Models;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Operations.Fehler;
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

    // Ein Ergebnis statt null, weil zwei Lagen zu unterscheiden sind: ein leerer Name ist eine
    // verletzte Regel, ein unbekanntes Board ein fehlendes Ding. Die Anfrage wird vor dem
    // Nachschlagen geprüft — wie beim Anlegen erfährt ein Agent zuerst, dass sein Rumpf nicht taugt.
    public Ergebnis<Board> BenenneBoardUm(long boardId, BoardUmbenennenAnfrage anfrage)
    {
        var befunde = BoardUmbenennenValidator.Pruefe(anfrage);
        var anfrageIstUngueltig = !befunde.IstOhneBefund;
        if (anfrageIstUngueltig)
        {
            return Ergebnis<Board>.Zurueckgewiesen(befunde);
        }

        var board = _repository.BenenneUm(boardId, anfrage);
        if (board is null)
        {
            return Ergebnis<Board>.Zurueckgewiesen(new Pruefbefunde([Nichtgefunden.Board(boardId)]));
        }

        return Ergebnis<Board>.Erfolg(board);
    }

    // null heißt: dieses Board gibt es nicht.
    public Board? SchalteKartenzahl(long boardId, Kartenzahlanzeige anzeige)
    {
        return _repository.SetzeKartenzahlanzeige(boardId, anzeige);
    }
}
