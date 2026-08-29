using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Interfaces.Boards;

public interface ISpaltenRepository
{
    Spalte? LegeAn(long boardId, SpalteAnlegenAnfrage anfrage);

    Spalte? Aendere(long boardId, long spalteId, SpalteAendernAnfrage anfrage);

    IReadOnlyList<Spalte>? LadeAlle(long boardId);

    Ergebnis<IReadOnlyList<Spalte>>? SetzeReihenfolge(long boardId, IReadOnlyList<long> reihenfolge);

    bool Entferne(long boardId, long spalteId);
}
