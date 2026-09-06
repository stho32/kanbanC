-- Eine Kartenklasse ist ein benannter Nummernkreis eines Boards: WBS, Bugmeldungen, Beschaffung.
-- Aus ihr bekommt eine Karte später ihre eigene, klassenspezifische Nummer (WBS-32, BUG-08).
-- Die Klasse gehört **einem** Board (Fremdschlüssel Board nach der referenzierten Tabelle,
-- Projektregel); ihr Präfix ist nur innerhalb dieses Boards eindeutig — zwei Projektboards dürfen
-- beide WBS- führen, und das ist der Normalfall.
-- Der Zaehlerstand wird **geführt, nicht gerechnet**: er beginnt bei 0 und wächst nur. Eine aus
-- den zugeordneten Karten gerechnete Zahl fiele zurück, sobald eine Karte die Klasse verlässt —
-- die nächste Zuordnung bekäme eine Nummer, die es schon gab, und eine Kartennummer ist eine
-- Identität. Die Spalte steht deshalb schon hier und nicht erst in der Migration des Zuordnens:
-- ALTER TABLE ADD COLUMN scheitert im zweiten Lauf, und eine bestehende CREATE TABLE IF NOT
-- EXISTS wächst nicht nachträglich um eine Spalte.
-- Der eigene Index auf Board aus demselben Grund wie in 012 bis 015: der Primärschlüssel führt
-- mit KartenklasseId, und jeder Leseweg dieser Tabelle fragt nach dem Board.
-- Der eindeutige Index mit COLLATE NOCASE folgt UX_Spalte_Board_Bezeichnung aus 002: wbs- und
-- WBS- auf einem Board erzeugten Nummern, die gleich aussehen und es nicht sind. Er sichert die
-- Regel gegen jeden Weg, der am Dienst vorbeischreibt; der lesbare Befund entsteht trotzdem davor
-- im Validator, damit der Aufrufer nie auf eine nackte Datenbankmeldung trifft.
-- **Keine Längengrenze in der Spalte**: die Grenze gehört in den Validator, wo sie einen lesbaren
-- Befund mit Kompensation liefert, statt als Datenbankmeldung anzukommen.
CREATE TABLE IF NOT EXISTS Kartenklasse
(
    KartenklasseId INTEGER PRIMARY KEY AUTOINCREMENT,
    Board          INTEGER NOT NULL REFERENCES Board (BoardId),
    Name           TEXT    NOT NULL,
    Praefix        TEXT    NOT NULL,
    Zaehlerstand   INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS IX_Kartenklasse_Board ON Kartenklasse (Board);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Kartenklasse_Board_Praefix ON Kartenklasse (Board, Praefix COLLATE NOCASE);
