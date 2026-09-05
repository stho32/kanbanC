using KanbanC.Contracts.Karten;

namespace KanbanC.Contracts.Boards;

// Karten kann gekürzt sein, Kartenzahl ist es nie: eine Liste von 20 ohne die Auskunft, dass es
// 137 sind, wäre für einen Agenten eine stille Lüge.
public record Spalte(
    long SpalteId,
    string Bezeichnung,
    int Position,
    bool IstAbschlussspalte,
    int? Anzeigegrenze,
    IReadOnlyList<Karte> Karten, // stil-check: C09 wie Board.Spalten
    int Kartenzahl);

// Innerhalb der BL teilen sich kartenlose Spalten dieselbe leere Liste, dadurch bleibt die
// Werte-Gleichheit des Records für sie erhalten. Nach dem Weg über JSON gilt das nicht:
// die Oberfläche baut je Abruf frische Listen, und der Record vergleicht diesen Member
// per Referenz. Wer sich auf Gleichheit verlässt, muss wissen, auf welcher Seite er steht.
