using KanbanC.BL.Models;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Interfaces.Klassen;

public interface IKartenklassenRepository
{
    IReadOnlyList<Kartenklasse>? LadeAlle(long boardId);

    Ergebnis<Kartenklasse>? LegeAn(long boardId, KartenklasseAnlegenAnfrage anfrage);
}
