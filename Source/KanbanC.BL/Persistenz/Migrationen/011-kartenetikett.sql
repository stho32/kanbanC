-- Ein Etikett ist eine freie Textmarke an einer Karte, kein verwalteter Satz je Board: es
-- entsteht mit der ersten Karte, die es trägt, und verschwindet mit der letzten. Deshalb gibt es
-- keine Etikettentabelle je Board — es gäbe nichts zu pflegen.
-- Karte und Text bilden zusammen den Schlüssel: dasselbe Etikett zweimal an derselben Karte ist
-- damit unmöglich, ohne dass jemand prüft. Ein zusätzlicher Index auf Karte entfällt — in SQLite
-- legt dieser Primärschlüssel bereits einen Index an, dessen führende Spalte Karte ist; ein
-- zweiter auf derselben Spalte wäre Dublette.
CREATE TABLE IF NOT EXISTS Etikett
(
    Karte INTEGER NOT NULL REFERENCES Karte (KarteId),
    Text  TEXT    NOT NULL,
    PRIMARY KEY (Karte, Text)
);
