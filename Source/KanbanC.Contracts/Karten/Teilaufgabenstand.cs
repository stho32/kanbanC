namespace KanbanC.Contracts.Karten;

// Der Abhakstand als eigener Rumpf, nicht als Umschalter ohne Rumpf: ein PUT, das kippt, ist
// nicht wiederholbar — ein Agent, der denselben Aufruf zweimal absetzt, käme sonst beim
// Ausgangszustand heraus. So setzt derselbe Aufruf zweimal denselben Stand.
public record Teilaufgabenstand(bool Abgehakt);
