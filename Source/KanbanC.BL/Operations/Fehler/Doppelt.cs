using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Fehler;

// Eine dritte Lage neben „gibt es nicht" und „ist stillgelegt": das Ding gibt es schon, und
// genau das ist der Grund. 400 statt 404 — es fehlt kein Ding, es wurde eine Regel verletzt;
// `Nichtgefunden.MeldetEinFehlendesDing` kennt diesen Code deshalb nicht.
// Die Regel gilt nur für den Dateiverweis: bei Anhang, Teilaufgabe und Kommentar sind zwei
// gleiche Texte **zwei Dinge** — zwei Dateien, zwei Arbeiten, zwei Äußerungen. Derselbe Pfad an
// derselben Karte ist **dasselbe Ding**: beide Zeilen zeigen auf dieselbe Datei, und die zweite
// trägt keine Aussage.
public static class Doppelt
{
    private const string DateiverweisDoppelt = "dateiverweis-doppelt";
    private const string ZeiteintragLaeuftSchon = "zeiteintrag-laeuft-schon";

    // Der Befund nennt den Pfad und die Kartennummer, damit der Aufrufer weiß, welcher seiner
    // Pfade schon steht — und **nie** eine nackte Datenbankmeldung über einen verletzten Index
    // sieht. Die Kompensation ist keine Wiederholung: derselbe Aufruf ginge wieder so aus.
    public static Fehlerbefund Dateiverweis(long karteId, string pfad)
    {
        return new Fehlerbefund(
            DateiverweisDoppelt,
            $"Der Pfad „{pfad}“ steht schon an der Karte {karteId}; ein zweiter Verweis auf dieselbe Datei sagt nichts Neues.",
            $"`GET /api/karten/{karteId}` abrufen, die Pfade in „dateiverweise“ ablesen und den Aufruf mit einem noch nicht eingetragenen Pfad wiederholen.");
    }

    // Dieselbe Lage am Zeiteintrag: für das Paar (Karte, Kontributor) läuft schon einer, und ein
    // zweiter laufender wäre zwei Antworten auf eine Frage. Der Befund nennt die Nummer des
    // anderen, damit der Aufrufer ihn ansprechen kann — sonst käme die nackte Datenbankmeldung
    // des partiellen Index UX_Zeiteintrag_Karte_Kontributor_Laufend heraus.
    public static Fehlerbefund LaufenderZeiteintrag(long karteId, long kontributorId, long laufendeZeiteintragId)
    {
        return new Fehlerbefund(
            ZeiteintragLaeuftSchon,
            $"Für den Kontributor {kontributorId} läuft an der Karte {karteId} schon der Zeiteintrag {laufendeZeiteintragId}; ein zweiter laufender Eintrag desselben Paares ist ausgeschlossen.",
            $"Den Zeiteintrag {laufendeZeiteintragId} über `PUT /api/karten/{karteId}/zeiten/{laufendeZeiteintragId}/ende` stoppen oder ihn über `PUT /api/karten/{karteId}/zeiten/{laufendeZeiteintragId}` ändern und den Aufruf wiederholen.");
    }
}
