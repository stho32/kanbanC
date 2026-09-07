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

    public IReadOnlyList<Kartenschreibauftrag> GeschriebeneAuftraege { get; private set; } = [];

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

    public int Schreibe(IReadOnlyList<Kartenschreibauftrag> auftraege, long kartenklasseId, long kontributorId)
    {
        WurdeGeschrieben = true;
        GeschriebeneAuftraege = auftraege;
        GeschriebeneKartenklasse = kartenklasseId;
        GeschriebenerKontributor = kontributorId;
        return auftraege.Count;
    }
}
