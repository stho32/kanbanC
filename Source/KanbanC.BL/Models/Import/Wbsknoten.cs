namespace KanbanC.BL.Models.Import;

// Eine gelesene Knotenzeile der WBS-Datei — alle zwölf Zellen, auch die, aus denen dieser Slice
// nichts macht: Aufwand, Fluss und Ausbaustufe kommen mit keinem Feld auf das Board, gehören aber
// zum gelesenen Knoten. Ein Leser, der sie wegwirft, wäre für den nächsten Slice blind.
// Die Zeilennummer reist mit, weil eine Meldung über eine übersprungene Zeile ohne sie niemanden
// zur Stelle führt.
public record Wbsknoten(
    string Id,
    Wbsebene Ebene,
    string Eltern,
    string Name,
    Wbsstatus Status,
    string Fertigkriterium,
    string Fluss,
    string Aufwand,
    string Ausbaustufe,
    string Braucht,
    string Requirement,
    string Notiz,
    int Zeilennummer);
