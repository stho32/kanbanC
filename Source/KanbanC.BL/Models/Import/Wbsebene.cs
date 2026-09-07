namespace KanbanC.BL.Models.Import;

// Die Ebenen der Planungshierarchie in ihrer Ordnung: Application ganz oben, Bubble ganz unten.
// Die Reihenfolge der Werte ist die fachliche Ordnung und wird verglichen — ein Knoten liegt
// „über der Schnittebene“, wenn seine Ebene einen kleineren Wert trägt.
// **Die Werte sind ein Vertrag mit der Datei**, nicht mit dieser Anwendung: sie stehen so im
// Skill work-breakdown-structure, der die WBS-Dateien erzeugt.
public enum Wbsebene
{
    Application,
    Dialog,
    Interaction,
    Feature,
    Bubble,
}
