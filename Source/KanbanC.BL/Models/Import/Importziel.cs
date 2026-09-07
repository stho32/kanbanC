using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Models.Import;

// Das gewählte Board mit dem, was ein Import von ihm braucht: seinen Namen für die Meldungen,
// seine Bahnen für die Zielspalte und seine Kartenklassen für die Nummer.
public record Importziel(long BoardId, string Boardname, IReadOnlyList<Importspalte> Spalten, IReadOnlyList<Kartenklasse> Kartenklassen); // stil-check: C09 wie Board.Spalten
