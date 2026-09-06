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

    // Setzt den Abhakstand **einer** Teilaufgabe und laesst die uebrigen unberuehrt; zurueck kommt
    // das ganze Kartendetail. null heisst: diese TeilaufgabeId gehoert nicht zu dieser Karte.
    Kartendetail? SetzeAbhakung(long karteId, long teilaufgabeId, Teilaufgabenstand stand);

    // Schreibt **einen** Kommentar mit dem Zeitpunkt der Uhr und haengt ihn ans Ende der
    // Zeitordnung; zurueck kommt das ganze Kartendetail. null heisst: diese KarteId gibt es nicht.
    // Der Urheber ist Pflicht und steckt in der Anfrage — einen Kommentar ohne ihn gibt es nicht.
    Kartendetail? SchreibeKommentar(long karteId, KommentarSchreibenAnfrage anfrage);

    // Haengt **eine** Datei an: die Zeile entsteht zuerst, ihre Nummer ist der Name der Datei auf
    // der Platte, die Bytes gehen danach im Strom dorthin. Zurueck kommt das ganze Kartendetail.
    // null heisst: diese KarteId gibt es nicht. Der Urheber ist Pflicht und steckt in der Anfrage.
    Kartendetail? HaengeAnhangAn(long karteId, AnhangAnlegenAnfrage anfrage, Stream inhalt);

    // Liefert Name und Lesestrom **eines** Anhangs; die Bedingung nennt beide Nummern.
    // null heisst: diesen Anhang gibt es an dieser Karte nicht. Fehlt die Datei zu einer
    // vorhandenen Zeile, wirft der Aufruf — ein leerer Download waere die schlechtere Antwort.
    Anhanginhalt? LiesAnhang(long karteId, long anhangId);

    // Entfernt Zeile **und** Datei, in dieser Reihenfolge; zurueck kommt das ganze Kartendetail.
    // null heisst: diesen Anhang gibt es an dieser Karte nicht, und entfernt wurde nichts.
    Kartendetail? EntferneAnhang(long karteId, long anhangId);

    // null heisst „diese Spalte gibt es an dieser Stelle nicht"; eine Spalte ohne Karten liefert
    // die leere Liste.
    IReadOnlyList<Karte>? LadeKartenDerSpalte(long boardId, long spalteId, Archivierung archivstand);
}
