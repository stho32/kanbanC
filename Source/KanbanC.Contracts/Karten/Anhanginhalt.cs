namespace KanbanC.Contracts.Karten;

// Die einzige Antwort dieses Slices, die keine JSON ist: der Name für den Content-Disposition-Kopf
// und der Strom für den Rumpf. Die Metadaten stehen im Kartendetail — eine zweite Adresse dafür
// wäre eine zweite Wahrheit über dieselbe Zeile.
// Der Inhalt reist als Strom und nicht als Byte-Array: bei 10 MB je Abruf wäre das vermeidbarer
// Druck auf den Arbeitsspeicher. Wer ihn bekommt, schließt ihn.
public record Anhanginhalt(string Dateiname, Stream Inhalt);
