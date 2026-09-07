namespace KanbanC.BL.Models.Auswertungen;

// Die gelesene Fassung des Abfrageparameters `?seit=`. `Seit` ist null, wenn der Parameter fehlt —
// das ist kein Fehler, sondern die Standardachse.
// Ein Ende gibt es nicht: die Kurve läuft immer bis heute.
public record Zeitraumwahl(DateOnly? Seit);
