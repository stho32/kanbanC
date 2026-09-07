namespace KanbanC.BL.Models.Import;

// Eine vorhandene Teilaufgabe, die nachgezogen wird: derselbe Knoten, neuer Text oder neuer Haken.
// **Der alte Haken reist mit**, weil die Zeile im Bericht sagen muss, was geschieht: ein Haken, den
// die Datei setzt, ist eine andere Auskunft als einer, den sie zurücknimmt.
public record Teilaufgabenaenderung(long TeilaufgabeId, string Text, int Position, bool Abgehakt, bool WarAbgehakt)
{
    // Wer am Board abhakt und dessen Knoten in der Datei nicht gruen ist, findet den Haken nach dem
    // Lauf zurückgenommen — und erfährt es an der Zeile seines Knotens.
    public bool DieAbhakungWirdZurueckgenommen => WarAbgehakt && !Abgehakt;

    public bool DerHakenWirdGesetzt => !WarAbgehakt && Abgehakt;
}
