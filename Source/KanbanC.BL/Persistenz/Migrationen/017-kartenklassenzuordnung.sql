-- Die Zuordnung einer Karte zu einer Kartenklasse — und damit die vergebene Nummer.
-- Eine eigene Tabelle und kein ALTER TABLE Karte ADD COLUMN: der Migrationslaeufer kennt kein
-- Journal und führt jedes Skript bei jedem Start aus; ein ALTER TABLE scheiterte im zweiten
-- Lauf, und die bestehende CREATE TABLE IF NOT EXISTS aus 003 wächst nicht nachträglich um
-- eine Spalte.
-- Abgelegt wird der **Zaehlerstand**, nicht die fertige Nummer: gebildet wird sie überall aus
-- Kartennummer.Aus(Praefix, Zaehlerstand), und eine zweite abgelegte Schreibweise derselben
-- Regel liefe beim ersten Sonderfall auseinander.
-- Die Nummer gehört der **Zuordnung**, nicht der Karte: ein Wechsel löscht die Zeile und legt
-- eine neue mit dem nächsten Stand an, die alte Nummer verfällt und wird nie wieder vergeben,
-- weil der Zaehlerstand der Kartenklasse nur wächst.
-- UX_Kartenklassenzuordnung_Karte trägt „höchstens eine Kartenklasse je Karte“ ins Schema; er
-- ist zugleich der Index, über den jeder Leseweg dieser Tabelle geht — ein zweiter, nicht
-- eindeutiger Index auf derselben Spalte wäre dieselbe Auskunft doppelt.
-- UX_Kartenklassenzuordnung_Kartenklasse_Zaehlerstand ist das Netz unter der Zusage, dass eine
-- Kartennummer sich nie wiederholt: dasselbe Verhältnis wie bei UX_Kartenklasse_Board_Praefix
-- in 016 — der lesbare Befund entsteht davor im Dienst, der Index fängt jeden Weg ab, der am
-- Dienst vorbeischreibt, auch zwei gleichzeitige Schreiber.
CREATE TABLE IF NOT EXISTS Kartenklassenzuordnung
(
    KartenklassenzuordnungId INTEGER PRIMARY KEY AUTOINCREMENT,
    Karte                    INTEGER NOT NULL REFERENCES Karte (KarteId),
    Kartenklasse             INTEGER NOT NULL REFERENCES Kartenklasse (KartenklasseId),
    Zaehlerstand             INTEGER NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Kartenklassenzuordnung_Karte ON Kartenklassenzuordnung (Karte);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Kartenklassenzuordnung_Kartenklasse_Zaehlerstand
    ON Kartenklassenzuordnung (Kartenklasse, Zaehlerstand);
