-- Das Sollband einer Karte: die Aufwandsspanne ihres WBS-Teilbaums in Stunden.
-- Eine eigene Tabelle und kein ALTER TABLE Karte ADD COLUMN: der Migrationslaeufer kennt kein
-- Journal und führt jedes Skript bei jedem Start aus; ein ALTER TABLE scheiterte im zweiten
-- Lauf, und die bestehende CREATE TABLE IF NOT EXISTS aus 003 wächst nicht nachträglich um
-- eine Spalte. Muster: Boardeinstellung (004), Karteneigenschaft (010),
-- Kartenklassenzuordnung (017).
-- Der Fremdschlüssel ist zugleich der Schlüssel: eine Karte trägt höchstens ein Sollband.
-- **Fehlt die Zeile, gibt es kein Band** — das ist die Darstellung von „ohne Soll". Deshalb sind
-- beide Spalten NOT NULL: ein halbes Band gibt es nicht. Ein Einzelwert der Datei (0,4) setzt
-- beide gleich, eine Spanne (2-4) trägt Unter- und Obergrenze getrennt; eine Reduktion auf eine
-- Zahl wäre ein Verlust, den kein späterer Lauf zurücknimmt.
-- Der einzige Erzeuger ist der WBS-Import; von Hand gesetzt wird die Sollzeit nirgends.
CREATE TABLE IF NOT EXISTS Kartensollzeit
(
    Karte              INTEGER PRIMARY KEY REFERENCES Karte (KarteId),
    SollzeitVonStunden REAL    NOT NULL,
    SollzeitBisStunden REAL    NOT NULL
);
