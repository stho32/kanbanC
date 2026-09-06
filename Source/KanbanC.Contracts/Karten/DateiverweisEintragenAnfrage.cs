namespace KanbanC.Contracts.Karten;

// Ein Dateiverweis, nicht die ganze Liste — wie beim Anlegen einer Teilaufgabe, beim Kommentar
// und beim Anhang. Mehr als Pfad und Urheber braucht das Eintragen nicht.
// Kontributor ist long und nicht long?: es gibt kein „niemand". Ein Dateiverweis ohne Urheber
// ist nach dem Fertig-Kriterium keiner, und die Spalte trägt NOT NULL. Das Feld heißt
// Kontributor nach der Tabelle, auf die es zeigt; der fachliche Begriff Urheber steht am
// gelesenen Dateiverweis.
// Kein Feld für den Zeitpunkt: den setzt die Anwendung beim Eintragen. Könnte der Aufrufer ihn
// mitgeben, könnte ein Agent die Reihenfolge der Liste fälschen — und der Zeitpunkt ist hier
// zugleich die einzige Ordnung.
public record DateiverweisEintragenAnfrage(string Pfad, long Kontributor);
