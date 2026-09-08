namespace KanbanC.Contracts.Auswertungen;

// Eine Karte des Bestands, wie die Puffertabelle sie zeigt: ihr Sollband, die erfasste Zeit und
// der Puffer, den sie verbraucht hat.
// Sollband und verbrauchter Puffer sind zusammen null oder zusammen gesetzt: **ohne Band gibt es
// weder Puffer noch Verbrauch** — nicht 0,0.
// Der Erledigungsstand steht dabei, weil der Fortschritt aus ihm entsteht; der Archivstand, weil
// die Zeit einer archivierten Karte geleistet wurde und in der Rechnung bleibt — sie fällt nicht
// aus der Tabelle, sondern steht markiert darin.
public record Pufferzeile(
    long KarteId,
    string? Kartennummer,
    string Titel,
    Zeitband? Sollband,
    TimeSpan ErfassteZeit,
    decimal? VerbrauchterPufferStunden,
    bool IstErledigt,
    bool IstArchiviert);
