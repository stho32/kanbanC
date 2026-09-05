namespace KanbanC.Contracts.Karten;

// Eine Teilaufgabe, nicht die ganze Liste — anders als bei den Etiketten. Mehr als den Text
// braucht das Anlegen nicht: die Position bestimmt der Provider (angehängt als höchste + 1), und
// eine frische Teilaufgabe ist nicht abgehakt.
public record TeilaufgabeAnlegenAnfrage(string Text);
