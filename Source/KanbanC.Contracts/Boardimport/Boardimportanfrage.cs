namespace KanbanC.Contracts.Boardimport;

// Alles, was ein Lauf außer der Datei selbst braucht. Sie reist als multipart-Formular und nicht
// als JSON, weil die Datei danebenliegt — dieselbe Lage wie beim WBS-Import und beim Anhang.
// **Trocken hat die Vorgabe true**: fehlt das Feld, wird nichts geschrieben. Ein vergessenes Feld
// legte sonst ein ganzes Board mit allen Karten, Personen und Zeiten an — und im ganzen Bestand
// gibt es keinen Weg, ein Board wieder zu löschen, nur zu archivieren.
public record Boardimportanfrage(bool Trocken, string Dateiname);
