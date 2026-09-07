namespace KanbanC.BL.Models.Import;

// Eine Karte, wie sie aus einem Knoten würde — **bevor** irgendetwas geschrieben ist. Der Entwurf
// trägt alles, was der Schreiblauf braucht, und nichts, was ein Board liefern müsste: die
// Zielspalte steht nicht darin, sie hängt an den Bahnen des gewählten Boards.
public record Kartenentwurf(
    Wbsknoten Knoten,
    string Titel,
    string? Beschreibung,
    IReadOnlyList<string> Etiketten, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<Teilaufgabenentwurf> Teilaufgaben, // stil-check: C09 wie Spalte.Karten
    string Dateiverweis,
    Sollband? Sollband);
