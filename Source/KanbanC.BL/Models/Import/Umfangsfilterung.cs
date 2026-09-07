namespace KanbanC.BL.Models.Import;

// Der Baum ohne die Knoten, die auch in der WBS nicht zum Umfang zählen — und die Zeilen, die
// sagen, welche das waren.
public record Umfangsfilterung(Wbsbaum Baum, IReadOnlyList<Uebersprungenezeile> Uebersprungene); // stil-check: C09 wie Spalte.Karten
