using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Services;

// Was der Schirm beisammen hat, bevor er die WebApi ruft. Er wohnt in der Oberfläche und nicht in
// den Contracts: die Anfrage selbst reist als multipart-Formular, und ein DTO dafür wäre eine
// zweite Gestalt derselben Felder.
public record Importauftrag(string Dateiname, long Kartenklasse, Schnittebene Schnittebene, string Pfad, bool Trocken, long Kontributor);
