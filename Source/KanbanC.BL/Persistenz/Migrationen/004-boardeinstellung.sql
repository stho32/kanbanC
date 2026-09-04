-- Die Einstellungen eines Boards: eine Zeile je Board, angelegt erst beim ersten Umschalten.
-- Fehlt die Zeile, gilt die Voreinstellung — bestehende Boards ändern ihr Aussehen nicht von
-- selbst. Der Fremdschlüssel ist zugleich der Schlüssel: zwei Einstellungszeilen für dasselbe
-- Board gäbe es sonst.
CREATE TABLE IF NOT EXISTS Boardeinstellung
(
    Board           INTEGER PRIMARY KEY REFERENCES Board (BoardId),
    ZeigtKartenzahl INTEGER NOT NULL
);
