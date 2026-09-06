namespace KanbanC.Contracts.Klassen;

// Genau eine Angabe: welche Kartenklasse die Karte fortan trägt. null heißt „ohne Klasse" und
// löst eine bestehende Zuordnung — dieselbe Handlung an derselben Stelle, deshalb kein eigenes
// DELETE daneben.
public record KartenklasseZuordnenAnfrage(long? Kartenklasse);
