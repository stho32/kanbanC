using KanbanC.BL.Interfaces.Kontributoren;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestKontributorenRepository : IKontributorenRepository
{
    private readonly List<Kontributor> _kontributoren = [];

    public KontributorAnlegenAnfrage? ErhalteneAnfrage { get; private set; }

    public Kontributor LegeAn(KontributorAnlegenAnfrage anfrage)
    {
        ErhalteneAnfrage = anfrage;
        var kontributor = new Kontributor(_kontributoren.Count + 1, anfrage.Name, anfrage.Art);
        _kontributoren.Add(kontributor);
        return kontributor;
    }

    // Die Reihenfolge der Liste ist die des Repositories: das Test-Repository sortiert bewusst
    // nicht, damit auffiele, wenn der Service ein zweites Mal sortierte.
    public IReadOnlyList<Kontributor> LadeAlle()
    {
        return _kontributoren;
    }
}
