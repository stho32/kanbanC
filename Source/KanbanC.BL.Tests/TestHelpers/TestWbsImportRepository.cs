using KanbanC.BL.Interfaces.Import;
using KanbanC.BL.Models.Import;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.TestHelpers;

public sealed class TestWbsImportRepository : IWbsImportRepository
{
    public Importziel? Ziel { get; set; } = new(
        4,
        "KanbanC — Umsetzung",
        [new Importspalte(10, "Bereit", 1, false), new Importspalte(11, "In Arbeit", 2, false), new Importspalte(12, "Erledigt", 3, true)],
        [new Kartenklasse(3, "WBS", "WBS-", 0)]);

    public Karteniststaende Iststand { get; set; } = new([]);

    public IReadOnlyList<Kartenschreibauftrag> GeschriebeneAnlagen { get; private set; } = [];

    public IReadOnlyList<Kartenaktualisierungsauftrag> GeschriebeneAktualisierungen { get; private set; } = [];

    public bool WurdeGeschrieben { get; private set; }

    public long GeschriebeneKartenklasse { get; private set; }

    public long GeschriebenerKontributor { get; private set; }

    public Importziel? LiesZiel(long boardId)
    {
        if (Ziel is null || Ziel.BoardId != boardId)
        {
            return null;
        }

        return Ziel;
    }

    public Karteniststaende LiesIststand(long boardId, long kartenklasseId)
    {
        return Iststand;
    }

    public int Schreibe(
        IReadOnlyList<Kartenschreibauftrag> anlagen,
        IReadOnlyList<Kartenaktualisierungsauftrag> aktualisierungen,
        long kartenklasseId,
        long kontributorId)
    {
        WurdeGeschrieben = true;
        GeschriebeneAnlagen = anlagen;
        GeschriebeneAktualisierungen = aktualisierungen;
        GeschriebeneKartenklasse = kartenklasseId;
        GeschriebenerKontributor = kontributorId;
        return anlagen.Count;
    }
}
