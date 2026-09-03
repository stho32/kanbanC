
namespace KanbanC.Contracts.Fehler;

// Der Rumpf jeder Fehlerantwort der API, bei 400 wie bei 404. Ob eine Regel verletzt wurde oder
// ein Ding fehlte, sagen der Statuscode und der Code des Befunds.
public record Zurueckweisung(IReadOnlyList<Fehlerbefund> Befunde); // stil-check: C09 wie Spalte.Karten
