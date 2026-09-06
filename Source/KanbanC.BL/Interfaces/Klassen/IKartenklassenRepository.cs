using KanbanC.BL.Models;
using KanbanC.BL.Models.Klassen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Interfaces.Klassen;

public interface IKartenklassenRepository
{
    IReadOnlyList<Kartenklasse>? LadeAlle(long boardId);

    Ergebnis<Kartenklasse>? LegeAn(long boardId, KartenklasseAnlegenAnfrage anfrage);

    // Die Karten dieser Kartenklasse über alle Spalten des Boards hinweg, in der Ordnung ihres
    // Nummernkreises und ungekürzt. null heißt: diese Kartenklasse gehört nicht zu diesem Board;
    // eine Kartenklasse ohne Karten liefert die leere Liste.
    IReadOnlyList<Klassenkarte>? LadeKartenDerKartenklasse(long boardId, long kartenklasseId, Archivierung archivstand);

    // null heißt: diese KartenklasseId gibt es nirgends. Nennt sie ein anderes Board, gehört
    // die Kartenklasse einem fremden — der Unterschied zwischen „gibt es nicht“ und „gibt es,
    // nur nicht hier“. Dieselbe Auskunft wie BoardDerKarte an den Karten.
    long? BoardDerKartenklasse(long kartenklasseId);

    // Vergibt die nächste Nummer der Kartenklasse und schreibt die Zuordnung — beides in
    // **einer** Transaktion. Trägt die Karte dieselbe Kartenklasse schon, bleibt alles stehen
    // und es wird **keine** Nummer verbraucht. null heißt: diese Karte gibt es nicht, oder die
    // Kartenklasse gehört nicht zu ihrem Board.
    Kartenklassenzuordnung? OrdneZu(long karteId, long kartenklasseId);

    // Nimmt der Karte ihre Zuordnung; der Zaehlerstand der Kartenklasse bleibt **unverändert**,
    // die Nummer verfällt. Eine Karte ohne Zuordnung zu lösen ist kein Fehler — das Ziel ist
    // erreicht. false heißt: diese Karte gibt es nicht.
    bool LoeseZuordnung(long karteId);
}
