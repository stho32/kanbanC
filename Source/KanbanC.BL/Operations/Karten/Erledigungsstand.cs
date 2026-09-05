using KanbanC.BL.Models.Karten;

namespace KanbanC.BL.Operations.Karten;

// Gemessen wird der Eintritt in die Abschlussspalte, nicht die letzte Bewegung: ein Zug innerhalb
// der Bahn ist bloßes Umsortieren und datiert die Karte nicht um. Der Austritt loescht das Datum,
// weil „erledigt“ eine Aussage über den jetzigen Zustand ist — eine wieder aufgemachte Karte unter
// ihrem alten Datum zu fuehren, hiesse sie an einem Tag zu zeigen, an dem sie nicht fertig war.
public static class Erledigungsstand
{
    public static Erledigungsaenderung NachDemZug(
        bool zielspalteIstAbschlussspalte,
        bool derZugBleibtInDerZielspalte,
        DateOnly? bisherigeErledigung,
        DateOnly heute)
    {
        var dieKarteWirdInIhrerAbschlussbahnUmsortiert = zielspalteIstAbschlussspalte && derZugBleibtInDerZielspalte;
        if (dieKarteWirdInIhrerAbschlussbahnUmsortiert)
        {
            return Erledigungsaenderung.Unveraendert;
        }

        if (zielspalteIstAbschlussspalte)
        {
            return Erledigungsaenderung.Setzen(heute);
        }

        var dieKarteVerlaesstEineAbschlussbahn = bisherigeErledigung is not null;
        if (dieKarteVerlaesstEineAbschlussbahn)
        {
            return Erledigungsaenderung.Loeschen;
        }

        return Erledigungsaenderung.Unveraendert;
    }
}
