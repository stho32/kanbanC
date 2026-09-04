using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Interfaces.Kontributoren;

public interface IKontributorenRepository
{
    Kontributor LegeAn(KontributorAnlegenAnfrage anfrage);

    // null heißt: diese KontributorId gibt es nicht.
    Kontributor? Aendere(long kontributorId, KontributorAendernAnfrage anfrage);

    // null heißt: diese KontributorId gibt es nicht.
    Kontributor? SetzeStilllegung(long kontributorId, Stilllegung stilllegung);

    IReadOnlyList<Kontributor> LadeAlle();
}
