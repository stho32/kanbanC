-- Ein Anhang ist eine an der Karte liegende Datei: Name, Größe, Urheber und Zeitpunkt. Wie beim
-- Kommentar (013) trägt jede Zeile eine eigene Identität — zwei Dateien gleichen Namens an
-- derselben Karte sind zwei Dateien, und Karte und Dateiname zusammen als Schlüssel machten die
-- zweite unmöglich.
-- **Keine Position**, wie in 013: hier ordnet der Zeitpunkt. Eine Position daneben wäre eine
-- zweite Wahrheit über dieselbe Reihenfolge.
-- **Keine BLOB-Spalte und keine Pfadspalte**: die Bytes liegen als gewöhnliche Datei neben der
-- Datenbank, damit sie auch ohne die Anwendung anfassbar bleiben, und ihr Ort wird aus der
-- Verbindungszeichenfolge gerechnet. Ein gespeicherter Pfad liefe beim ersten Verschieben der
-- Datenbankdatei auseinander.
-- Die AnhangId ist zugleich der Name der Datei auf der Platte. Deshalb ist sie mehr als ein
-- Schlüssel: der Originalname lebt nur in Dateiname, und Nutzereingabe berührt die Platte nie.
-- Der eigene Index auf Karte aus demselben Grund wie in 012 und 013: der Primärschlüssel führt mit
-- AnhangId, seine führende Spalte ist also nicht Karte, und alle Lesewege fragen nach der Karte.
-- Die Fremdschlüssel heißen Karte und Kontributor nach den referenzierten Tabellen (Projektregel);
-- der fachliche Begriff **Urheber** lebt in DTO, Beschriftung und Kriterium weiter.
-- NOT NULL auf Kontributor und Zeitpunkt: ein Anhang ohne Urheber ist nach dem Fertig-Kriterium
-- keiner, und die Vision verlangt, dass an jeder Karte ablesbar ist, wer gehandelt hat.
-- Zeitpunkt ist TEXT und trägt ISO-8601 in **UTC**, wie in 013: nur bei einheitlichem
-- Zeitzonenversatz sortiert Text lexikografisch wie chronologisch.
-- Dateigroesse ist die **geschriebene** Länge in Bytes, keine gemeldete: eine gemeldete Zahl wäre
-- vom Aufrufer beliebig setzbar.
-- Eigene Tabelle statt ALTER TABLE Karte ADD COLUMN: eine Karte trägt n Anhänge — und der
-- Migrationslaeufer führt jedes Skript bei jedem Start aus, ohne Journal.
CREATE TABLE IF NOT EXISTS Anhang
(
    AnhangId     INTEGER PRIMARY KEY AUTOINCREMENT,
    Karte        INTEGER NOT NULL REFERENCES Karte (KarteId),
    Kontributor  INTEGER NOT NULL REFERENCES Kontributor (KontributorId),
    Dateiname    TEXT    NOT NULL,
    Dateigroesse INTEGER NOT NULL,
    Zeitpunkt    TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Anhang_Karte ON Anhang (Karte);
