namespace KanbanC.Contracts.Auswertungen;

// Die Zahlen über der Kurve, **gerechnet geliefert** — die Oberfläche summiert nichts nach.
// `Offen` ist der Wert der Kurve am letzten Tag, `Erledigt` ist `ImBestand − Offen`: jede andere
// Lesart erzeugte eine Kopfzahl, die nicht zum rechten Rand der Kurve passt.
// `OhneErledigungsdatum` zählt die Karten in einer Abschlussspalte oder im Archiv, die kein
// Erledigungsdatum tragen — sie verlassen die Kurve nie, und die Fußzeile nennt ihre Zahl, statt
// sie zu verschweigen.
public record Burndownkopfzahlen(int Offen, int Erledigt, int ImBestand, int OhneErledigungsdatum);
