using KanbanC.BL.Operations.Fehler;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Operations.Import;

// Gehört die genannte Kartenklasse diesem Board? **Der Import legt nie eine an** — das ist die
// Klassenpflege im Layout-Modus, und ein zweiter Weg dorthin müsste Name und Präfix raten.
// null heißt „mit dieser Kartenklasse ist alles in Ordnung“.
public static class Kartenklassenpruefung
{
    public static Fehlerbefund? Pruefe(long boardId, string boardname, long kartenklasseId, IReadOnlyList<Kartenklasse> kartenklassenDesBoards, long? boardDerKartenklasse)
    {
        var dasBoardFuehrtKeineKartenklasse = kartenklassenDesBoards.Count == 0;
        if (dasBoardFuehrtKeineKartenklasse)
        {
            return Importbefunde.OhneKartenklasse(boardId, boardname);
        }

        var dieKlasseGehoertZuDiesemBoard = kartenklassenDesBoards.Any(kartenklasse => kartenklasse.KartenklasseId == kartenklasseId);
        if (dieKlasseGehoertZuDiesemBoard)
        {
            return null;
        }

        // Zwei Lagen, zwei Codes — wie beim Laden der Karten einer Klasse: „gibt es nicht“ schickt
        // den Aufrufer an die Liste des Boards, „gehört einem anderen Board“ sagt ihm, dass es sie
        // gibt, nur nicht hier.
        var dieKartenklasseGibtEsNirgends = boardDerKartenklasse is null;
        if (dieKartenklasseGibtEsNirgends)
        {
            return Nichtgefunden.Kartenklasse(boardId, kartenklasseId);
        }

        return Nichtgefunden.FremdeKartenklasse(boardId, kartenklasseId, boardDerKartenklasse!.Value);
    }
}
