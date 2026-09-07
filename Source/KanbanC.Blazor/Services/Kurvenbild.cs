namespace KanbanC.Blazor.Services;

// Das gerechnete Bild der Kurve: das `points`-Attribut für die Polylinie, dieselben Punkte einzeln
// für die Marken darauf, und der Höchstwert für die Beschriftung der Achse.
public record Kurvenbild(string Punkteattribut, IReadOnlyList<Kurvenpunkt> Punkte, int Hoechstwert); // stil-check: C09 Komposition benannter Werte
