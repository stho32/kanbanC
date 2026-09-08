using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Models.Karten;

// Die Karte des Boards, bevor ihre fünf Listen an ihr hängen: Kartenzeile, Ort, Archivmarke und
// Kartenklasse aus **einer** Abfrage. Die Listen kommen aus fünf weiteren und werden erst von der
// Integration angesetzt — der Kartenleser kennt die n-Leser nicht.
// Der Zaehlerstand steht **neben** der Kartennummer, die die Karte schon trägt: aus „AB203" ist er
// nicht sicher zurückzurechnen, weil ein Präfix selbst Ziffern tragen darf. null heißt „diese
// Karte trägt keine Klasse".
internal sealed record Rohdatenkartenlage(
    Karte Karte,
    long Spalte,
    string Spaltenbezeichnung,
    Archivierung Archivstand,
    Kartenklasse? Kartenklasse,
    int? Zaehlerstand);
