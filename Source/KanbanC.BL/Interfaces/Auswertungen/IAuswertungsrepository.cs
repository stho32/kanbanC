using KanbanC.BL.Models.Auswertungen;

namespace KanbanC.BL.Interfaces.Auswertungen;

public interface IAuswertungsrepository
{
    // Ist und Soll des Kartenbestands — Board und Kartenklasse zusammen — in **einem**
    // Lesevorgang je Bestand und nicht je Karte: 41 Karten kosten dieselben Abfragen wie eine.
    // Archivierte Karten stehen mit darin; ihre Zeit wurde geleistet.
    // Ein Bestand ohne Karten liefert die leere Menge — das ist eine Antwort und kein Fehler.
    SollIstKarten LiesSollIst(long boardId, long kartenklasseId);
}
