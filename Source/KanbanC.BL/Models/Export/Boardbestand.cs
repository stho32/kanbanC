using KanbanC.Contracts.Export;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Models.Export;

// Was die Datenbank über ein Board hält — alles, was der Boardexport trägt, außer dem Kopf: der
// sagt, wann und von welcher Anwendung die Datei entstand, und das weiß keine Zeile der
// Datenbank.
public record Boardbestand(
    Exportboard Board,
    IReadOnlyList<Exportspalte> Spalten, // stil-check: C09 wie Board.Spalten
    IReadOnlyList<Kartenklasse> Kartenklassen, // stil-check: C09 wie Board.Spalten
    IReadOnlyList<Kontributor> Kontributoren, // stil-check: C09 wie Board.Spalten
    IReadOnlyList<Exportkarte> Karten, // stil-check: C09 wie Board.Spalten
    IReadOnlyList<Zeiteintrag> Zeiteintraege); // stil-check: C09 wie Board.Spalten
