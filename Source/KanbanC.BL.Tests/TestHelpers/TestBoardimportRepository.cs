using KanbanC.BL.Interfaces.Boardimport;
using KanbanC.Contracts.Export;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.TestHelpers;

// **Das Repository beweist, dass der trockene Lauf nichts schreibt** — statt dass der Test es
// glaubt: es merkt sich, ob die Schreibmethode gerufen wurde, und mit welcher Datei.
public sealed class TestBoardimportRepository : IBoardimportRepository
{
    private readonly List<Kontributor> _vorhandene = [];
    private long _naechsteBoardId = 3;

    public bool WurdeGeschrieben { get; private set; }

    public Boardexport? GeschriebeneDatei { get; private set; }

    public TestBoardimportRepository MitVorhandenenKontributoren(params Kontributor[] kontributoren)
    {
        _vorhandene.AddRange(kontributoren);
        return this;
    }

    public TestBoardimportRepository MitNaechsterBoardId(long boardId)
    {
        _naechsteBoardId = boardId;
        return this;
    }

    public long SchreibeBoard(Boardexport datei)
    {
        WurdeGeschrieben = true;
        GeschriebeneDatei = datei;
        return _naechsteBoardId;
    }

    public IReadOnlyList<Kontributor> LiesVorhandeneKontributoren()
    {
        return _vorhandene;
    }
}
