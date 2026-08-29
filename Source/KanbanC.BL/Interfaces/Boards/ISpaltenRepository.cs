using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Interfaces.Boards;

public interface ISpaltenRepository
{
    Spalte? LegeAn(long boardId, SpalteAnlegenAnfrage anfrage);

    Spalte? Aendere(long boardId, long spalteId, SpalteAendernAnfrage anfrage);
}
