namespace KanbanC.BL.Models.Auswertungen;

// Das Ergebnis des Schnitts: die verbliebenen Zeilen und die **tatsächlich gelieferten** Grenzen.
// Geliefert heißt nicht angefragt — ein Dateiname, der eine nicht gelieferte Spanne nennt, lügt.
public record Zeitexportausschnitt(Zeitexportzeilen Zeilen, DateOnly Von, DateOnly Bis);
