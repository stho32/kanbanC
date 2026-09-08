using KanbanC.BL.Interfaces.Rohdaten;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Tests.TestHelpers;

// null heißt „dieses Board gibt es nicht", die leere Liste heißt „dieses Board hat nichts" — die
// beiden Lagen sind der ganze Unterschied, den der Dienst zu treffen hat.
public sealed class TestRohdatenRepository : IRohdatenRepository
{
    private readonly HashSet<long> _bekannteBoards = []; // stil-check: C11 Testablage der Boardnummern, kein Domaenenbestand
    private readonly Dictionary<long, IReadOnlyList<Rohdatenkarte>> _kartenJeBoard = []; // stil-check: C11 Testablage je Board, kein Domaenenbestand
    private readonly Dictionary<long, IReadOnlyList<Zeiteintrag>> _zeiteintraegeJeBoard = []; // stil-check: C11 Testablage je Board, kein Domaenenbestand

    public int Lesevorgaenge { get; private set; }

    public TestRohdatenRepository MitLeeremBoard(long boardId)
    {
        _bekannteBoards.Add(boardId);
        return this;
    }

    public TestRohdatenRepository MitKarten(long boardId, params Rohdatenkarte[] karten)
    {
        _bekannteBoards.Add(boardId);
        _kartenJeBoard[boardId] = karten;
        return this;
    }

    public TestRohdatenRepository MitZeiteintraegen(long boardId, params Zeiteintrag[] zeiteintraege)
    {
        _bekannteBoards.Add(boardId);
        _zeiteintraegeJeBoard[boardId] = zeiteintraege;
        return this;
    }

    public IReadOnlyList<Rohdatenkarte>? LiesKartenDesBoards(long boardId)
    {
        var dasBoardGibtEsNicht = !_bekannteBoards.Contains(boardId);
        if (dasBoardGibtEsNicht)
        {
            return null; // stil-check: C25 null heisst „dieses Board gibt es nicht", der Vertrag von IRohdatenRepository
        }

        Lesevorgaenge = Lesevorgaenge + 1;
        if (_kartenJeBoard.TryGetValue(boardId, out var karten))
        {
            return karten;
        }

        return [];
    }

    public IReadOnlyList<Zeiteintrag>? LiesZeiteintraegeDesBoards(long boardId)
    {
        var dasBoardGibtEsNicht = !_bekannteBoards.Contains(boardId);
        if (dasBoardGibtEsNicht)
        {
            return null; // stil-check: C25 null heisst „dieses Board gibt es nicht", der Vertrag von IRohdatenRepository
        }

        Lesevorgaenge = Lesevorgaenge + 1;
        if (_zeiteintraegeJeBoard.TryGetValue(boardId, out var zeiteintraege))
        {
            return zeiteintraege;
        }

        return [];
    }
}
