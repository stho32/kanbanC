using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Models.Boards;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestBoardRepository : IBoardRepository
{
    private readonly List<Board> _boards = [];

    public BoardAnlegenAnfrage? ErhalteneAnfrage { get; private set; }

    public Spaltenvorlagen? ErhalteneSpalten { get; private set; }

    public long? ErfragteBoardId { get; private set; }

    public Board Speichere(Board board)
    {
        _boards.Add(board);
        return board;
    }

    public Board LegeAn(BoardAnlegenAnfrage anfrage, Spaltenvorlagen standardspalten)
    {
        ErhalteneAnfrage = anfrage;
        ErhalteneSpalten = standardspalten;
        var board = new Board(_boards.Count + 1, anfrage.Name, anfrage.Art, anfrage.Starttermin, anfrage.Zieltermin, [], false, false);
        return Speichere(board);
    }

    public IReadOnlyList<BoardUebersicht> LadeAlle(Archivierung archivstand)
    {
        var gewaehlte = _boards.Where(b => b.IstArchiviert == archivstand.IstArchiviert);
        return gewaehlte.Select(b => new BoardUebersicht(b.BoardId, b.Name, b.Art, b.Starttermin, b.Zieltermin)).ToList();
    }

    public Board? Lade(long boardId)
    {
        ErfragteBoardId = boardId;
        return _boards.SingleOrDefault(b => b.BoardId == boardId);
    }

    public BoardUmbenennenAnfrage? GeschriebeneUmbenennung { get; private set; }

    public Board? BenenneUm(long boardId, BoardUmbenennenAnfrage anfrage)
    {
        var stelle = _boards.FindIndex(b => b.BoardId == boardId);
        var boardIstUnbekannt = stelle < 0;
        if (boardIstUnbekannt)
        {
            return null;
        }

        GeschriebeneUmbenennung = anfrage;
        var umbenannt = _boards[stelle] with { Name = anfrage.Name };
        _boards[stelle] = umbenannt;
        return umbenannt;
    }

    public Kartenzahlanzeige? GeschriebeneAnzeige { get; private set; }

    public Board? SetzeKartenzahlanzeige(long boardId, Kartenzahlanzeige anzeige)
    {
        var stelle = _boards.FindIndex(b => b.BoardId == boardId);
        var boardIstUnbekannt = stelle < 0;
        if (boardIstUnbekannt)
        {
            return null;
        }

        GeschriebeneAnzeige = anzeige;
        var geschaltet = _boards[stelle] with { ZeigtKartenzahl = anzeige.ZeigtKartenzahl };
        _boards[stelle] = geschaltet;
        return geschaltet;
    }
}
