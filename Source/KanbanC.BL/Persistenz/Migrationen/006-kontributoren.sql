-- Ein Kontributor: Mensch, Agent oder abgebildete Person. Die Art steht als Text in der Zeile,
-- wie die Art eines Boards. Kein UNIQUE auf Name — zwei Menschen dürfen gleich heißen, und
-- unterschieden werden sie über die KontributorId.
CREATE TABLE IF NOT EXISTS Kontributor
(
    KontributorId  INTEGER PRIMARY KEY AUTOINCREMENT,
    Name           TEXT    NOT NULL,
    Kontributorart TEXT    NOT NULL
);
