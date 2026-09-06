namespace KanbanC.Contracts.Klassen;

// Genau die zwei Angaben, mit denen eine Kartenklasse entsteht. **Kein Feld für den
// Zaehlerstand:** der Aufrufer setzt ihn nicht, er beginnt bei 0.
public record KartenklasseAnlegenAnfrage(string Name, string Praefix);
