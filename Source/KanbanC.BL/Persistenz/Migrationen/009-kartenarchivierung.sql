-- Das Archiv einer Karte: die Zeile selbst ist die Aussage — vorhanden heißt archiviert. Der
-- Fremdschlüssel auf Karte ist zugleich der Schlüssel, sonst trüge eine Karte zwei Archivstände.
-- Kein ArchiviertAm: das Artboard zeichnet kein Datum, und ein erfundenes verdürbe die Auswertung.
-- Bestehende Karten bekommen keine Zeile und gelten damit alle als aktiv.
CREATE TABLE IF NOT EXISTS Kartenarchivierung
(
    Karte INTEGER PRIMARY KEY REFERENCES Karte (KarteId)
);
