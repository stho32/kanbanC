namespace KanbanC.BL.Models.Import;

// Was eine Anlage hinterlässt: der Dateiverweis, aus dem sie entstand, und die beiden Kennungen
// der entstandenen Karte. Der **Dateiverweis** ist der Schlüssel — dieselbe Kupplung, an der ein
// zweiter Lauf wiedererkennt, und damit der einzige Weg zurück zur Zeile, der weder an der
// Reihenfolge noch am Titel hängt.
public record Kartenanlageergebnis(string Dateiverweis, long KarteId, string Kartennummer);
