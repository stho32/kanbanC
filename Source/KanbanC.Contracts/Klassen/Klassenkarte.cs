using KanbanC.Contracts.Karten;

namespace KanbanC.Contracts.Klassen;

// Eine Karte einer Kartenklasse samt dem Ort, an dem sie liegt. Der Abruf sammelt über die
// Spaltengrenze hinweg; ohne ihre Spalte bliebe jede Karte die Auskunft schuldig, die den Abruf
// erst nützlich macht. Board und Boardname reisen nicht mit — sie stehen in der Adresse.
public record Klassenkarte(Karte Karte, long Spalte, string Spaltenbezeichnung);
