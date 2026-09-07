namespace KanbanC.Contracts.Auswertungen;

// Der gerechnete Soll-Ist-Vergleich eines Kartenbestands: je Karte eine Zeile in
// Kartennummernfolge, darunter die Summe.
// Ein Bestand ohne Karten liefert die leere Zeilenliste und eine Summe ohne Band — das ist eine
// Antwort und kein Fehler.
public record SollIstAuswertung(IReadOnlyList<SollIstZeile> Zeilen, SollIstSumme Summe); // stil-check: C09 wie Importbericht.Zeilen
