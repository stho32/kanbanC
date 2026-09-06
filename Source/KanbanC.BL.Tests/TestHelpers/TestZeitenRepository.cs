using KanbanC.BL.Interfaces.Zeiten;
using KanbanC.BL.Models.Zeiten;
using KanbanC.BL.Operations.Zeiten;
using KanbanC.Contracts.Karten;
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

    public Zeiteintrag? TrageNach(long karteId, ZeiteintragNachtragenAnfrage anfrage)
    {
        if (!_dieKarteGibtEs)
        {
            return null;
        }

        Schreibzugriffe++;
        var nachgetragener = new Zeiteintrag(_zeiteintraege.Count + 1, karteId, Zeitmesser with { KontributorId = anfrage.Kontributor }, anfrage.Beginn, anfrage.Ende);
        _zeiteintraege.Add(nachgetragener);
        return nachgetragener;
    }

    public Zeiteintrag? Lies(long karteId, long zeiteintragId)
    {
        return _zeiteintraege.FirstOrDefault(eintrag => eintrag.Karte == karteId && eintrag.ZeiteintragId == zeiteintragId);
    }

    // Hält denselben Vertrag wie das echte Repository: der Rückfall auf „läuft" wird **hier**
    // entschieden und nicht vom Dienst davor, und ein blockierender Eintrag kommt statt des
    // geänderten zurück — ohne Schreibzugriff.
    public Zeiteintragsaenderung? Aendere(long karteId, long zeiteintragId, ZeiteintragAendernAnfrage anfrage)
    {
        var vorhandener = _zeiteintraege.FirstOrDefault(eintrag => eintrag.Karte == karteId && eintrag.ZeiteintragId == zeiteintragId);
        var denEintragGibtEsAnDieserKarteNicht = vorhandener is null;
        if (denEintragGibtEsAnDieserKarteNicht)
        {
            return null;
        }

        var schonLaufender = LaufenderEintragAusser(karteId, anfrage.Kontributor, zeiteintragId);
        var einAndererStehtDemRueckfallImWeg = anfrage.Ende is null && schonLaufender is not null;
        if (einAndererStehtDemRueckfallImWeg)
        {
            return new Zeiteintragsaenderung(schonLaufender!, WurdeGeaendert: false);
        }

        Schreibzugriffe++;
        var kontributor = Zeitmesser with { KontributorId = anfrage.Kontributor };
        var geaenderter = vorhandener! with { Kontributor = kontributor, Beginn = anfrage.Beginn, Ende = anfrage.Ende };
        _zeiteintraege[_zeiteintraege.IndexOf(vorhandener!)] = geaenderter;
        return new Zeiteintragsaenderung(geaenderter, WurdeGeaendert: true);
    }

    private Zeiteintrag? LaufenderEintragAusser(long karteId, long kontributorId, long zeiteintragId)
    {
        return _zeiteintraege.FirstOrDefault(eintrag =>
            eintrag.Karte == karteId
            && eintrag.Kontributor.KontributorId == kontributorId
            && eintrag.Ende is null
            && eintrag.ZeiteintragId != zeiteintragId);
    }

    // Das Kartendetail entsteht hier als leere Hülle mit den verbliebenen Einträgen: der Dienst
    // reicht es unverändert durch, und mehr braucht kein Test dieser Ebene.
    public Kartendetail? Loesche(long karteId, long zeiteintragId)
    {
        var vorhandener = _zeiteintraege.FirstOrDefault(eintrag => eintrag.Karte == karteId && eintrag.ZeiteintragId == zeiteintragId);
        var denEintragGibtEsAnDieserKarteNicht = vorhandener is null;
        if (denEintragGibtEsAnDieserKarteNicht)
        {
            return null;
        }

        Schreibzugriffe++;
        _zeiteintraege.Remove(vorhandener!);
        return Restkartendetail(karteId);
    }

    private Kartendetail Restkartendetail(long karteId)
    {
        var verbliebene = _zeiteintraege.Where(eintrag => eintrag.Karte == karteId).ToList();
        var karte = new Karte(karteId, "Migration schreiben", 1, null, null, null, Kartenfarbe.Ohne, Kontributor: null, Kartennummer: null);
        return new Kartendetail(karte, 1, "Entwicklung", 1, "Backlog", null, [], [], [], [], [], [], null, verbliebene);
    }

    // Ein Eintrag, den es gibt — nur an einer anderen Karte. Für den Stopp ist er wie ein
    // unbekannter.
    public TestZeitenRepository MitLaufendemEintrag(long karteId, long kontributorId, DateTimeOffset beginn)
    {
        _zeiteintraege.Add(new Zeiteintrag(_zeiteintraege.Count + 1, karteId, Zeitmesser with { KontributorId = kontributorId }, beginn, Ende: null));
        return this;
    }

    // Ein abgeschlossener Eintrag im Bestand — für die Änderung braucht es einen, der schon
    // dasteht.
    public TestZeitenRepository MitAbgeschlossenemEintrag(long karteId, long kontributorId, DateTimeOffset beginn, DateTimeOffset ende)
    {
        _zeiteintraege.Add(new Zeiteintrag(_zeiteintraege.Count + 1, karteId, Zeitmesser with { KontributorId = kontributorId }, beginn, ende));
        return this;
    }
}
