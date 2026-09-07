namespace KanbanC.BL.Models.Import;

// Was aus einer gelesenen Datei herauskommt: der Kopf, der Baum und alles, was nicht mitkam.
// Die Übersprungenen reisen **neben** dem Baum und nicht in ihm — sie sind keine Knoten, und
// sie zu verschweigen wäre die eine Sache, die hier nirgends erlaubt ist.
public record Wbsbestand(Wbsfrontmatter Frontmatter, Wbsbaum Baum, IReadOnlyList<Uebersprungenezeile> Uebersprungene); // stil-check: C09 wie Spalte.Karten
