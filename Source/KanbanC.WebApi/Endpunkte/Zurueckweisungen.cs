using KanbanC.BL.Models;
using KanbanC.Contracts.Fehler;

namespace KanbanC.WebApi.Endpunkte;

public static class Zurueckweisungen
{
    public static Zurueckweisung Aus(Pruefbefunde befunde)
    {
        var gesammelte = new List<Fehlerbefund>();
        foreach (var befund in befunde)
        {
            gesammelte.Add(befund);
        }

        return new Zurueckweisung(gesammelte);
    }

    public static IResult AlsNichtgefunden(Fehlerbefund befund)
    {
        return Results.NotFound(new Zurueckweisung([befund]));
    }
}
