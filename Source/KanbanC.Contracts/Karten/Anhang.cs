using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Contracts.Karten;

// Eine an der Karte liegende Datei mit eigener Identität: die AnhangId überlebt, was um sie herum
// geschieht, und zwei Dateien gleichen Namens sind zwei Dateien. Sie ist zugleich der Name der
// Datei auf der Platte — der Originalname lebt nur in Dateiname.
// Der Urheber reist als ganzer Kontributor und nicht als Nummer — dieselbe Entscheidung wie bei
// Kommentar.Urheber: die Zeile zeigt Name und Kürzel im title, und StillgelegtAm liefert den
// Zusatz „stillgelegt" ohne einen zweiten Abruf. Ein „niemand" gibt es nicht: ein Anhang ohne
// Urheber ist keiner.
// Die Dateigroesse steht in Bytes und nicht als Text: die Umrechnung in „41 kB" ist Darstellung
// und gehört in die Oberflächenschicht — sonst wäre keine Grenze mehr rechenbar.
// Der Zeitstempel heißt Zeitpunkt und nicht AngehaengtAm, weil die Namensregel „<Verb>Am" im
// ganzen Stack für ein reines Datum steht. Keine Position: die Reihenfolge ist der Zeitpunkt.
// Kein Feld für den Ablagepfad: den rechnet der Anhangpfad.
public record Anhang(long AnhangId, string Dateiname, long Dateigroesse, Kontributor Urheber, DateTimeOffset Zeitpunkt);
