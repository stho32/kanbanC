using KanbanC.Contracts.Import;

namespace KanbanC.BL.Models.Import;

// Was der Soll-Ist-Vergleich über den Knoten einer Karte herausgefunden hat: in welches Fach sie
// fällt, welche Nummer sie trägt, wo sie eine hat, und was an ihrer Zeile zu sagen ist —
// zurückgenommene Abhakung, Statusabweichung, Dublettenverdacht.
public record Kartenwirkung(string KnotenId, Importwirkung Wirkung, string? Kartennummer, string? Grund);
