-- Der Tag, an dem eine Karte erledigt wurde: die Zeile entsteht mit dem Eintritt in die
-- Abschlussspalte und verschwindet mit dem Austritt. Der Fremdschlüssel ist zugleich der
-- Schlüssel, sonst trüge eine Karte zwei Erledigungsdaten. Karten, die schon vor dieser
-- Migration in einer Abschlussspalte lagen, bekommen keine Zeile: ein Datum, das die Migration
-- erfände, wäre keins und verdürbe die Auswertung, wegen der die Tabelle entsteht.
CREATE TABLE IF NOT EXISTS Karteerledigung
(
    Karte      INTEGER PRIMARY KEY REFERENCES Karte (KarteId),
    ErledigtAm TEXT    NOT NULL
);
