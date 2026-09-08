namespace KanbanC.Contracts.Boardimport;

// Was die Datei bräche, gezählt: zehn Zahlen, die in **jeder** Antwort stehen und nicht nur bei
// trocken=true. Wer Vorschau und Bericht des geschriebenen Laufs nebeneinanderlegt, sieht daran,
// dass unterwegs nichts verlorenging — und ein Agent bekommt ohne zweiten Aufruf, was ein Mensch
// im Schirm sieht.
public record Boardimportzahlen(
    int Spalten,
    int Kartenklassen,
    int Kontributoren,
    int Karten,
    int Etiketten,
    int Teilaufgaben,
    int Kommentare,
    int Anhaenge,
    int Dateiverweise,
    int Zeiteintraege);
