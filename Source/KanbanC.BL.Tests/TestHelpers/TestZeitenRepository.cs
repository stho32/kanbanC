using KanbanC.BL.Interfaces.Zeiten;
using KanbanC.BL.Models.Zeiten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Tests.TestHelpers;

// Hält denselben Vertrag wie das echte Repository: null heißt „diese KarteId gibt es nicht",
// der zweite Start desselben Paares gibt denselben Eintrag mit IstNeu = false zurück und
// schreibt nichts, und Ende bleibt in jedem Fall null.
public sealed class TestZeitenRepository : IZeitenRepository
{
    private static readonly Kontributor Zeitmesser = new(3, "Stefan", Kontributorart.Mensch, StillgelegtAm: null);
    private readonly List<Zeiteintrag> _zeiteintraege = [];
    private readonly bool _dieKarteGibtEs;

    private TestZeitenRepository(bool dieKarteGibtEs)
    {
        _dieKarteGibtEs = dieKarteGibtEs;
    }

    public static TestZeitenRepository Leer()
    {
        return new TestZeitenRepository(dieKarteGibtEs: true);
    }

    public static TestZeitenRepository OhneDieseKarte()
    {
        return new TestZeitenRepository(dieKarteGibtEs: false);
    }

    public int Schreibzugriffe { get; private set; }

    public IReadOnlyList<Zeiteintrag> Zeiteintraege => _zeiteintraege;

    public DateTimeOffset? ErhaltenerBeginn { get; private set; }

    public Zeitmessungsstart? StarteZeitmessung(long karteId, long kontributorId, DateTimeOffset beginn)
    {
        ErhaltenerBeginn = beginn;
        if (!_dieKarteGibtEs)
        {
            return null;
        }

        var laufender = _zeiteintraege.FirstOrDefault(eintrag => eintrag.Karte == karteId && eintrag.Kontributor.KontributorId == kontributorId && eintrag.Ende is null);
        var fuerDiesesPaarLaeuftSchonEiner = laufender is not null;
        if (fuerDiesesPaarLaeuftSchonEiner)
        {
            return new Zeitmessungsstart(laufender!, IstNeu: false);
        }

        Schreibzugriffe++;
        var angelegter = new Zeiteintrag(_zeiteintraege.Count + 1, karteId, Zeitmesser with { KontributorId = kontributorId }, beginn, Ende: null);
        _zeiteintraege.Add(angelegter);
        return new Zeitmessungsstart(angelegter, IstNeu: true);
    }
}
