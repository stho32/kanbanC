using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Models.Zeiten;

// Der Eintrag **plus die Auskunft, ob er neu ist**. Sie entscheidet an der API zwischen 201 („jetzt
// läuft er") und 200 („er lief schon") und darf nicht aus dem Eintrag geraten werden: einem
// Eintrag ist nicht anzusehen, ob dieser Aufruf ihn angelegt hat — ein Beginn von vor einer
// Millisekunde kann beides bedeuten.
public sealed record Zeitmessungsstart(Zeiteintrag Zeiteintrag, bool IstNeu);
