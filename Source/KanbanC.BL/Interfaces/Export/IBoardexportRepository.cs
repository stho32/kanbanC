using KanbanC.BL.Models.Export;

namespace KanbanC.BL.Interfaces.Export;

public interface IBoardexportRepository
{
    // Der ganze Bestand eines Boards in **einer** Lesetransaktion: Board, Spalten, Kartenklassen,
    // die referenzierten Kontributoren, alle Karten samt Ort, Archivmarke, Klassenzuordnung mit
    // Zählerstand, Sollband und den fünf Listen, und alle Zeiteinträge.
    // Ohne gemeinsame Transaktion käme eine Karte, die zwischen der ersten und der letzten
    // Abfrage entsteht, mit leeren Listen zurück — die Datei wäre stillschweigend unvollständig.
    // Ein Board ohne Karten liefert leere Listen — das ist eine Antwort und kein Fehler.
    // null heißt: dieses Board gibt es nicht.
    Boardbestand? LiesBoardbestand(long boardId);
}
