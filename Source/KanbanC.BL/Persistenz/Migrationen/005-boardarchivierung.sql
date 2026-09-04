-- Das Archiv eines Boards: die Zeile selbst ist die Aussage — vorhanden heißt archiviert.
-- Ein eigener Fremdschlüssel als Primärschlüssel, damit dasselbe Board nicht zweimal abgelegt
-- werden kann. Bestehende Boards bekommen keine Zeile und gelten damit als aktiv.
CREATE TABLE IF NOT EXISTS Boardarchivierung
(
    Board INTEGER PRIMARY KEY REFERENCES Board (BoardId)
);
