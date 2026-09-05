-- Eine Teilaufgabe ist ein Schritt der Karte: ein kurzer Text, der einzeln abgehakt wird. Anders
-- als beim Etikett (011) bilden Karte und Text hier **nicht** den Schlüssel — eine Teilaufgabe hat
-- eine Identität, die das Abhaken überlebt. Mit dem Text als Schlüsselbestandteil verlöre ein
-- späteres Umbenennen den Abhakstand, und zwei gleichlautende Teilaufgaben wären unmöglich, obwohl
-- zwei gleich benannte Arbeiten zwei Arbeiten sind.
-- Deshalb eine eigene TeilaufgabeId, wie sie Board, Spalte, Karte und Kontributor führen — und
-- deshalb auch ein eigener Index auf Karte: der Primärschlüssel führt mit TeilaufgabeId, seine
-- führende Spalte ist also nicht Karte, und alle Lesewege dieser Tabelle fragen nach der Karte.
-- Bei Etikett entfiel der Index genau aus dem umgekehrten Grund.
-- Position hält die Anzeigereihenfolge fest, damit nicht die Datenbank sie bestimmt und zwei
-- Abrufe dieselbe Karte verschieden zeigen. Angehängt wird als höchste + 1; verdichtet wird nicht,
-- weil dieser Slice weder Löschen noch Umsortieren kennt und damit keine Lücke entsteht.
-- Abgehakt ist ein Ja/Nein ohne Zeitpunkt: das Artboard zeigt keins, die Karte hat mit ErledigtAm
-- schon einen Zeitpunkt für den einen Ort, an dem er gebraucht wird, und ein Datum, das niemand
-- liest, wäre tote Flexibilität. Der Vorgabewert trägt die frisch angelegte Zeile.
-- Eigene Tabelle statt ALTER TABLE Karte ADD COLUMN: eine Karte trägt n Teilaufgaben, eine Spalte
-- trägt eine — und der Migrationslaeufer führt jedes Skript bei jedem Start aus, ohne Journal.
CREATE TABLE IF NOT EXISTS Teilaufgabe
(
    TeilaufgabeId INTEGER PRIMARY KEY AUTOINCREMENT,
    Karte         INTEGER NOT NULL REFERENCES Karte (KarteId),
    Text          TEXT    NOT NULL,
    Position      INTEGER NOT NULL,
    Abgehakt      INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS IX_Teilaufgabe_Karte ON Teilaufgabe (Karte);
