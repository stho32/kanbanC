using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Interfaces.Import;

public interface IWbsImportRepository
{
    // Die Bahnen des Boards, so viel davon, wie die Zielspaltenwahl braucht. null heißt: dieses
    // Board gibt es nicht.
    Importziel? LiesZiel(long boardId);

    // Schreibt den ganzen Lauf in **einer** Transaktion: je Auftrag eine Karte, ihre
    // Kartenklassenzuordnung mit der nächsten Nummer, ihre Etiketten, ihre Teilaufgaben samt Haken
    // und ihren Dateiverweis. Bricht ein Schritt ab, steht danach keine Karte des Laufs.
    // Zurück kommt die Zahl der angelegten Karten.
    int Schreibe(IReadOnlyList<Kartenschreibauftrag> auftraege, long kartenklasseId, long kontributorId);
}
