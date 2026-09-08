using KanbanC.BL.Interfaces.Auswertungen;
using KanbanC.BL.Models.Auswertungen;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestAuswertungsrepository : IAuswertungsrepository
{
    private const string Unbekanntesboard = "Unbekannt";
    private readonly Dictionary<(long BoardId, long KartenklasseId), SollIstKarten> _bestaende = []; // stil-check: C11 Testablage je Bestand, kein Domaenenbestand
    private readonly Dictionary<(long BoardId, long KartenklasseId), Erledigungsstandkarten> _erledigungsstaende = []; // stil-check: C11 Testablage je Bestand, kein Domaenenbestand
    private readonly Dictionary<(long BoardId, long KartenklasseId), Zeitexportzeilen> _zeiteintraege = []; // stil-check: C11 Testablage je Bestand, kein Domaenenbestand
    private readonly Dictionary<(long BoardId, long KartenklasseId), Pufferstandkarten> _pufferstaende = []; // stil-check: C11 Testablage je Bestand, kein Domaenenbestand

    public bool WurdeGelesen { get; private set; }

    public int Lesevorgaenge { get; private set; }

    public TestAuswertungsrepository MitBestand(long boardId, long kartenklasseId, params SollIstKarte[] karten)
    {
        _bestaende[(boardId, kartenklasseId)] = new SollIstKarten(karten);
        return this;
    }

    public SollIstKarten LiesSollIst(long boardId, long kartenklasseId)
    {
        WurdeGelesen = true;
        Lesevorgaenge = Lesevorgaenge + 1;
        if (_bestaende.TryGetValue((boardId, kartenklasseId), out var bestand))
        {
            return bestand;
        }

        return new SollIstKarten([]);
    }

    public TestAuswertungsrepository MitErledigungsstaenden(long boardId, long kartenklasseId, params Erledigungsstandkarte[] karten)
    {
        _erledigungsstaende[(boardId, kartenklasseId)] = new Erledigungsstandkarten(karten);
        return this;
    }

    public Erledigungsstandkarten LiesErledigungsstaende(long boardId, long kartenklasseId)
    {
        WurdeGelesen = true;
        Lesevorgaenge = Lesevorgaenge + 1;
        if (_erledigungsstaende.TryGetValue((boardId, kartenklasseId), out var bestand))
        {
            return bestand;
        }

        return new Erledigungsstandkarten([]);
    }

    public TestAuswertungsrepository MitZeiteintraegen(long boardId, long kartenklasseId, string boardname, params Zeitexportzeile[] zeilen)
    {
        _zeiteintraege[(boardId, kartenklasseId)] = new Zeitexportzeilen(boardname, zeilen);
        return this;
    }

    public Zeitexportzeilen LiesZeiteintraege(long boardId, long kartenklasseId)
    {
        WurdeGelesen = true;
        Lesevorgaenge = Lesevorgaenge + 1;
        if (_zeiteintraege.TryGetValue((boardId, kartenklasseId), out var bestand))
        {
            return bestand;
        }

        return new Zeitexportzeilen(Unbekanntesboard, []);
    }

    public TestAuswertungsrepository MitPufferstaenden(long boardId, long kartenklasseId, params Pufferstandkarte[] karten)
    {
        _pufferstaende[(boardId, kartenklasseId)] = new Pufferstandkarten(karten);
        return this;
    }

    public Pufferstandkarten LiesPufferstaende(long boardId, long kartenklasseId)
    {
        WurdeGelesen = true;
        Lesevorgaenge = Lesevorgaenge + 1;
        if (_pufferstaende.TryGetValue((boardId, kartenklasseId), out var bestand))
        {
            return bestand;
        }

        return new Pufferstandkarten([]);
    }
}
