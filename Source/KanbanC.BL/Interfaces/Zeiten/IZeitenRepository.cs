using KanbanC.BL.Models.Zeiten;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.BL.Interfaces.Zeiten;

public interface IZeitenRepository
{
    // Legt einen offenen Eintrag für das Paar (Karte, Kontributor) an — oder gibt den zurück, der
    // für dieses Paar schon läuft, ohne zu schreiben.
    // Die Uhr wird hereingereicht und nicht im Repository gelesen: sonst wäre der Beginn im Test
    // nicht setzbar.
    // null heißt: diese KarteId gibt es nicht.
    Zeitmessungsstart? StarteZeitmessung(long karteId, long kontributorId, DateTimeOffset beginn);

    // Setzt das Ende eines laufenden Eintrags dieser Karte — und lässt ein schon gesetztes Ende
    // unangetastet: ein zweiter Stopp verschöbe sonst die gemessene Zeit nach hinten.
    // Die Uhr wird hereingereicht und nicht im Repository gelesen: sonst wäre das Ende im Test
    // nicht setzbar.
    // null heißt: diesen Zeiteintrag gibt es an dieser Karte nicht.
    Zeiteintrag? BeendeZeitmessung(long karteId, long zeiteintragId, DateTimeOffset uhrzeit);
}
