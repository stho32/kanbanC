namespace KanbanC.BL.Models.Import;

// Eine Bahn des Zielboards, so viel davon, wie die Zielspaltenwahl braucht: Nummer, Name, Ordnung
// und die eine markierte Eigenschaft des Schemas.
public record Importspalte(long SpalteId, string Bezeichnung, int Position, bool IstAbschlussspalte);
