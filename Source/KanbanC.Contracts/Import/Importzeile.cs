namespace KanbanC.Contracts.Import;

// Eine Zeile des Importberichts: was der Knoten war und was aus ihm wurde. Der Grund steht an
// jeder Zeile, an der etwas zu sagen ist — übersprungen, zurückgenommene Abhakung,
// Statusabweichung, Dublettenverdacht, verwaiste Karte.
// Die Kennung ist die Knoten-ID (I0001) oder, wenn die Zeile nicht einmal eine hergab, die
// Zeilennummer in der Datei: eine Zeile mit zu wenigen Zellen hat keine ID, die man nennen könnte.
// Die Kartennummer steht nur dort, wo es eine gibt: an einer wiedererkannten oder verwaisten
// Karte. Eine anzulegende Karte hat in der Vorschau noch keine — sie entsteht erst im dritten
// Schritt, und erst dessen Bericht trägt sie nach.
// Die KarteId steht **neben** der Nummer und nicht statt ihrer: die Nummer ist das, was ein Mensch
// liest und in einem Kommentar nennt, die KarteId das, was ein Weg braucht.
public record Importzeile(string Kennung, string? Ebene, Importwirkung Wirkung, string? Grund, string? Kartennummer, long? KarteId);
