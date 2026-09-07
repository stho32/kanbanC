using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Interfaces.Import;

public interface IWbsImportRepository
{
    // Die Bahnen des Boards, so viel davon, wie die Zielspaltenwahl braucht. null heißt: dieses
    // Board gibt es nicht.
    Importziel? LiesZiel(long boardId);

    // Der Iststand der Kartenklasse in **einem** Lesevorgang je Lauf und nicht je Karte: 41 Karten
    // kosten dieselben Abfragen wie eine. Archivierte Karten stehen mit darin — sie zu übergehen
    // erzeugte eine zweite Karte für denselben Knoten.
    Karteniststaende LiesIststand(long boardId, long kartenklasseId);

    // Schreibt den ganzen Lauf in **einer** Transaktion: je Anlage eine Karte, ihre
    // Kartenklassenzuordnung mit der nächsten Nummer, ihre Etiketten, ihre Teilaufgaben samt Haken
    // und ihren Dateiverweis; je Aktualisierung Titel, Beschreibung, Etiketten und Teilaufgaben.
    // Bricht ein Schritt ab, steht danach weder eine neue noch eine halb nachgezogene Karte.
    // **Die Kartenklassenzuordnung einer wiedererkannten Karte wird nicht angefasst** — der
    // Zaehlerstand wächst nur je neuer Karte.
    // Zurück kommt die Zahl der angelegten Karten.
    int Schreibe(
        IReadOnlyList<Kartenschreibauftrag> anlagen,
        IReadOnlyList<Kartenaktualisierungsauftrag> aktualisierungen,
        long kartenklasseId,
        long kontributorId);
}
