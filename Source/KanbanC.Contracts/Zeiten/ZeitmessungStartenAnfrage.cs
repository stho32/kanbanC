namespace KanbanC.Contracts.Zeiten;

// Der Kontributor wird mitgegeben und nie erraten: die Identität ist ein Browserzustand je Tab
// und kein Login — geraten hieße, immer denselben zu nehmen. Dasselbe tut
// KommentarSchreibenAnfrage für den Urheber.
// **Kein Feld für den Beginn:** den setzt die Anwendung. Könnte der Aufrufer ihn mitgeben, könnte
// ein Agent die Reihenfolge fälschen — dieselbe Entscheidung wie beim Kommentarzeitpunkt.
public record ZeitmessungStartenAnfrage(long Kontributor);
