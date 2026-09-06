namespace KanbanC.Contracts.Klassen;

// Ein benannter Nummernkreis eines Boards. Der Zaehlerstand reist mit, weil die Zeile im Board
// die nächste Nummer zeigt, bevor die Karte existiert.
public record Kartenklasse(long KartenklasseId, string Name, string Praefix, int Zaehlerstand);
