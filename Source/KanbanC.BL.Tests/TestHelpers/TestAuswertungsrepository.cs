using KanbanC.BL.Interfaces.Auswertungen;
using KanbanC.BL.Models.Auswertungen;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestAuswertungsrepository : IAuswertungsrepository
{
    private readonly Dictionary<(long BoardId, long KartenklasseId), SollIstKarten> _bestaende = [];

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
}
