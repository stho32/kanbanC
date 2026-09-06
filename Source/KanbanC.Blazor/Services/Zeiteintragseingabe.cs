namespace KanbanC.Blazor.Services;

// Was das Zeitenformular meldet, wenn es bestätigt wird: die Werte in **Ortszeit**, so wie sie
// eingetippt wurden. Die Umrechnung in einen vollen Zeitstempel macht die Seite über
// Zeitpunktform — das Formular rechnet nicht.
// Ein Bis von null heißt „läuft"; das Formular gibt es nur her, wenn ein Ende überhaupt
// entfallen darf.
public record Zeiteintragseingabe(long Kontributor, DateOnly Tag, TimeOnly Von, TimeOnly? Bis);
