namespace KanbanC.BL.Models.Import;

// Eine Teilaufgabe, wie sie an der Karte steht — mit ihrer Kennung, weil ein Nachziehen sie
// braucht. Der Text trägt die Knoten-ID vorn, wo die Teilaufgabe aus der Datei stammt; wo nicht,
// hat ein Mensch sie angelegt.
public record Teilaufgabenstand(long TeilaufgabeId, string Text, int Position, bool Abgehakt);
