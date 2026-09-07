namespace KanbanC.BL.Models.Import;

// Eine wiedererkannte Karte mit dem, was an ihr nachgezogen wird — und **nur** damit: Spalte,
// Position, Verantwortlicher, Fälligkeit, Farbe, Zeiten, Kommentare, Anhänge, Kartennummer und
// Archivstand stehen hier nicht, weil sie Arbeit am Board sind und nicht Abschrift der Datei.
public record Kartenaktualisierungsauftrag(
    long KarteId,
    string Titel,
    string? Beschreibung,
    Etikettenabgleichergebnis Etiketten,
    Teilaufgabenabgleichergebnis Teilaufgaben);
