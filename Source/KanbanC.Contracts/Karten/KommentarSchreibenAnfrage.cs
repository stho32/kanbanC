namespace KanbanC.Contracts.Karten;

// Ein Kommentar, nicht die ganze Liste — wie beim Anlegen einer Teilaufgabe. Mehr als Text und
// Urheber braucht das Schreiben nicht.
// Kontributor ist long und nicht long?: es gibt kein „niemand". Beim Verantwortlichen an der
// Karte ist null ein gültiger Wert, hier wäre es ein Kommentar ohne Absender — und genau den
// schließt das Fertig-Kriterium aus. Das Feld heißt Kontributor nach der Tabelle, auf die es
// zeigt; der fachliche Begriff Urheber steht am gelesenen Kommentar.
// Kein Feld für den Zeitpunkt: den setzt die Anwendung beim Schreiben. Könnte der Aufrufer ihn
// mitgeben, könnte ein Agent die Reihenfolge des Gesprächs fälschen.
public record KommentarSchreibenAnfrage(string Text, long Kontributor);
