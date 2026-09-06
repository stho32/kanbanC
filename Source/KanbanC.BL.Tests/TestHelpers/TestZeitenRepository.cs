using KanbanC.BL.Interfaces.Zeiten;
using KanbanC.BL.Models.Zeiten;
using KanbanC.BL.Operations.Zeiten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Tests.TestHelpers;

// Hält denselben Vertrag wie das echte Repository: beim Start heißt null „diese KarteId gibt es
// nicht", beim Stopp „diesen Zeiteintrag gibt es an dieser Karte nicht"; der zweite Start
// desselben Paares gibt denselben Eintrag mit IstNeu = false zurück und schreibt nichts, und der
// zweite Stopp lässt das gesetzte Ende stehen, ohne zu schreiben.
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

    public DateTimeOffset? ErhalteneUhrzeitBeimStopp { get; private set; }

    public Zeiteintrag? BeendeZeitmessung(long karteId, long zeiteintragId, DateTimeOffset uhrzeit)
    {
        ErhalteneUhrzeitBeimStopp = uhrzeit;
        var vorhandener = _zeiteintraege.FirstOrDefault(eintrag => eintrag.Karte == karteId && eintrag.ZeiteintragId == zeiteintragId);
        var denEintragGibtEsAnDieserKarteNicht = vorhandener is null;
        if (denEintragGibtEsAnDieserKarteNicht)
        {
            return null;
        }

        var derEintragIstSchonBeendet = vorhandener!.Ende is not null;
        if (derEintragIstSchonBeendet)
        {
            return vorhandener;
        }

        Schreibzugriffe++;
        var beendeter = vorhandener with { Ende = Zeitmessungsende.Fuer(vorhandener.Beginn, uhrzeit) };
        _zeiteintraege[_zeiteintraege.IndexOf(vorhandener)] = beendeter;
        return beendeter;
    }

    // Ein Eintrag, den es gibt — nur an einer anderen Karte. Für den Stopp ist er wie ein
    // unbekannter.
    public TestZeitenRepository MitLaufendemEintrag(long karteId, long kontributorId, DateTimeOffset beginn)
    {
        _zeiteintraege.Add(new Zeiteintrag(_zeiteintraege.Count + 1, karteId, Zeitmesser with { KontributorId = kontributorId }, beginn, Ende: null));
        return this;
    }
}
