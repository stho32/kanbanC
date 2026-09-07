namespace KanbanC.Contracts.Auswertungen;

// Ein Kalendertag der Reihe: was an ihm erledigt wurde und wie viele Karten an seinem Ende noch
// offen waren. Tage ohne Abschluss stehen mit leerer Liste und `0` erledigten Karten darin — sie
// fehlen nicht, sonst bekäme die Kurve eine Lücke.
public record Burndowntag(DateOnly Tag, IReadOnlyList<Burndownkarte> ErledigteKarten, int OffeneKarten); // stil-check: C09 wie SollIstAuswertung.Zeilen
