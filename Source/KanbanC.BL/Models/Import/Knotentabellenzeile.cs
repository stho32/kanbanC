namespace KanbanC.BL.Models.Import;

// Eine Zeile der Knotentabelle mit ihrer Nummer in der Datei. Die Nummer reist mit, weil eine
// Meldung über eine übersprungene Zeile ohne sie niemanden zur Stelle führt.
public record Knotentabellenzeile(int Zeilennummer, Wbszellen Zellen);
