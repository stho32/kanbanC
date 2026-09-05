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

    // Setzt die **ganze** Liste: was uebergeben wird, ist danach die Liste der Karte.
    // null heisst: diese KarteId gibt es nicht.
    Kartendetail? SetzeEtiketten(long karteId, Kartenetiketten etiketten);

    // Legt **eine** Teilaufgabe an und haengt sie hinten an; zurueck kommt das ganze Kartendetail,
    // damit die Seite eine Quelle behaelt. null heisst: diese KarteId gibt es nicht.
    Kartendetail? LegeTeilaufgabeAn(long karteId, TeilaufgabeAnlegenAnfrage anfrage);

    // null heisst „diese Spalte gibt es an dieser Stelle nicht"; eine Spalte ohne Karten liefert
    // die leere Liste.
    IReadOnlyList<Karte>? LadeKartenDerSpalte(long boardId, long spalteId, Archivierung archivstand);
}
