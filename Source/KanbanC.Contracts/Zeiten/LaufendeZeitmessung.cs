using KanbanC.Contracts.Karten;

namespace KanbanC.Contracts.Zeiten;

// Ein laufender Zeiteintrag samt dem Ort, an dem gemessen wird. Der Zeiteintrag reist
// **unverändert** weiter — es gibt kein zweites Zeiteintrag-DTO; der Umschlag legt nur den Ort
// dazu, wie Klassenkarte es für die Karte tut.
// Die Karte reist als ganze Karte und nicht als Nummer: Kartennummer und Titel stehen damit ohne
// zweiten Abruf in der Zeile, und die Antwort trägt dieselbe Kartengestalt wie überall sonst.
// Das Board steht dabei, weil die Auskunft über **alle** Boards geht und ein Kartentitel allein
// nicht sagt, wo die Karte liegt.
// **Ein** Feld Archiviert für Karte und Board: für den Leser ist die Folge dieselbe — die Karte
// steht in keiner Bahn mehr — und die Kompensation dieselbe: öffnen und stoppen.
public record LaufendeZeitmessung(Zeiteintrag Zeiteintrag, Karte Karte, long Board, string Boardname, bool Archiviert);
