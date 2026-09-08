using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.Contracts.Export;

// Ein Board als **ein** Gegenstand: eigenständig ist eine Datei, die niemanden mehr braucht —
// keinen zweiten Aufruf, keinen Auflöser, kein Werkzeug. Was sie nicht tragen kann, sagt sie im
// Kopf.
// Jede Nummer, auf die eine Zeile zeigt, steht als Zeile in derselben Datei, und neben jeder
// Nummer steht ihr Name: die Spalte mit Bezeichnung, die Kartenklasse mit Name und Präfix, der
// Kontributor mit Name.
// Die Kontributoren sind die **referenzierten** dieses Boards, nicht die Personenliste der
// Installation — ein Board exportiert seinen Inhalt, nicht das Adressbuch daneben.
// Die Karten stehen flach und nicht in die Spalten geschachtelt: der Ort steht an der Karte, und
// geschachtelt stünde jede Karte zweimal in der Datei.
public record Boardexport(
    Exportkopf Kopf,
    Exportboard Board,
    IReadOnlyList<Exportspalte> Spalten, // stil-check: C09 wie Board.Spalten
    IReadOnlyList<Kartenklasse> Kartenklassen, // stil-check: C09 wie Board.Spalten
    IReadOnlyList<Kontributor> Kontributoren, // stil-check: C09 wie Board.Spalten
    IReadOnlyList<Exportkarte> Karten, // stil-check: C09 wie Board.Spalten
    IReadOnlyList<Zeiteintrag> Zeiteintraege); // stil-check: C09 wie Board.Spalten
