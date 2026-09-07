namespace KanbanC.BL.Models.Import;

// Eine Zeile, die nicht zum Knoten wurde — mit dem gefundenen Wert im Grund, nie mit „ungültig“.
// Die Kennung ist die Knoten-ID, wo es eine gab, sonst die Zeilennummer: eine Zeile mit zu wenigen
// Zellen hat keine ID, die man nennen könnte.
public record Uebersprungenezeile(string Kennung, int Zeilennummer, string Grund);
