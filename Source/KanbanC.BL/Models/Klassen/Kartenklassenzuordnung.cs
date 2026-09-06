namespace KanbanC.BL.Models.Klassen;

// Die geschriebene Zuordnungszeile mit dem vergebenen Zaehlerstand. Sie bleibt in der
// Fachlogik und reist nicht in die Contracts: nach außen sichtbar ist die fertige Nummer
// (Karte.Kartennummer), nicht der Stand, aus dem sie gebildet wird.
public record Kartenklassenzuordnung(long KartenklassenzuordnungId, long Karte, long Kartenklasse, int Zaehlerstand);
