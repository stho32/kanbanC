namespace KanbanC.Contracts.Import;

// Welcher Lauf, wann und woraus. Der Kopf steht **im Bericht** und nicht in der Oberfläche: ein
// Bericht, der den Schirm verlässt — als Antwort an einen Agenten oder als kopierter Text —, ist
// ohne diese drei Angaben in dem Augenblick wertlos, in dem er ankommt.
// Der Zeitpunkt ist der des Laufs, nicht der der Anzeige.
public record Importlaufkopf(DateTimeOffset Zeitpunkt, long Urhebernummer, string Urhebername, string Pfad);
