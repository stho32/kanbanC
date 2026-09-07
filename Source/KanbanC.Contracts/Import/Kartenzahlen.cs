namespace KanbanC.Contracts.Import;

// Wie viele Karten jede Wahl der Schnittebene ergäbe — die Zahl steht im Bild, bevor jemand sie
// erzeugt. Sie reist in **jeder** Antwort und nicht nur bei trocken=true: der Regler in Schritt 2
// braucht sie ohne zweiten Aufruf, und ein Agent sieht daran, was ein anderer Schnitt gekostet
// hätte.
public record Kartenzahlen(int Dialog, int Interaction, int Feature, int Bubble);
