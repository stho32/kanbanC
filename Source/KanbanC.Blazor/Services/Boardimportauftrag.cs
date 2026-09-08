namespace KanbanC.Blazor.Services;

// Was der Schirm beisammen hat, bevor er die WebApi ruft — **zwei Felder, mehr fuehrt die Route
// nicht**. Er wohnt in der Oberfläche und nicht in den Contracts: die Anfrage selbst reist als
// multipart-Formular, und ein DTO dafür wäre eine zweite Gestalt derselben Felder.
public record Boardimportauftrag(string Dateiname, bool Trocken);
