using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Interfaces.Karten;

public interface IKartenRepository
{
    Karte? LegeAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage);

    Ergebnis<IReadOnlyList<Spalte>>? Verschiebe(long boardId, long karteId, Kartenlage lage);

    // null heisst „diese Karte gibt es an dieser Stelle nicht"; sonst kommen die Spalten des
    // Boards zurück, weil die betroffene Spalte neu durchnummeriert wurde.
    IReadOnlyList<Spalte>? SetzeArchivierung(long boardId, long karteId, Archivierung archivierung);

    long? BoardDerKarte(long karteId);

    // null heisst: diese KarteId gibt es nicht. Ohne Board in der Signatur, weil die
    // Kartenadresse keins traegt — das Board steht erst in der Antwort.
    Kartendetail? LiesKartendetail(long karteId);

    // null heisst: diese KarteId gibt es nicht. Zurueck kommt das ganze Kartendetail, damit die
    // Seite eine Quelle behaelt und nach dem Schreiben nicht nachladen muss.
    Kartendetail? Aendere(long karteId, KarteAendernAnfrage anfrage);

    // null heisst „diese Spalte gibt es an dieser Stelle nicht"; eine Spalte ohne Karten liefert
    // die leere Liste.
    IReadOnlyList<Karte>? LadeKartenDerSpalte(long boardId, long spalteId, Archivierung archivstand);
}
