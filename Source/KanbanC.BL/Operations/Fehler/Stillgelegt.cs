using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Fehler;

// Eine andere Lage als „gibt es nicht": den Kontributor gibt es, er arbeitet nur nicht mehr mit.
// Deshalb ein eigener Code und eine eigene Stelle — und deshalb 400 statt 404: es fehlt kein
// Ding, es wurde eine Regel verletzt. Nichtgefunden.MeldetEinFehlendesDing kennt diesen Code
// bewusst nicht.
public static class Stillgelegt
{
    public static Fehlerbefund Kontributor(long kontributorId)
    {
        return new Fehlerbefund(
            "kontributor-stillgelegt",
            $"Der Kontributor mit der Nummer {kontributorId} ist stillgelegt und kann nicht verantwortlich sein.",
            "`GET /api/kontributoren` abrufen und den Aufruf mit einer KontributorId ohne „stillgelegtAm“ wiederholen — oder ihn über `PUT /api/kontributoren/{kontributorId}/stilllegung` mit „istStillgelegt“ = false zurückholen.");
    }
}
