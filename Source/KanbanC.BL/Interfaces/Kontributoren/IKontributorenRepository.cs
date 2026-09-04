using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Interfaces.Kontributoren;

public interface IKontributorenRepository
{
    Kontributor LegeAn(KontributorAnlegenAnfrage anfrage);
}
