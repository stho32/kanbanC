using KanbanC.Contracts.Zeiten;

namespace KanbanC.Contracts.Boards;

// Die laufenden Zeiteinträge reisen am Board und nicht an Karte: an der Karte wären es 57
// Konstruktionsstellen und vier Leseabfragen für eine n-Beziehung, hier ist es ein Feld und eine
// zusätzliche Abfrage je Boardabruf. Der Preis: die Zuordnung Eintrag-zu-Karte macht die Bahn.
// Die Liste ist flach über alle Spalten und trägt nur Einträge ohne Ende — „laufend" steht schon
// im Namen. Kein laufender Timer heißt eine leere Liste, nie null.
public record Board(
    long BoardId,
    string Name,
    BoardArt Art,
    DateOnly? Starttermin,
    DateOnly? Zieltermin,
    IReadOnlyList<Spalte> Spalten,
    bool ZeigtKartenzahl,
    bool IstArchiviert,
    IReadOnlyList<Zeiteintrag> LaufendeZeiteintraege);
