using KanbanC.BL.Models.Zeiten;

namespace KanbanC.BL.Interfaces.Zeiten;

public interface IZeitenRepository
{
    // Legt einen offenen Eintrag für das Paar (Karte, Kontributor) an — oder gibt den zurück, der
    // für dieses Paar schon läuft, ohne zu schreiben.
    // Die Uhr wird hereingereicht und nicht im Repository gelesen: sonst wäre der Beginn im Test
    // nicht setzbar.
    // null heißt: diese KarteId gibt es nicht.
    Zeitmessungsstart? StarteZeitmessung(long karteId, long kontributorId, DateTimeOffset beginn);
}
