using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Contracts.Karten;

// Eine Äußerung an der Karte mit eigener Identität: die KommentarId überlebt, was um sie herum
// geschieht, und zwei gleichlautende Kommentare sind zwei Äußerungen.
// Der Urheber reist als ganzer Kontributor und nicht als Nummer — dieselbe Entscheidung wie bei
// Kartendetail.Verantwortlicher: die Zeile zeigt Name und Kürzel, das Kürzel folgt aus Name und
// Art, und StillgelegtAm liefert den Zusatz „stillgelegt" ohne ein zweites Feld und ohne einen
// zweiten Abruf. Anders als dort gibt es kein „niemand": ein Kommentar ohne Urheber ist keiner.
// Der Zeitstempel heißt Zeitpunkt und nicht GeschriebenAm, weil die Namensregel „<Verb>Am" im
// ganzen Stack für ein reines Datum steht; dieser Wert trägt als erster eine Uhrzeit.
// DateTimeOffset statt DateTime, weil der Wert über HTTP zu Agenten reist und ein DateTime
// unterwegs seine Zeitzone verliert.
// Keine Position: die Reihenfolge ist der Zeitpunkt.
public record Kommentar(long KommentarId, string Text, Kontributor Urheber, DateTimeOffset Zeitpunkt);
