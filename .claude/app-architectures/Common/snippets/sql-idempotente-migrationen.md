# Idempotente SQL-Migrationen

> Muster-Katalog für Migrationen, die mehrfach ausführbar sind — ohne Fehler und ohne Seiteneffekte. Formatierung nach Skill `sql-stil` (`~/.claude/skills/sql-stil/SKILL.md`): Fluss-Ausrichtung, explizite Spalten, Schlüsselwörter GROSS. Genutzt von `/erstelle-db-migration`; dort stehen Dateiformat, Header und Schreibgrenzen.

## Problemstellung

Eine Migration läuft nicht genau einmal: Sie wird auf mehreren Umgebungen ausgeführt, nach einem abgebrochenen Deployment wiederholt oder von einem Werkzeug erneut eingespielt, das den Stand nicht kennt. Ein `CREATE TABLE` ohne Prüfung bricht beim zweiten Lauf ab; ein `UPDATE` ohne einschränkendes `WHERE` verändert beim zweiten Lauf Daten, die schon migriert waren.

## Lösungsansatz

Jede Anweisung prüft ihren eigenen Vorzustand:

- **DDL**: Existenz über die Systemkataloge (SQL Server) oder native `IF [NOT] EXISTS`-Klauseln (MariaDB) prüfen; `DROP … IF EXISTS` und `CREATE OR ALTER` sind ab SQL Server 2016 Standard.
- **Daten**: `INSERT` nur nach Existenzprüfung oder als `MERGE` über den fachlichen Schlüssel; `UPDATE` und `DELETE` nur mit einem `WHERE`, das bereits migrierte Zeilen ausschließt.
- **Große Tabellen**: Änderungen in Batches, damit Locks kurz bleiben.
- **NOT NULL nachrüsten**: erst nullable anlegen, dann Daten füllen, dann `NOT NULL` setzen.

Standard-Zielsystem ist **Microsoft SQL Server 2019**; MariaDB nur auf ausdrückliche Angabe.

## SQL Server 2019

### Tabellen

```sql
-- Tabelle anlegen
    IF NOT EXISTS
       (
           SELECT 1
             FROM sys.tables t
            WHERE t.name = 'TabellenName'
              AND t.schema_id = SCHEMA_ID('dbo')
       )
 BEGIN
    CREATE TABLE dbo.TabellenName
           (
               Id   INT IDENTITY(1, 1) NOT NULL,
               Name NVARCHAR(100) NOT NULL,
               CONSTRAINT PK_TabellenName PRIMARY KEY CLUSTERED (Id)
           );
   END;

-- Tabelle entfernen
  DROP TABLE IF EXISTS dbo.TabellenName;
```

### Spalten

```sql
-- Spalte hinzufügen
    IF NOT EXISTS
       (
           SELECT 1
             FROM sys.columns c
            WHERE c.object_id = OBJECT_ID('dbo.TabellenName')
              AND c.name = 'NeueSpalte'
       )
 BEGIN
     ALTER TABLE dbo.TabellenName ADD NeueSpalte INT NULL;
   END;

-- Spalte entfernen
    IF EXISTS
       (
           SELECT 1
             FROM sys.columns c
            WHERE c.object_id = OBJECT_ID('dbo.TabellenName')
              AND c.name = 'AlteSpalte'
       )
 BEGIN
     ALTER TABLE dbo.TabellenName DROP COLUMN AlteSpalte;
   END;

-- Spalte umbenennen — sp_rename meldet eine Warnung, die ignoriert werden kann
    IF EXISTS
       (
           SELECT 1
             FROM sys.columns c
            WHERE c.object_id = OBJECT_ID('dbo.TabellenName')
              AND c.name = 'AlterName'
       )
   AND NOT EXISTS
       (
           SELECT 1
             FROM sys.columns c
            WHERE c.object_id = OBJECT_ID('dbo.TabellenName')
              AND c.name = 'NeuerName'
       )
 BEGIN
      EXEC sp_rename 'dbo.TabellenName.AlterName', 'NeuerName', 'COLUMN';
   END;

-- Datentyp ändern — ohne Prüfung idempotent
 ALTER TABLE dbo.TabellenName ALTER COLUMN Spalte NVARCHAR(200) NOT NULL;
```

### Indizes

```sql
-- Index anlegen (eindeutiger Index: CREATE UNIQUE INDEX UX_TabellenName_Spalte, gleiches Muster)
    IF NOT EXISTS
       (
           SELECT 1
             FROM sys.indexes i
            WHERE i.name = 'IX_TabellenName_Spalte'
              AND i.object_id = OBJECT_ID('dbo.TabellenName')
       )
 BEGIN
    CREATE INDEX IX_TabellenName_Spalte ON dbo.TabellenName (Spalte);
   END;

-- Index entfernen
  DROP INDEX IF EXISTS IX_TabellenName_Spalte ON dbo.TabellenName;

-- Index neu aufbauen — DROP_EXISTING vermeidet doppeltes Sortieren
CREATE INDEX IX_TabellenName_Spalte ON dbo.TabellenName (Spalte) WITH (DROP_EXISTING = ON);
```

### Constraints

```sql
-- Primärschlüssel
    IF NOT EXISTS
       (
           SELECT 1
             FROM sys.key_constraints kc
            WHERE kc.name = 'PK_TabellenName'
              AND kc.parent_object_id = OBJECT_ID('dbo.TabellenName')
       )
 BEGIN
     ALTER TABLE dbo.TabellenName ADD CONSTRAINT PK_TabellenName PRIMARY KEY CLUSTERED (Id);
   END;

-- Fremdschlüssel
    IF NOT EXISTS
       (
           SELECT 1
             FROM sys.foreign_keys fk
            WHERE fk.name = 'FK_TabellenName_AndereTabelle'
       )
 BEGIN
     ALTER TABLE dbo.TabellenName ADD CONSTRAINT FK_TabellenName_AndereTabelle
               FOREIGN KEY (AndereId) REFERENCES dbo.AndereTabelle (Id);
   END;

-- Eindeutigkeit
    IF NOT EXISTS
       (
           SELECT 1
             FROM sys.key_constraints kc
            WHERE kc.name = 'UQ_TabellenName_Spalte'
              AND kc.parent_object_id = OBJECT_ID('dbo.TabellenName')
       )
 BEGIN
     ALTER TABLE dbo.TabellenName ADD CONSTRAINT UQ_TabellenName_Spalte UNIQUE (Spalte);
   END;

-- Vorgabewert
    IF NOT EXISTS
       (
           SELECT 1
             FROM sys.default_constraints dc
            WHERE dc.name = 'DF_TabellenName_Spalte'
       )
 BEGIN
     ALTER TABLE dbo.TabellenName ADD CONSTRAINT DF_TabellenName_Spalte DEFAULT (0) FOR Spalte;
   END;

-- Prüfbedingung
    IF NOT EXISTS
       (
           SELECT 1
             FROM sys.check_constraints cc
            WHERE cc.name = 'CK_TabellenName_Spalte'
       )
 BEGIN
     ALTER TABLE dbo.TabellenName ADD CONSTRAINT CK_TabellenName_Spalte CHECK (Spalte > 0);
   END;

-- Constraint entfernen (Typ 'F' = Fremdschlüssel; 'PK', 'UQ', 'D', 'C' analog)
    IF EXISTS
       (
           SELECT 1
             FROM sys.objects o
            WHERE o.name = 'FK_TabellenName_AndereTabelle'
              AND o.type = 'F'
       )
 BEGIN
     ALTER TABLE dbo.TabellenName DROP CONSTRAINT FK_TabellenName_AndereTabelle;
   END;
```

### Views und Prozeduren

```sql
-- View anlegen oder ersetzen
CREATE OR ALTER VIEW dbo.MeineView
AS
SELECT t.Id, t.Name
  FROM dbo.TabellenName t;
GO

-- Prozedur anlegen oder ersetzen
CREATE OR ALTER PROCEDURE dbo.MeineProzedur
AS
 BEGIN
    SELECT 1 AS Ergebnis;
   END;
GO

-- entfernen
  DROP VIEW IF EXISTS dbo.MeineView;
  DROP PROCEDURE IF EXISTS dbo.MeineProzedur;
```

### Daten

```sql
-- Einfügen nur, wenn nicht vorhanden
    IF NOT EXISTS
       (
           SELECT 1
             FROM dbo.TabellenName t
            WHERE t.Id = 123
       )
 BEGIN
    INSERT INTO dbo.TabellenName (Id, Name)
    VALUES (123, 'Wert');
   END;

-- Upsert über den fachlichen Schlüssel
 MERGE dbo.TabellenName AS ziel
 USING
       (
           SELECT 123 AS Id, 'Wert' AS Name
       ) AS quelle ON quelle.Id = ziel.Id
  WHEN MATCHED
  THEN UPDATE
          SET ziel.Name = quelle.Name
  WHEN NOT MATCHED BY TARGET
  THEN INSERT (Id, Name)
       VALUES (quelle.Id, quelle.Name);

-- Datenmigration: nur nicht migrierte Zeilen
UPDATE dbo.TabellenName
   SET NeueSpalte = 'Wert'
 WHERE NeueSpalte IS NULL;

-- Löschen: idempotent durch das WHERE
DELETE
  FROM dbo.TabellenName
 WHERE Bedingung = 1;
```

### Große Tabellen: Batch-Update

```sql
DECLARE @BatchGroesse INT = 10000;
DECLARE @Betroffene   INT = 1;

 WHILE @Betroffene > 0
 BEGIN
    UPDATE TOP (@BatchGroesse) dbo.TabellenName
       SET NeueSpalte = 'Wert'
     WHERE NeueSpalte IS NULL;

       SET @Betroffene = @@ROWCOUNT;
   END;
```

### Spalte mit NOT NULL nachrüsten

```sql
-- 1. nullable anlegen
    IF NOT EXISTS
       (
           SELECT 1
             FROM sys.columns c
            WHERE c.object_id = OBJECT_ID('dbo.TabellenName')
              AND c.name = 'NeueSpalte'
       )
 BEGIN
     ALTER TABLE dbo.TabellenName ADD NeueSpalte INT NULL;
   END;

-- 2. Daten füllen
UPDATE dbo.TabellenName
   SET NeueSpalte = 0
 WHERE NeueSpalte IS NULL;

-- 3. NOT NULL setzen
 ALTER TABLE dbo.TabellenName ALTER COLUMN NeueSpalte INT NOT NULL;

-- 4. optional: Vorgabewert
    IF NOT EXISTS
       (
           SELECT 1
             FROM sys.default_constraints dc
            WHERE dc.name = 'DF_TabellenName_NeueSpalte'
       )
 BEGIN
     ALTER TABLE dbo.TabellenName ADD CONSTRAINT DF_TabellenName_NeueSpalte DEFAULT (0) FOR NeueSpalte;
   END;
```

## MariaDB

MariaDB kennt native `IF [NOT] EXISTS`-Klauseln für fast alle DDL-Operationen (ab 10.0; `DROP INDEX IF EXISTS` ab 10.1.4). `OR REPLACE` ist eine MariaDB-Erweiterung, nicht MySQL-kompatibel. DDL ist nicht transaktional — ein Fehler lässt sich nicht zurückrollen.

### Tabellen und Spalten

```sql
CREATE TABLE IF NOT EXISTS TabellenName
       (
           Id   INT AUTO_INCREMENT NOT NULL,
           Name VARCHAR(100) NOT NULL,
           PRIMARY KEY (Id)
       ) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4;

  DROP TABLE IF EXISTS TabellenName;

 ALTER TABLE TabellenName ADD COLUMN IF NOT EXISTS NeueSpalte INT NULL;
 ALTER TABLE TabellenName ADD COLUMN IF NOT EXISTS NeueSpalte INT NULL AFTER AndereSpalte;
 ALTER TABLE TabellenName DROP COLUMN IF EXISTS AlteSpalte;

-- Datentyp ändern — idempotent
 ALTER TABLE TabellenName MODIFY COLUMN Spalte VARCHAR(200) NOT NULL;

-- Spalte umbenennen (ab 10.5.2) — kein IF EXISTS, deshalb Prüfung über information_schema
   SET @SpalteVorhanden =
       (
           SELECT COUNT(*)
             FROM information_schema.columns c
            WHERE c.table_schema = DATABASE()
              AND c.table_name = 'TabellenName'
              AND c.column_name = 'AlterName'
       );
   SET @Anweisung = IF(@SpalteVorhanden > 0,
                       'ALTER TABLE TabellenName RENAME COLUMN AlterName TO NeuerName',
                       'SELECT 1');
PREPARE anweisung FROM @Anweisung;
EXECUTE anweisung;
DEALLOCATE PREPARE anweisung;
```

### Indizes und Constraints

```sql
CREATE INDEX IF NOT EXISTS IX_TabellenName_Spalte ON TabellenName (Spalte);
CREATE UNIQUE INDEX IF NOT EXISTS UX_TabellenName_Spalte ON TabellenName (Spalte);
  DROP INDEX IF EXISTS IX_TabellenName_Spalte ON TabellenName;

-- alternativ über ALTER TABLE, oder Index ersetzen
 ALTER TABLE TabellenName DROP INDEX IF EXISTS IX_TabellenName_Spalte;
 ALTER TABLE TabellenName ADD INDEX IF NOT EXISTS IX_TabellenName_Spalte (Spalte);
CREATE OR REPLACE INDEX IX_TabellenName_Spalte ON TabellenName (Spalte);

 ALTER TABLE TabellenName ADD PRIMARY KEY IF NOT EXISTS (Id);
 ALTER TABLE TabellenName ADD CONSTRAINT IF NOT EXISTS FK_TabellenName_AndereTabelle
           FOREIGN KEY (AndereId) REFERENCES AndereTabelle (Id);
 ALTER TABLE TabellenName DROP FOREIGN KEY IF EXISTS FK_TabellenName_AndereTabelle;

-- Eindeutigkeit als Index
CREATE UNIQUE INDEX IF NOT EXISTS UQ_TabellenName_Spalte ON TabellenName (Spalte);
```

### Views und Prozeduren

```sql
CREATE OR REPLACE VIEW MeineView
AS
SELECT t.Id, t.Name
  FROM TabellenName t;

  DROP VIEW IF EXISTS MeineView;

  DROP PROCEDURE IF EXISTS MeineProzedur;
DELIMITER //
CREATE PROCEDURE MeineProzedur()
 BEGIN
    SELECT 1 AS Ergebnis;
   END //
DELIMITER ;
```

### Daten

```sql
-- Duplikat-Fehler ignorieren
INSERT IGNORE INTO TabellenName (Id, Name)
VALUES (123, 'Wert');

-- Upsert
INSERT INTO TabellenName (Id, Name)
VALUES (123, 'Wert')
    ON DUPLICATE KEY UPDATE Name = VALUES(Name);

-- löschen und neu einfügen
REPLACE INTO TabellenName (Id, Name)
VALUES (123, 'Wert');

-- Datenmigration und Löschen: idempotent durch das WHERE
UPDATE TabellenName
   SET NeueSpalte = 'Wert'
 WHERE NeueSpalte IS NULL;

DELETE
  FROM TabellenName
 WHERE Bedingung = 1;
```

### Große Tabellen: Batch-Update

`WHILE … DO` ist in MariaDB nur innerhalb eines Stored Programs erlaubt — deshalb eine Wegwerf-Prozedur:

```sql
  DROP PROCEDURE IF EXISTS BatchUpdate;
DELIMITER //
CREATE PROCEDURE BatchUpdate()
 BEGIN
    DECLARE betroffene INT DEFAULT 1;

     WHILE betroffene > 0 DO
        UPDATE TabellenName
           SET NeueSpalte = 'Wert'
         WHERE NeueSpalte IS NULL
         LIMIT 10000;

           SET betroffene = ROW_COUNT();
       END WHILE;
   END //
DELIMITER ;
  CALL BatchUpdate();
  DROP PROCEDURE IF EXISTS BatchUpdate;
```

### Spalte mit NOT NULL nachrüsten

```sql
 ALTER TABLE TabellenName ADD COLUMN IF NOT EXISTS NeueSpalte INT NULL;

UPDATE TabellenName
   SET NeueSpalte = 0
 WHERE NeueSpalte IS NULL;

 ALTER TABLE TabellenName MODIFY COLUMN NeueSpalte INT NOT NULL DEFAULT 0;
```

## Hinweise

- **SQL Server**: Schema-Präfix (`dbo.`) immer angeben; `sp_rename` erzeugt eine Warnung, die ignoriert werden kann.
- **MariaDB**: DDL ist nicht transaktional; `OR REPLACE` nur in MariaDB, nicht in MySQL.
- **Allgemein**: jede Migration unabhängig und mehrfach ausführbar; Datenmigrationen mit `WHERE`, das Migriertes ausschließt; große Datenmengen in Batches; `NOT NULL` in drei Schritten (nullable → füllen → `NOT NULL`).

## Checkliste

- [ ] Jede DDL-Anweisung prüft ihren Vorzustand (Katalogabfrage oder `IF [NOT] EXISTS`)
- [ ] Jedes `INSERT` hat eine Existenzprüfung oder ist ein `MERGE` über den fachlichen Schlüssel
- [ ] Jedes `UPDATE`/`DELETE` schließt bereits migrierte Zeilen über das `WHERE` aus
- [ ] Große Tabellen: Batch-Verarbeitung
- [ ] Neue `NOT NULL`-Spalten in drei Schritten
- [ ] Formatierung nach `sql-stil` (Selbstkontrolle des Skills durchlaufen)
- [ ] Zweimal hintereinander ausführbar ohne Fehler — gedanklich durchgespielt
