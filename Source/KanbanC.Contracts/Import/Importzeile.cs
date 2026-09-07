namespace KanbanC.Contracts.Import;

// Eine Zeile des Importberichts: was der Knoten war und was aus ihm wurde. Der Grund steht nur
// an übersprungenen Zeilen — an einer angelegten Karte wäre er eine leere Auskunft.
// Die Kennung ist die Knoten-ID (I0001) oder, wenn die Zeile nicht einmal eine hergab, die
// Zeilennummer in der Datei: eine Zeile mit zu wenigen Zellen hat keine ID, die man nennen könnte.
public record Importzeile(string Kennung, string? Ebene, Importwirkung Wirkung, string? Grund);
