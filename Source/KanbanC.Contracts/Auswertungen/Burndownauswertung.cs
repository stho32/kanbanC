namespace KanbanC.Contracts.Auswertungen;

// Der gerechnete Restumfang eines Kartenbestands über die Zeit: je Kalendertag eine Zeile,
// darüber die Kopfzahlen. Kurve, Tagestabelle und Kopfzahlen lesen **dieselbe** Reihe.
// Ein Bestand ohne Karten liefert den einen Tag heute mit `0` — das ist eine Antwort und kein
// Fehler.
public record Burndownauswertung(IReadOnlyList<Burndowntag> Tage, Burndownkopfzahlen Kopfzahlen); // stil-check: C09 wie SollIstAuswertung.Zeilen
