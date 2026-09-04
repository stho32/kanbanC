-- Die Stilllegung eines Kontributors: die Zeile selbst ist die Aussage — vorhanden heißt
-- stillgelegt. Ein eigener Fremdschlüssel als Primärschlüssel, damit derselbe Kontributor nicht
-- zweimal abgelegt werden kann. Anders als bei Boardarchivierung trägt die Zeile ein Datum: die
-- Liste zeigt „stillgelegt seit <Datum>“, und ein Datum, das nur die Oberfläche erfände, wäre
-- keins. Bestehende Kontributoren bekommen keine Zeile und gelten damit als aktiv.
CREATE TABLE IF NOT EXISTS Kontributorstilllegung
(
    Kontributor   INTEGER PRIMARY KEY REFERENCES Kontributor (KontributorId),
    StillgelegtAm TEXT    NOT NULL
);
