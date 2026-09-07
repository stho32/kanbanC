namespace KanbanC.Contracts.Import;

// Eine Zeile des Importberichts: was der Knoten war und was aus ihm wurde. Der Grund steht an
// jeder Zeile, an der etwas zu sagen ist — übersprungen, zurückgenommene Abhakung,
// Statusabweichung, Dublettenverdacht, verwaiste Karte.
// Die Kennung ist die Knoten-ID (I0001) oder, wenn die Zeile nicht einmal eine hergab, die
// Zeilennummer in der Datei: eine Zeile mit zu wenigen Zellen hat keine ID, die man nennen könnte.
// Die Kartennummer steht nur dort, wo es eine gibt: an einer wiedererkannten oder verwaisten
// Karte. Eine anzulegende Karte hat noch keine — sie entsteht erst im dritten Schritt.
public record Importzeile(string Kennung, string? Ebene, Importwirkung Wirkung, string? Grund, string? Kartennummer);
