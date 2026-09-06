namespace KanbanC.Contracts.Karten;

// Ein Anhang, nicht die ganze Liste — wie beim Anlegen einer Teilaufgabe und beim Kommentar.
// Kontributor ist long und nicht long?: es gibt kein „niemand". Ein Anhang ohne Urheber ist nach
// dem Fertig-Kriterium keiner, und die Spalte trägt NOT NULL.
// Der Dateiname ist der **gemeldete** Name; er wird auf seinen letzten Pfadbestandteil gekürzt,
// bevor er in die Spalte geht. Die Dateigroesse ist die **gemeldete** Größe und dient allein der
// Zurückweisung vor dem Schreiben — gespeichert wird, was tatsächlich auf der Platte landet.
// Kein Feld für den Zeitpunkt und keines für den Ablagepfad: den einen setzt die Anwendung, den
// anderen rechnet sie.
public record AnhangAnlegenAnfrage(string Dateiname, long Dateigroesse, long Kontributor);
