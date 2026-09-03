using KanbanC.BL.Models;
using KanbanC.BL.Operations.Fehler;
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

    // Der Code des Befunds sagt, ob ein Ding fehlte oder eine Regel verletzt wurde; der
    // Statuscode folgt ihm.
    public static IResult AlsFehlerantwort(Pruefbefunde befunde)
    {
        var zurueckweisung = Aus(befunde);
        var einDingFehlt = zurueckweisung.Befunde.Any(Nichtgefunden.MeldetEinFehlendesDing);
        if (einDingFehlt)
        {
            return Results.NotFound(zurueckweisung);
        }

        return Results.BadRequest(zurueckweisung);
    }
}
