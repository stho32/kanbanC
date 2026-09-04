using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Boards;

public static class BoardAnlegenValidator
{
    private const string Boardroute = "POST /api/boards";

    public static Pruefbefunde Pruefe(BoardAnlegenAnfrage anfrage)
    {
        var befunde = new List<Fehlerbefund>();

        var nameIstLeer = Boardname.IstLeer(anfrage.Name);
        if (nameIstLeer)
        {
            befunde.Add(Boardname.LeererName(Boardroute));
        }

        var artIstUnbekannt = !Enum.IsDefined(anfrage.Art);
        if (artIstUnbekannt)
        {
            befunde.Add(new Fehlerbefund(
                "board-art-unbekannt",
                "Die Board-Art ist unbekannt; erlaubt sind Linie und Projekt.",
                $"`{Boardroute}` mit „art“ = „Linie“ oder „Projekt“ wiederholen."));
        }

        var zielterminLiegtVorStarttermin = anfrage.Zieltermin < anfrage.Starttermin;
        if (zielterminLiegtVorStarttermin)
        {
            befunde.Add(new Fehlerbefund(
                "zieltermin-vor-starttermin",
                "Der Zieltermin darf nicht vor dem Starttermin liegen.",
                $"`{Boardroute}` mit einem „zieltermin“ ab dem {anfrage.Starttermin:yyyy-MM-dd} wiederholen — oder den „starttermin“ vorziehen."));
        }

        return new Pruefbefunde(befunde);
    }
}
