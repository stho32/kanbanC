-- Ein Zeiteintrag ist die erfasste Arbeitszeit eines Kontributors an einer Karte. Wie beim
-- Kommentar (013) und beim Anhang (014) trägt jede Zeile eine eigene Identität: zwei Messungen
-- desselben Menschen an derselben Karte sind zwei Vorgänge.
-- **Ein laufender Timer ist genau der Eintrag ohne Ende.** Deshalb steht Ende von Anfang an in
-- der Tabelle, obwohl erst I0024 sie füllt: der Migrationslaeufer kennt kein Journal und führt
-- jedes Skript bei jedem Start aus — ein nachträgliches ALTER TABLE ADD COLUMN scheiterte im
-- zweiten Lauf, und die bestehende CREATE TABLE IF NOT EXISTS wächst nicht.
-- Eigene Tabelle statt ALTER TABLE Karte ADD COLUMN: eine Karte trägt n Zeiteinträge.
-- Die Fremdschlüssel heißen Karte und Kontributor nach den referenzierten Tabellen
-- (Projektregel), nie <Tabelle>Nummer.
-- Beginn und Ende sind TEXT und tragen ISO-8601 in **UTC**: nur bei einheitlichem
-- Zeitzonenversatz sortiert Text lexikografisch wie chronologisch, und ORDER BY Beginn wäre sonst
-- eine stille Lüge (belegt in SqliteEigenschaftenTests).
-- Der eigene Index auf Karte aus demselben Grund wie in 013: der Primärschlüssel führt mit
-- ZeiteintragId, seine führende Spalte ist also nicht Karte, und die Lesewege dieser Tabelle
-- fragen nach der Karte.
-- UX_Zeiteintrag_Karte_Kontributor_Laufend trägt die einzige Obergrenze dieses Gegenstands ins
-- Schema: ein Kontributor darf auf mehreren Karten zugleich messen, auf **derselben** Karte aber
-- nur einmal — zwei offene Einträge desselben Paares wären zwei Antworten auf eine Frage („seit
-- wann arbeitet er hier?") und zählten dieselbe Uhrzeit doppelt. Der Index ist **partiell**
-- (WHERE Ende IS NULL), weil die Schranke nur für laufende Einträge gilt: abgeschlossene
-- Messungen desselben Paares gibt es beliebig viele. Dasselbe Verhältnis wie bei
-- UX_Kartenklassenzuordnung_Kartenklasse_Zaehlerstand in 017 — der lesbare Befund entsteht davor
-- im Dienst, der Index fängt jeden Weg ab, der am Dienst vorbeischreibt.
CREATE TABLE IF NOT EXISTS Zeiteintrag
(
    ZeiteintragId INTEGER PRIMARY KEY AUTOINCREMENT,
    Karte         INTEGER NOT NULL REFERENCES Karte (KarteId),
    Kontributor   INTEGER NOT NULL REFERENCES Kontributor (KontributorId),
    Beginn        TEXT    NOT NULL,
    Ende          TEXT    NULL
);

CREATE INDEX IF NOT EXISTS IX_Zeiteintrag_Karte ON Zeiteintrag (Karte);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Zeiteintrag_Karte_Kontributor_Laufend
    ON Zeiteintrag (Karte, Kontributor)
 WHERE Ende IS NULL;
