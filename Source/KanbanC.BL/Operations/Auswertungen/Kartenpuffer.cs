namespace KanbanC.BL.Operations.Auswertungen;

// Was eine einzelne Karte zum Puffer der Kette beiträgt: den Puffer, den ihr Band aufspannt, und
// den Teil davon, den ihre erfasste Zeit schon aufgebraucht hat.
// Der Verbrauch kann größer sein als der Puffer — eine Karte, die über ihre Obergrenze läuft, hat
// mehr verbraucht, als sie mitgebracht hat.
public record Kartenpuffer(decimal PufferStunden, decimal VerbrauchteStunden);
