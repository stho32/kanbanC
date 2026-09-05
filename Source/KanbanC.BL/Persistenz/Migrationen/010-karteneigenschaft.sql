-- Die Eigenschaften einer Karte: eine Zeile je Karte, angelegt erst beim ersten Ändern. Fehlt
-- die Zeile, gilt die Voreinstellung — bestehende Karten lesen sich als „ohne Beschreibung,
-- ohne Fälligkeit, Farbe ohne, niemand verantwortlich" und ändern sich nicht von selbst.
-- Eine Tabelle für vier Werte, Muster Boardeinstellung (004): sie werden immer im selben
-- Formular geändert und immer zusammen gelesen. Der Fremdschlüssel ist zugleich der Schlüssel,
-- sonst trüge eine Karte zwei Eigenschaftszeilen.
-- Die Spalte Kontributor entsteht hier mit und wird erst mit dem Verantwortlichen gefüllt —
-- der ausdrückliche Preis der einen Tabelle: in SQLite wächst eine bestehende Tabelle über
-- CREATE TABLE IF NOT EXISTS nicht nachträglich um eine Spalte.
-- FaelligAm steht als ISO-Text (YYYY-MM-DD) wie ErledigtAm und die Boardtermine.
CREATE TABLE IF NOT EXISTS Karteneigenschaft
(
    Karte        INTEGER PRIMARY KEY REFERENCES Karte (KarteId),
    Beschreibung TEXT    NULL,
    Kontributor  INTEGER NULL REFERENCES Kontributor (KontributorId),
    FaelligAm    TEXT    NULL,
    Farbe        TEXT    NOT NULL
);
