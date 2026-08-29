using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;

namespace KanbanC.BL.Operations.Boards;

public static class BoardAnlegenValidator
{
    public static Pruefbefunde Pruefe(BoardAnlegenAnfrage anfrage)
    {
        var meldungen = new List<string>();

        var nameIstLeer = string.IsNullOrWhiteSpace(anfrage.Name);
        if (nameIstLeer)
        {
            meldungen.Add("Der Name darf nicht leer sein.");
        }

        var artIstUnbekannt = !Enum.IsDefined(anfrage.Art);
        if (artIstUnbekannt)
        {
            meldungen.Add("Die Board-Art ist unbekannt; erlaubt sind Linie und Projekt.");
        }

        var zielterminLiegtVorStarttermin = anfrage.Zieltermin < anfrage.Starttermin;
        if (zielterminLiegtVorStarttermin)
        {
            meldungen.Add("Der Zieltermin darf nicht vor dem Starttermin liegen.");
        }

        return new Pruefbefunde(meldungen);
    }
}
