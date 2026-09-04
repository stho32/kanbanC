using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Operations.Boards;

public static class BoardUmbenennenValidator
{
    private const string Umbenennenroute = "PUT /api/boards/{boardId}";

    public static Pruefbefunde Pruefe(BoardUmbenennenAnfrage anfrage)
    {
        var nameIstLeer = Boardname.IstLeer(anfrage.Name);
        if (nameIstLeer)
        {
            return new Pruefbefunde([Boardname.LeererName(Umbenennenroute)]);
        }

        return Pruefbefunde.Keine;
    }
}
