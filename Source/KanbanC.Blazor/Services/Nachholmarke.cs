namespace KanbanC.Blazor.Services;

// Was an einer Karte steht, die sich während der Trennung bewegt hat.
// **Ohne Wer und ohne Wann, und das ist eine Entscheidung:** die Zahl im Band kommt aus einem
// Vergleich, und ein Vergleich weiß, *was* sich geändert hat — nicht, *wer* es war und *wann*. Ein
// Name oder eine Uhrzeit hier wäre geraten; der Zeitpunkt steht einmal im Band, wo er stimmt.
// **Die einzige Marke ohne Frist:** nach einer Trennung ist sie kein Zuruf mehr, sondern das
// Protokoll der Lücke, und das darf man in Ruhe lesen. Sie geht mit dem Band.
public static class Nachholmarke
{
    public const string Wortlaut = "geändert, während die Verbindung weg war";
}
