namespace KanbanC.Contracts.Karten;

// Ein Schritt der Karte: ein kurzer Text mit eigener Nummer, der einzeln abgehakt wird. Die
// Nummer überlebt das Abhaken und ein späteres Umbenennen — sie ist das Einzige, was den
// Abhakstand an derselben Zeile festhält.
// Position hält die Anzeigereihenfolge fest; ohne sie bestimmte die Datenbank sie, und zwei
// Abrufe zeigten dieselbe Karte verschieden.
// Abgehakt ist ein Ja/Nein ohne Zeitpunkt: das Artboard zeigt keins, und die Karte hat mit
// ErledigtAm schon einen Zeitpunkt für den einen Ort, an dem er gebraucht wird.
public record Teilaufgabe(long TeilaufgabeId, string Text, int Position, bool Abgehakt);
