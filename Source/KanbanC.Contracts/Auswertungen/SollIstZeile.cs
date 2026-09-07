namespace KanbanC.Contracts.Auswertungen;

// Eine Karte des Bestands mit ihren beiden Zahlen. **Die Zeiteinträge reisen nicht mit** — die
// Antwort ist die Auswertung und nicht ihr Rohstoff; wer die einzelnen Einträge braucht, holt sie
// an der Karte.
// Sollband und Abweichung sind zusammen null oder zusammen gesetzt: ohne Band gibt es keine
// Abweichung — nicht `im Band` und nicht `0`.
// Der Archivstand steht dabei, weil die Zeit einer archivierten Karte geleistet wurde und in der
// Summe bleibt; sie fällt nicht aus der Tabelle, sondern steht markiert darin.
public record SollIstZeile(
    long KarteId,
    string? Kartennummer,
    string Titel,
    TimeSpan ErfassteZeit,
    Zeitband? Sollband,
    Abweichung? Abweichung,
    bool IstArchiviert);
