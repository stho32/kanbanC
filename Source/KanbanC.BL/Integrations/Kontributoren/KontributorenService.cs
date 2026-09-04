using KanbanC.BL.Interfaces.Kontributoren;
using KanbanC.BL.Models;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Integrations.Kontributoren;

public sealed class KontributorenService
{
    private readonly IKontributorenRepository _repository;

    public KontributorenService(IKontributorenRepository repository)
    {
        _repository = repository;
    }

    public Ergebnis<Kontributor> LegeKontributorAn(KontributorAnlegenAnfrage anfrage)
    {
        var kontributor = _repository.LegeAn(anfrage);
        return Ergebnis<Kontributor>.Erfolg(kontributor);
    }

    public IReadOnlyList<Kontributor> LadeAlleKontributoren()
    {
        return _repository.LadeAlle();
    }
}
