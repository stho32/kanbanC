using KanbanC.BL.Interfaces.Export;
using KanbanC.BL.Models.Export;

namespace KanbanC.BL.Tests.TestHelpers;

// null heißt „dieses Board gibt es nicht", ein Bestand mit leeren Listen heißt „dieses Board hat
// nichts" — die beiden Lagen sind der ganze Unterschied, den der Dienst zu treffen hat.
public sealed class TestBoardexportRepository : IBoardexportRepository
{
    private readonly Dictionary<long, Boardbestand> _bestandJeBoard = []; // stil-check: C11 Testablage je Board, kein Domaenenbestand

    public int Lesevorgaenge { get; private set; }

    public TestBoardexportRepository MitBestand(long boardId, Boardbestand bestand)
    {
        _bestandJeBoard[boardId] = bestand;
        return this;
    }

    public Boardbestand? LiesBoardbestand(long boardId)
    {
        var dasBoardGibtEsNicht = !_bestandJeBoard.ContainsKey(boardId);
        if (dasBoardGibtEsNicht)
        {
            return null; // stil-check: C25 null heisst „dieses Board gibt es nicht", der Vertrag von IBoardexportRepository
        }

        Lesevorgaenge = Lesevorgaenge + 1;
        return _bestandJeBoard[boardId];
    }
}
