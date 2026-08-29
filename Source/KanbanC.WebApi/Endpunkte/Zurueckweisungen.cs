using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;

namespace KanbanC.WebApi.Endpunkte;

public static class Zurueckweisungen
{
    public static Zurueckweisung Aus(Pruefbefunde befunde)
    {
        var meldungen = new List<string>();
        foreach (var meldung in befunde)
        {
            meldungen.Add(meldung);
        }

        return new Zurueckweisung(meldungen);
    }
}
