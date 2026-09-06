using KanbanC.BL.Models.Zeiten;
using KanbanC.Contracts.Karten;
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

    // Legt einen abgeschlossenen Eintrag an — ohne vorherige Messung, mit Beginn und Ende des
    // Aufrufers. Der partielle Index kann dabei nie anschlagen: ein Nachtrag traegt immer ein
    // Ende.
    // null heißt: diese KarteId gibt es nicht.
    Zeiteintrag? TrageNach(long karteId, ZeiteintragNachtragenAnfrage anfrage);

    // Der Eintrag, wie er vor einer Änderung dasteht — der Dienst braucht seinen bisherigen
    // Kontributor, weil die Stilllegung beim Ändern nur bei Kontributorwechsel greift.
    // null heißt: diesen Zeiteintrag gibt es an dieser Karte nicht.
    Zeiteintrag? Lies(long karteId, long zeiteintragId);

    // Ändert Kontributor, Beginn und Ende eines bestehenden Eintrags; die Karte bleibt, was sie
    // war. Ein Ende von null macht ihn wieder laufend — steht dem ein anderer laufender Eintrag
    // desselben Paares im Weg, wird **nicht** geschrieben und er kommt in der Auskunft mit.
    // Geprüft wird unter demselben Schreibschloss, unter dem geschrieben wird: sonst bliebe ein
    // Fenster, in dem der partielle Index statt eines Befunds zuschlägt.
    // null heißt: diesen Zeiteintrag gibt es an dieser Karte nicht.
    Zeiteintragsaenderung? Aendere(long karteId, long zeiteintragId, ZeiteintragAendernAnfrage anfrage);

    // Entfernt einen Eintrag und liefert das ganze Kartendetail ohne ihn — Hausform
    // EntferneAnhang, weil dieselbe Seite es verbraucht.
    // null heißt: diesen Zeiteintrag gibt es an dieser Karte nicht.
    Kartendetail? Loesche(long karteId, long zeiteintragId);
}
