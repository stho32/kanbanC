namespace KanbanC.Contracts.Boardimport;

// Dieselbe Gestalt für Vorschau und Lauf — eine zweite Berichtsform wären zwei Wahrheiten über
// denselben Vorgang.
// **BoardId ist in der Vorschau null**: das Board entsteht erst beim Schreiben, und eine
// erfundene Nummer wäre eine Zusage, die niemand hält.
// Die doppelten Namen und der Anhanghinweis sind die zwei Preise des Imports, und sie stehen
// **vor** dem Schreiben da: welche Personen ein zweites Mal entstehen, und dass die Anhänge ohne
// Inhalt ankommen.
public record Boardimportbericht(
    string Boardname,
    long? BoardId,
    Boardimportzahlen Zahlen,
    IReadOnlyList<string> DoppelteNamen, // stil-check: C09 wie Board.Spalten
    string Anhanghinweis);
