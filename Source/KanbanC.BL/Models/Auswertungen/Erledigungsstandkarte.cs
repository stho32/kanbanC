namespace KanbanC.BL.Models.Auswertungen;

// Eine gelesene Karte des Bestands, wie der Burndown sie braucht: ihr Erledigungstag und das,
// womit die Tageszeile sich benennt. Das Wort „Karte“ steht bewusst im Namen — der
// `Erledigungsstand` in Operations/Karten entscheidet über den Zug einer einzelnen Karte und
// meint etwas anderes.
// `ErledigtAm` fehlt, wenn die Karte keine Erledigungszeile trägt: **fehlende Zeile heißt nicht
// erledigt**, auch wenn die Karte archiviert ist oder in einer Abschlussspalte steht.
public record Erledigungsstandkarte(
    long KarteId,
    string? Kartennummer,
    string Titel,
    DateOnly? ErledigtAm,
    bool IstArchiviert,
    bool StehtInAbschlussspalte);
