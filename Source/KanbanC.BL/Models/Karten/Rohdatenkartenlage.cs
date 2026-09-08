using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Models.Karten;

// Die Karte des Boards, bevor ihre fünf Listen an ihr hängen: Kartenzeile, Ort, Archivmarke und
// Kartenklasse aus **einer** Abfrage. Die Listen kommen aus fünf weiteren und werden erst von der
// Integration angesetzt — der Kartenleser kennt die n-Leser nicht.
internal sealed record Rohdatenkartenlage(
    Karte Karte,
    long Spalte,
    string Spaltenbezeichnung,
    Archivierung Archivstand,
    Kartenklasse? Kartenklasse);
