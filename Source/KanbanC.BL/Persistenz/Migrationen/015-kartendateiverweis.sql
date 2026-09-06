-- Ein Dateiverweis ist ein an der Karte hinterlegter Pfad auf eine Datei, die **nicht** in der
-- Anwendung liegt: eine Anforderung, eine WBS-Zeile, ein Architekturdokument im Repository
-- daneben. Der Unterschied zum Anhang (014) ist der ganze Zweck — der Anhang bringt eine Kopie
-- mit, der Dateiverweis zeigt auf die Datei, die dort weiterlebt, wo sie hingehört.
-- Wie bei Kommentar (013) und Anhang (014) trägt jede Zeile eine eigene Identität.
-- **Keine Position**, wie in 013 und 014: hier ordnet der Zeitpunkt. Eine Position daneben wäre
-- eine zweite Wahrheit über dieselbe Reihenfolge.
-- Der eigene Index auf Karte aus demselben Grund wie in 012 bis 014: der Primärschlüssel führt
-- mit DateiverweisId, seine führende Spalte ist also nicht Karte, und alle Lesewege dieser
-- Tabelle fragen nach der Karte.
-- **Anders als 014 ein eindeutiger Index auf (Karte, Pfad)** — die zweite Stelle im ganzen
-- Schema, an der eine Dublette ausgeschlossen wird (die erste ist 002). Beim Anhang waren zwei
-- gleichnamige Dateien **zwei Dinge**; hier ist derselbe Pfad an derselben Karte **dasselbe
-- Ding**: zwei Zeilen zeigen auf dieselbe Datei, und die zweite trägt keine Aussage.
-- **Ohne COLLATE NOCASE**, anders als der eindeutige Index in 002: Groß- und Kleinschreibung
-- unterscheidet Pfade. Auf der Zielplattform der Vision sind README.md und readme.md zwei
-- Dateien, und ein Vergleich, der das einebnet, wäre eine Annahme über fremde Dateisysteme.
-- Der Index sichert die Regel gegen jeden Weg, der am Dienst vorbeischreibt. Der lesbare Befund
-- entsteht trotzdem davor im Repository — ein Aufrufer trifft nie auf eine nackte
-- Datenbankmeldung über einen verletzten Index (Fehlervertrag aus R00007).
-- Die Fremdschlüssel heißen Karte und Kontributor nach den referenzierten Tabellen
-- (Projektregel); der fachliche Begriff **Urheber** lebt in DTO, Beschriftung und Kriterium
-- weiter.
-- NOT NULL auf Kontributor und Zeitpunkt: ein Dateiverweis ohne Urheber ist nach dem
-- Fertig-Kriterium keiner, und die Vision verlangt, dass an jeder Karte ablesbar ist, wer
-- gehandelt hat.
-- Zeitpunkt ist TEXT und trägt ISO-8601 in **UTC**, wie in 013 und 014: nur bei einheitlichem
-- Zeitzonenversatz sortiert Text lexikografisch wie chronologisch.
-- Der Pfad steht so da, wie er eingetragen wurde — bis auf die Ränder. **Keine Längengrenze in
-- der Spalte**: die Regel gehört in den Validator, wo sie einen lesbaren Befund mit
-- Kompensation liefert, statt als Datenbankmeldung anzukommen.
-- Eigene Tabelle statt ALTER TABLE Karte ADD COLUMN: eine Karte trägt n Dateiverweise — und der
-- Migrationslaeufer führt jedes Skript bei jedem Start aus, ohne Journal.
CREATE TABLE IF NOT EXISTS Dateiverweis
(
    DateiverweisId INTEGER PRIMARY KEY AUTOINCREMENT,
    Karte          INTEGER NOT NULL REFERENCES Karte (KarteId),
    Kontributor    INTEGER NOT NULL REFERENCES Kontributor (KontributorId),
    Pfad           TEXT    NOT NULL,
    Zeitpunkt      TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Dateiverweis_Karte ON Dateiverweis (Karte);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Dateiverweis_Karte_Pfad ON Dateiverweis (Karte, Pfad);
