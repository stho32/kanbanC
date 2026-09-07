using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Models.Auswertungen;

// Ein Zeiteintrag in der Gestalt, in der er in die Datei geht: eine Zeile je Eintrag, nicht je
// Summe. **Ende is null heißt „läuft"** — dieselbe Regel wie am Zeiteintrag selbst; die Datei
// schreibt dann weder Ende noch Dauer.
// Sie bleibt in der Fachlogik und reist nie über HTTP: aus ihr wird Text.
// Die KontributorId steht dabei, obwohl sie in keine Spalte der Datei geht — zwei Kontributoren
// dürfen gleich heißen (Migration 006), und „n Kontributoren" wäre über die Namen gezählt eine
// falsche Zahl.
public record Zeitexportzeile(
    long ZeiteintragId,
    string Kartennummer,
    string Kartentitel,
    long KontributorId,
    string Kontributorname,
    Kontributorart Kontributorart,
    DateTimeOffset Beginn,
    DateTimeOffset? Ende)
{
    // Der Tag eines Eintrags ist der Tag **seines eigenen Wertes** samt dessen Offset; geschnitten
    // wird an ihm.
    // Die Ablage schreibt jeden Zeitpunkt in UTC (`ZeitenRepository.AlsIsoText`), im Betrieb ist der
    // Offset deshalb `+00:00` und der Tag der UTC-Tag. Ein Eintrag, der kurz nach Mitternacht
    // Ortszeit beginnt, fällt damit in den UTC-Vortag — eine geerbte Eigenschaft der Zeiterfassung
    // und keine Festlegung dieses Exports.
    public DateOnly Beginntag => DateOnly.FromDateTime(Beginn.DateTime);
}
