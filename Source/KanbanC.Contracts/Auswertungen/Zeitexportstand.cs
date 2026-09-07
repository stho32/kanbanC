namespace KanbanC.Contracts.Auswertungen;

// Der Stand eines Zeitexports — **ohne Zeilen**. Er ist damit kein zweiter Weg zu den Daten neben
// den Rohdaten über die API; wer die Einträge will, holt die Datei.
// Gebraucht wird er an zwei Stellen des Schirms: der Leerfall muss mit Grund und
// Kompensationsaktion beantwortet werden statt mit einem Verweis auf eine leere Datei, und die
// Zählzeile braucht **gerechnete** Zahlen — die Oberfläche zählt nichts nach.
// `Von` und `Bis` sind die **tatsächlich gelieferten** Grenzen, nie die angefragten; der Dateiname
// nennt genau sie.
public record Zeitexportstand(
    int Eintraege,
    int Karten,
    int Kontributoren,
    int Laufende,
    DateOnly Von,
    DateOnly Bis,
    string Dateiname);
