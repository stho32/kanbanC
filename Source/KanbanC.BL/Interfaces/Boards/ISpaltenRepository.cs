using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Interfaces.Boards;

public interface ISpaltenRepository
{
    Ergebnis<Spalte>? LegeAn(long boardId, SpalteAnlegenAnfrage anfrage);

    Ergebnis<Spalte>? Aendere(long boardId, long spalteId, SpalteAendernAnfrage anfrage);

    IReadOnlyList<Spalte>? LadeAlle(long boardId);

    Ergebnis<IReadOnlyList<Spalte>>? SetzeReihenfolge(long boardId, IReadOnlyList<long> reihenfolge);

    Ergebnis<Spalte>? Entferne(long boardId, long spalteId);
}
