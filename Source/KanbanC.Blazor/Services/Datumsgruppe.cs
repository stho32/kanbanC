using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Services;

// ErsteStelle ist der Index der ersten Karte dieser Gruppe in der Kartenliste der Bahn: die
// Ablagestellen rechnen weiterhin über die ganze Bahn, nicht über die Gruppe.
public sealed record Datumsgruppe(string Ueberschrift, int ErsteStelle, IReadOnlyList<Karte> Karten);
