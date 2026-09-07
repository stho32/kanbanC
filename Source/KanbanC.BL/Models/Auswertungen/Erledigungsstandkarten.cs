namespace KanbanC.BL.Models.Auswertungen;

// Die Karten eines Bestands — und die drei Fragen, die der Burndown an sie stellt: **wie viele
// sind es**, **wann wurde die erste erledigt** und **wie viele stehen ohne Erledigungsdatum in
// einer Abschlussspalte oder im Archiv**.
// Alle drei Antworten wohnen hier und nicht im Rechner, weil sie Aussagen über genau diese Menge
// sind.
public sealed class Erledigungsstandkarten
{
    private readonly Erledigungsstandkarte[] _karten;

    public Erledigungsstandkarten(IEnumerable<Erledigungsstandkarte> karten)
    {
        _karten = karten.ToArray();
    }

    public int Kartenanzahl => _karten.Length;

    public Erledigungsstandkarte this[int index] => _karten[index];

    public IEnumerator<Erledigungsstandkarte> GetEnumerator()
    {
        return ((IEnumerable<Erledigungsstandkarte>)_karten).GetEnumerator();
    }

    // Der Anfang der Achse, wenn kein Zeitraum gewählt wurde. Trägt keine Karte ein Datum, gibt es
    // keinen frühesten Abschluss — dann beginnt die Achse heute.
    public DateOnly? FruehesteErledigung
    {
        get
        {
            DateOnly? fruehestes = null;
            foreach (var karte in _karten)
            {
                if (karte.ErledigtAm is null)
                {
                    continue;
                }

                var dieseKarteWurdeFrueherErledigt = fruehestes is null || karte.ErledigtAm.Value < fruehestes.Value;
                if (dieseKarteWurdeFrueherErledigt)
                {
                    fruehestes = karte.ErledigtAm;
                }
            }

            return fruehestes;
        }
    }

    // Die Zahl, die unter der Kurve steht, damit eine flache Strecke nicht für Stillstand gehalten
    // wird: diese Karten verlassen die Kurve nie. Sie stammen aus der Zeit vor Migration 008 oder
    // vor dem Import; ein nachträgliches Datum wäre erfunden.
    // Eine Karte ohne Datum in einer normalen Bahn zählt nicht mit — sie ist schlicht offen.
    public int OhneErledigungsdatumInAbschlussOderArchiv
    {
        get
        {
            var ohneDatum = 0;
            foreach (var karte in _karten)
            {
                var dieKarteGiltAlsFertigOhneEsZuBelegen = karte.ErledigtAm is null && (karte.StehtInAbschlussspalte || karte.IstArchiviert);
                if (dieKarteGiltAlsFertigOhneEsZuBelegen)
                {
                    ohneDatum = ohneDatum + 1;
                }
            }

            return ohneDatum;
        }
    }
}
