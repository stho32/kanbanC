using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Contracts.Karten;

// Ein an der Karte hinterlegter Pfad auf eine Datei, die **nicht** in der Anwendung liegt: eine
// Anforderung, eine WBS-Zeile, ein Architekturdokument im Repository daneben. Der Unterschied
// zum Anhang ist der ganze Zweck — der Anhang bringt eine Kopie mit, der Dateiverweis zeigt auf
// die Datei, die dort weiterlebt, wo sie hingehört.
// Er heißt Dateiverweis und nicht Verweis: das Wort ist in dieser Anwendung schon der
// Hyperlink (Boardverweis, TitelverweisDerKarte, .board-verweis), und zwei Bedeutungen desselben
// Worts stünden auf demselben Schirm nebeneinander.
// Die DateiverweisId überlebt, was um sie herum geschieht. Anders als beim Anhang sind zwei
// gleiche Pfade an derselben Karte aber **kein** zweites Ding: sie zeigen auf dieselbe Datei,
// und die zweite Zeile trägt keine Aussage. Der eindeutige Index in 015 schließt sie aus.
// Der Pfad steht so da, wie er eingetragen wurde — bis auf die Ränder. Trennzeichen werden
// nicht umgeschrieben: ein Pfad, den die Anwendung umschreibt, ist nicht mehr der Pfad, den
// jemand gemeint hat.
// Der Urheber reist als ganzer Kontributor und nicht als Nummer — dieselbe Entscheidung wie bei
// Kommentar und Anhang: die Zeile zeigt Name und den Zusatz „stillgelegt" im title, ohne einen
// zweiten Abruf. Ein „niemand" gibt es nicht.
// Der Zeitstempel heißt Zeitpunkt und nicht EingetragenAm, weil die Namensregel „<Verb>Am" im
// ganzen Stack für ein reines Datum steht. Keine Position: die Reihenfolge ist der Zeitpunkt.
public record Dateiverweis(long DateiverweisId, string Pfad, Kontributor Urheber, DateTimeOffset Zeitpunkt);
