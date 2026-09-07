namespace KanbanC.BL.Models.Import;

// **Das eine Vergleichbare beider Seiten.** Der Sollentwurf und der Iststand einer Karte sind
// ehrlich verschieden — links hängt ein Wbsknoten, rechts eine KarteId mit Nummer, Spalte und
// Zeiten. Verglichen wird deshalb nicht der Träger, sondern dieses Abbild.
// Das Sollband steht darin, weil es aus der Datei stammt und sich mit ihr ändert.
// Was nicht darin steht, wird nie verglichen und ist damit nie ein Grund für „geändert“: Spalte,
// Position, Verantwortlicher, Fälligkeit, Farbe, Zeiten, Kommentare, Anhänge, Kartennummer,
// Archivstand und ErledigtAm. Der Dateiverweis fehlt ebenfalls — er ist der Schlüssel, und ein
// Schlüssel, der zugleich Vergleichsfeld ist, könnte nie „geändert“ ergeben.
public record Kartenabbild(
    string Titel,
    string? Beschreibung,
    IReadOnlyList<string> Etiketten, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<Teilaufgabenentwurf> Teilaufgaben, // stil-check: C09 wie Spalte.Karten
    Sollband? Sollband);
