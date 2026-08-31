-- Eine Karte ist eine Aufgabe in genau einer Spalte. Sie trägt in dieser Ausbaustufe ihren Titel
-- und ihre Position innerhalb der Spalte; jedes weitere Feld (Beschreibung, Klasse, Zeiten) kommt
-- mit der Interaction, die es braucht, über eine eigene Migration.
CREATE TABLE IF NOT EXISTS Karte
(
    KarteId  INTEGER PRIMARY KEY AUTOINCREMENT,
    Spalte   INTEGER NOT NULL REFERENCES Spalte (SpalteId),
    Titel    TEXT    NOT NULL,
    Position INTEGER NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Karte_Spalte ON Karte (Spalte);
