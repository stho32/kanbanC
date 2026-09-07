using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// **Nie stillschweigend.** Die Datei gewinnt auch beim Haken — wer am Board abhakt, findet ihn
// nach dem nächsten Lauf zurückgenommen. Deshalb steht jede einzelne Rücknahme als Grund an der
// Zeile ihrer Karte, mit dem Weg, der sie aufhebt: den Knoten in der Datei auf gruen setzen.
// Ein Grund je Rücknahme und **keine Sammelmeldung**: drei zurückgenommene Haken ergeben drei
// Sätze, jeder mit seinem Knoten.
public static class Hakenruecknahme
{
    public static string Fuer(string knotenId, Wbsstatus status)
    {
        return $"1 Abhakung zurückgenommen (`{knotenId}`) — die Datei führt den Knoten auf `{Wbswoerter.WortFuer(status)}`. "
            + "Wenn der Haken bleiben soll: setze den Knoten in der Datei auf `gruen`.";
    }
}
