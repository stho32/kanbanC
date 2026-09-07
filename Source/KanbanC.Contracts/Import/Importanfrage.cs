namespace KanbanC.Contracts.Import;

// Alles, was ein Lauf außer der Datei selbst braucht. Sie reist als multipart-Formular und nicht
// als JSON, weil die Datei danebenliegt — dieselbe Lage wie beim Anhang.
// **Trocken hat die Vorgabe true**: fehlt das Feld, wird nichts geschrieben. Eine Datei mit 540
// Zeilen erzeugte sonst unbesehen hunderte Karten, und die teure Richtung gehört nie in die
// Vorgabe.
// Der Pfad ist der Weg zurück in die Datei; ein Browser liefert beim Upload nur den Dateinamen,
// das Repository kennt aber den ganzen Weg. Fehlt er, gilt der Dateiname — kürzer, nicht falsch.
// Der Kontributor ist Pflicht und keine Zierde: Kartendateiverweis trägt Kontributor NOT NULL,
// ohne Urheber entsteht kein Verweis und damit keine vollständige Karte.
public record Importanfrage(
    long Kartenklasse,
    Schnittebene Schnittebene,
    string? Pfad,
    bool Trocken,
    long? Kontributor,
    string Dateiname);
