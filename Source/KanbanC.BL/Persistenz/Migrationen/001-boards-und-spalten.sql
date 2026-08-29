-- Ein Board mit seinen Spalten. Termine als ISO-Text (YYYY-MM-DD), Art als Text (Linie | Projekt).
CREATE TABLE IF NOT EXISTS Board
(
    BoardId     INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT    NOT NULL,
    Art         TEXT    NOT NULL,
    Starttermin TEXT    NULL,
    Zieltermin  TEXT    NULL
);

CREATE TABLE IF NOT EXISTS Spalte
(
    SpalteId           INTEGER PRIMARY KEY AUTOINCREMENT,
    Board              INTEGER NOT NULL REFERENCES Board (BoardId),
    Bezeichnung        TEXT    NOT NULL,
    Position           INTEGER NOT NULL,
    IstAbschlussspalte INTEGER NOT NULL,
    Anzeigegrenze      INTEGER NULL
);

CREATE INDEX IF NOT EXISTS IX_Spalte_Board ON Spalte (Board);
