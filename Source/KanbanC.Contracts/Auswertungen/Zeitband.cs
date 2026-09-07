namespace KanbanC.Contracts.Auswertungen;

// Eine Zeitspanne mit Unter- und Obergrenze in Stunden — so, wie die WBS-Datei ihre Aufwände
// führt: `0,4` ergibt 0,4–0,4, `2-4` ergibt 2,0–4,0.
// **Ein Band und keine Zahl**, weil eine Schätzung, die die Datei als Spanne führt, auf dem Weg
// zum Leser nicht genauer werden darf, als sie ist. Aus derselben Regel folgt, dass die Summe
// über mehrere Bänder selbst ein Band ist.
// Stunden stehen als decimal: Zehntelstunden summieren sich damit ohne Rundungsrest.
public record Zeitband(decimal VonStunden, decimal BisStunden);
