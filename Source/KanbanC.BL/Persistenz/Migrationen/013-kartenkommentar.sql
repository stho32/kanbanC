-- Ein Kommentar ist eine Äußerung an der Karte: ein Fließtext mit Urheber und Zeitpunkt. Wie bei
-- der Teilaufgabe (012) und anders als beim Etikett (011) trägt jede Zeile eine eigene Identität —
-- zwei gleichlautende Kommentare sind zwei Äußerungen, und Karte und Text zusammen als Schlüssel
-- machten die zweite unmöglich.
-- **Keine Position**, anders als in 012: hier ordnet der Zeitpunkt. Eine Position daneben wäre eine
-- zweite Wahrheit über dieselbe Reihenfolge und liefe beim ersten Zurückdatieren auseinander.
-- Der eigene Index auf Karte aus demselben Grund wie dort: der Primärschlüssel führt mit
-- KommentarId, seine führende Spalte ist also nicht Karte, und alle Lesewege dieser Tabelle fragen
-- nach der Karte.
-- Der Fremdschlüssel heißt Kontributor nach der referenzierten Tabelle (Projektregel); der
-- fachliche Begriff **Urheber** lebt in DTO, Beschriftung und Kriterium weiter — dieselbe Trennung
-- wie bei Karteneigenschaft.Kontributor gegen den Verantwortlichen.
-- NOT NULL auf Kontributor und Zeitpunkt: ein Kommentar ohne Urheber oder ohne Zeitpunkt ist nach
-- dem Fertig-Kriterium keiner, und die Vision verlangt, dass an jeder Karte ablesbar ist, wer
-- gehandelt hat.
-- Zeitpunkt ist TEXT und trägt ISO-8601 in **UTC**: nur bei einheitlichem Zeitzonenversatz sortiert
-- Text lexikografisch wie chronologisch, und ORDER BY Zeitpunkt wäre sonst eine stille Lüge
-- (belegt in SqliteEigenschaftenTests).
-- Eigene Tabelle statt ALTER TABLE Karte ADD COLUMN: eine Karte trägt n Kommentare — und der
-- Migrationslaeufer führt jedes Skript bei jedem Start aus, ohne Journal.
CREATE TABLE IF NOT EXISTS Kommentar
(
    KommentarId INTEGER PRIMARY KEY AUTOINCREMENT,
    Karte       INTEGER NOT NULL REFERENCES Karte (KarteId),
    Kontributor INTEGER NOT NULL REFERENCES Kontributor (KontributorId),
    Text        TEXT    NOT NULL,
    Zeitpunkt   TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Kommentar_Karte ON Kommentar (Karte);
