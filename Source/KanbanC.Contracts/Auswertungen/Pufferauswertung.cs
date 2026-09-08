namespace KanbanC.Contracts.Auswertungen;

// Der gerechnete Pufferstand eines Kartenbestands: je Karte eine Zeile in Kartennummernfolge,
// darüber die Kopfzahlen. Kopfzahlen, Kurve und Tabelle lesen **dieselbe** Auswertung; keine von
// ihnen rechnet nach.
// Ein Bestand ohne Karten liefert die leere Zeilenliste und Kopfzahlen ohne Werte — das ist eine
// Antwort und kein Fehler.
public record Pufferauswertung(IReadOnlyList<Pufferzeile> Zeilen, Pufferkopfzahlen Kopfzahlen); // stil-check: C09 wie SollIstAuswertung.Zeilen
