namespace KanbanC.BL.Models.Import;

// Der Kopf der Datei. Application ist die Pflichtangabe — sie sagt, dass es überhaupt eine WBS
// ist; Sprache und Stand reisen mit, weil sie dastehen, und kosten nichts.
public record Wbsfrontmatter(string Application, string Sprache, string Zuletzt);
