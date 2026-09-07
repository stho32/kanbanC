namespace KanbanC.BL.Models.Import;

// Was mit den Etiketten einer wiedererkannten Karte geschieht. **Fremd** ist auch hier das Fach
// des Menschen: ein Etikett, das die Datei gar nicht erzeugen kann, hat er gesetzt (`I0015`) und
// bleibt — sonst wäre jede von Hand ergänzte Karte auf ewig „geändert“.
public record Etikettenabgleichergebnis(
    IReadOnlyList<string> Anzulegen, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<string> ZuEntfernen, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<string> Unveraendert, // stil-check: C09 wie Spalte.Karten
    IReadOnlyList<string> Fremd); // stil-check: C09 wie Spalte.Karten
