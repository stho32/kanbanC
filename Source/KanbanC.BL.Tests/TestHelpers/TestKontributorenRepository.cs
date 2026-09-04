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
        var kontributor = new Kontributor(_kontributoren.Count + 1, anfrage.Name, anfrage.Art, StillgelegtAm: null);
        _kontributoren.Add(kontributor);
        return kontributor;
    }

    public long? GeaenderteKontributorId { get; private set; }

    public KontributorAendernAnfrage? ErhalteneAenderung { get; private set; }

    // null heißt hier dasselbe wie im echten Repository: diese KontributorId gibt es nicht.
    public Kontributor? Aendere(long kontributorId, KontributorAendernAnfrage anfrage)
    {
        GeaenderteKontributorId = kontributorId;
        ErhalteneAenderung = anfrage;
        var stelle = _kontributoren.FindIndex(kontributor => kontributor.KontributorId == kontributorId);
        var denKontributorGibtEsNicht = stelle < 0;
        if (denKontributorGibtEsNicht)
        {
            return null;
        }

        var geaenderter = new Kontributor(kontributorId, anfrage.Name, anfrage.Art, StillgelegtAm: null);
        _kontributoren[stelle] = geaenderter;
        return geaenderter;
    }

    public Stilllegung? ErhalteneStilllegung { get; private set; }

    // Das Test-Repository hält denselben Vertrag wie das echte: null heißt „diese KontributorId
    // gibt es nicht“, das Datum wird beim Stilllegen gesetzt und beim Zurückholen gelöscht, und
    // ein zweites Stilllegen lässt es stehen.
    public Kontributor? SetzeStilllegung(long kontributorId, Stilllegung stilllegung)
    {
        ErhalteneStilllegung = stilllegung;
        var stelle = _kontributoren.FindIndex(kontributor => kontributor.KontributorId == kontributorId);
        var denKontributorGibtEsNicht = stelle < 0;
        if (denKontributorGibtEsNicht)
        {
            return null;
        }

        var geschaltete = _kontributoren[stelle] with { StillgelegtAm = NeuerStand(_kontributoren[stelle], stilllegung) };
        _kontributoren[stelle] = geschaltete;
        return geschaltete;
    }

    private static DateOnly? NeuerStand(Kontributor kontributor, Stilllegung stilllegung)
    {
        if (!stilllegung.IstStillgelegt)
        {
            return null;
        }

        return kontributor.StillgelegtAm ?? DateOnly.FromDateTime(DateTime.Today);
    }

    // Die Reihenfolge der Liste ist die des Repositories: das Test-Repository sortiert bewusst
    // nicht, damit auffiele, wenn der Service ein zweites Mal sortierte.
    public IReadOnlyList<Kontributor> LadeAlle()
    {
        return _kontributoren;
    }
}
