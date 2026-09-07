namespace KanbanC.Contracts.Auswertungen;

// Die Lage der erfassten Zeit zum Sollband. Der Überschuss steht nur bei `UeberDemBand` — unter
// dem Band und im Band gibt es keine Zahl, die die Schätzung hergäbe.
public record Abweichung(Abweichungslage Lage, decimal? UeberschussStunden);
