namespace KanbanC.Contracts.Export;

// Was die Datei über sich selbst sagt — **einschließlich der Lücke**. Der Anhanghinweis steht als
// Satz darin und nicht nur in der Anforderung: die Bytes der Anhänge reisen nicht mit, und eine
// Datei, die das verschweigt, verspricht mehr, als sie hält.
// Die Fassungsnummer ist die Zusage an den Import: er prüft sie, bevor er etwas schreibt.
public record Exportkopf(string Anwendung, int Fassung, DateTimeOffset ErzeugtAm, string Anhanghinweis);
