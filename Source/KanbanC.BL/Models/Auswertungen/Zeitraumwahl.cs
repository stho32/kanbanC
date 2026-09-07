namespace KanbanC.BL.Models.Auswertungen;

// Die gelesene Fassung der Abfrageparameter, die einen Zeitraum aufspannen. Eine Grenze ist null,
// wenn ihr Parameter fehlt — das ist kein Fehler, sondern „hier wird nicht geschnitten".
// Der Burndown liest nur die erste Grenze: seine Kurve läuft immer bis heute.
public record Zeitraumwahl(DateOnly? Von, DateOnly? Bis);
