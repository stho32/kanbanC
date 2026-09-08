namespace KanbanC.Contracts.Auswertungen;

// Die Zahlen über der Fieberkurve, **gerechnet geliefert** — die Oberfläche summiert nichts nach.
// Stunden als decimal wie das Zeitband, Anteile als ganze Prozent.
// **`null` und `0,0` sind verschiedene Antworten**: ohne Bandsumme gibt es keinen Kettenpuffer
// (`null`), bei lauter Punktschätzungen gibt es ihn und er ist `0,0` — dann hat der Verbrauch eine
// Zahl und nur sein Anteil keine, weil es nichts gibt, wovon er ein Anteil wäre.
// Die Kartenzahl steht als zweite Lesart des Fortschritts daneben: die Achse trägt Stunden gegen
// Stunden, aber „2 von 5 erledigt" ist die Zahl, die ein Mensch nachzählen kann.
public record Pufferkopfzahlen(
    decimal? KettenpufferStunden,
    decimal? VerbrauchteStunden,
    decimal? VerbrauchsanteilProzent,
    decimal? FortschrittProzent,
    int ErledigteKarten,
    int Kartenanzahl,
    int KartenOhneSoll);
