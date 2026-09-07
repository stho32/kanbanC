using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Models.Auswertungen;

// Was **ein** Dienstaufruf für beide Routen liefert: die Zeilen für die Datei und den Stand für
// den Schirm. Zwei Aufrufe nebeneinander ließen den Schirm etwas anderes zählen, als die Datei
// enthält.
public record Zeitexport(Zeitexportzeilen Zeilen, Zeitexportstand Stand);
