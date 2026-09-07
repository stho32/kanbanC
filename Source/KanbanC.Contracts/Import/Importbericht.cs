namespace KanbanC.Contracts.Import;

// Die Bilanz eines Laufs samt einer Zeile je Knoten. Fünf Zahlen: die vier Fächer des Soll-Ist-
// Vergleichs und daneben Uebersprungen — Zeilen, die nie eine Karte werden und deshalb in keinem
// Fach stehen.
// Der Laufkopf kommt erst dazu, wenn der Bericht den Prozess verlässt: die Uhr und der Urheber
// stehen an der Anfrage, nicht in der Bilanz.
public record Importbericht(
    int Angelegt,
    int Geaendert,
    int Unveraendert,
    int Uebersprungen,
    int Verwaist,
    Kartenzahlen Kartenzahlen,
    IReadOnlyList<Importzeile> Zeilen, // stil-check: C09 wie Spalte.Karten
    Importlaufkopf? Laufkopf);
