namespace KanbanC.BL.Models.Import;

// Ein Nachfahre des Kartenknotens als abhakbarer Schritt: die ID vorn, dann der Name — und der
// Haken, wenn der Knoten gruen oder bestehend ist.
public record Teilaufgabenentwurf(string Text, bool Abgehakt);
