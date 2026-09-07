using KanbanC.Contracts.Import;

namespace KanbanC.BL.Models.Import;

// Eine Berichtszeile mit ihrer Stelle in der Datei. Die Nummer bleibt in der Fachlogik: nach außen
// reist die Zeile, und die Reihenfolge des Berichts ist die der Datei — dieselbe, in der ein
// Mensch sie liest.
public record Importberichtzeile(int Zeilennummer, Importzeile Zeile);
