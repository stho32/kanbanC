namespace KanbanC.Blazor.Services;

// Ob eine Meldung der WebApi die offene Sicht überhaupt angeht. Die Regel steht hier und nicht
// zweimal in Board.razor: Kartenereignis und Importereignis stellen dieselbe Frage, und über den
// Browser ist ihre Antwort nicht zu zeigen — eine Sicht, die auf ein fremdes Board hin nachlädt,
// zeigt danach genau dasselbe Bild wie vorher.
public static class Boardereignisse
{
    public static bool GehoertZurSicht(long boardDesEreignisses, long boardDerSicht)
    {
        return boardDesEreignisses == boardDerSicht;
    }
}
