using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Interfaces.Rohdaten;

public interface IRohdatenRepository
{
    // Alle Karten des Boards mit Ort, Archivmarke, Kartenklasse und ihren fünf Listen — in
    // **sechs** Lesevorgängen je Board und nicht in sechs je Karte. Ungekürzt und ungefiltert:
    // die Anzeigegrenze der Abschlussspalte ist eine Anzeigeregel des Boards, und die archivierte
    // Karte kommt mit ihrer Marke mit statt zu fehlen.
    // Ein Board ohne Karten liefert die leere Liste — das ist eine Antwort und kein Fehler.
    // null heißt: dieses Board gibt es nicht.
    IReadOnlyList<Rohdatenkarte>? LiesKartenDesBoards(long boardId);

    // Alle Zeiteinträge des Boards in Beginn-Folge, laufende mit leerem Ende. Einträge auf
    // archivierten Karten und von stillgelegten Kontributoren bleiben drin; ihre Zeit wurde
    // geleistet. Kein Zeitraumschnitt — dieser Abruf sagt „vollständig" zu.
    // Ein Board ohne Zeiteintrag liefert die leere Liste. null heißt: dieses Board gibt es nicht.
    IReadOnlyList<Zeiteintrag>? LiesZeiteintraegeDesBoards(long boardId);
}
