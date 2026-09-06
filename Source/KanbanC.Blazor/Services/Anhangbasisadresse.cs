namespace KanbanC.Blazor.Services;

// Die im Netz erreichbare Adresse der WebApi, wie der **Browser** sie sieht — nicht die, unter
// der der Blazor-Prozess sie ruft. Auf einem Rechner fallen beide zusammen; im LAN nicht, sobald
// die interne Adresse auf „localhost" zeigt.
// Ein eigener kleiner Wert statt eines Griffs in die Konfiguration aus der Komponente: die
// Voreinstellung („dann eben die interne Adresse") gehört an die eine Stelle, an der der Wirt
// aufgebaut wird, und nicht in jeden Schirm, der einen Verweis zeichnet.
public record Anhangbasisadresse(string Adresse);
