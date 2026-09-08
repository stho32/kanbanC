using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Models.Auswertungen;

// Eine gelesene Karte des Bestands, wie der Pufferstand sie braucht: ihr Sollband, ihre erfasste
// Zeit, ihr Erledigungstag und das, womit die Zeile sich benennt.
// Das Sollband fehlt, wenn die Karte keine Sollzeitzeile trägt: **fehlende Zeile heißt kein
// Soll** — und ohne Soll gibt es weder Puffer noch Verbrauch.
// `ErledigtAm` fehlt, wenn die Karte keine Erledigungszeile trägt: **fehlende Zeile heißt nicht
// erledigt**, wortgleich mit dem Burndown — nicht „steht in einer Abschlussspalte", nicht „ist
// archiviert".
public record Pufferstandkarte(
    long KarteId,
    string? Kartennummer,
    string Titel,
    TimeSpan ErfassteZeit,
    Zeitband? Sollband,
    DateOnly? ErledigtAm,
    bool IstArchiviert);
