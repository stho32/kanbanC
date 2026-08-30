-- Die Bezeichnung einer Spalte wird je Board eindeutig, ohne Rücksicht auf Groß-/Kleinschreibung
-- und umschließende Leerzeichen. Vorhandene Duplikate werden vorher deterministisch umbenannt:
-- die Spalte mit der kleinsten SpalteId behält ihren Namen, jede weitere bekommt eine laufende
-- Zahl in der Reihenfolge ihrer SpalteId. Der Lauf liefert dadurch wiederholbar dasselbe Ergebnis.
UPDATE Spalte
   SET Bezeichnung = TRIM(Bezeichnung)
 WHERE Bezeichnung <> TRIM(Bezeichnung);

UPDATE Spalte
   SET Bezeichnung = Spalte.Bezeichnung || ' (' || dubletten.Rang || ')'
  FROM (
           SELECT SpalteId,
                  ROW_NUMBER() OVER (PARTITION BY Board, Bezeichnung COLLATE NOCASE ORDER BY SpalteId) AS Rang
             FROM Spalte
       ) dubletten
 WHERE dubletten.SpalteId = Spalte.SpalteId
   AND dubletten.Rang > 1;

-- Zweiter Durchgang für den Fall, dass die angehängte Zahl selbst auf einen vorhandenen Namen
-- trifft — etwa bei „Erledigt", „Erledigt", „Erledigt (2)". Die SpalteId ist eindeutig und
-- beendet die Kette.
UPDATE Spalte
   SET Bezeichnung = Spalte.Bezeichnung || ' (#' || Spalte.SpalteId || ')'
  FROM (
           SELECT SpalteId,
                  ROW_NUMBER() OVER (PARTITION BY Board, Bezeichnung COLLATE NOCASE ORDER BY SpalteId) AS Rang
             FROM Spalte
       ) dubletten
 WHERE dubletten.SpalteId = Spalte.SpalteId
   AND dubletten.Rang > 1;

CREATE UNIQUE INDEX IF NOT EXISTS UX_Spalte_Board_Bezeichnung ON Spalte (Board, Bezeichnung COLLATE NOCASE);
