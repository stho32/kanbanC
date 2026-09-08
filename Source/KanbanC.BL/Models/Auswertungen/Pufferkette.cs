using KanbanC.BL.Operations.Auswertungen;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Models.Auswertungen;

// **Was aus den Karten für die Menge folgt** — die vier Größen der Kopfzeile als Aussagen über
// genau diesen Bestand. Sie wohnen hier und nicht im Dienst, genau wie die Bandsumme des
// Soll-Ist-Vergleichs bei ihrer Menge wohnt.
// Was eine einzelne Karte an Puffer mitbringt und verbraucht, entscheidet der Pufferrechner; die
// Kette liest ihn, statt seine Regel ein zweites Mal zu schreiben.
// **Drei Lagen, drei Antworten, und sie einzuebnen wäre der teuerste Fehler:** ohne Karte und ohne
// jedes Band gibt es nichts (`null`); tragen alle Karten Punktschätzungen, gibt es einen
// Kettenpuffer von `0,0` — dann sind Verbrauch und Fortschritt Zahlen und nur der Anteil hat
// keinen Wert, weil es nichts gibt, wovon er ein Anteil wäre.
public sealed class Pufferkette
{
    private const decimal Vollausschlag = 100m;
    private readonly Pufferstandkarten _karten;

    public Pufferkette(Pufferstandkarten karten)
    {
        _karten = karten;
    }

    public int Kartenanzahl => _karten.Kartenanzahl;

    // Der Puffer der Kette ist die Spanne zwischen aggressiver und abgesicherter Soll-Summe.
    // Karten ohne Band zählen in keiner der beiden Summen mit; trägt keine ein Band, gibt es
    // keinen Kettenpuffer und nicht 0,0.
    public decimal? KettenpufferStunden
    {
        get
        {
            var bandsumme = Bandsumme;
            if (bandsumme is null)
            {
                return null; // stil-check: C25 null heisst „diese Kette traegt kein Soll"
            }

            return bandsumme.BisStunden - bandsumme.VonStunden;
        }
    }

    public decimal? VerbrauchteStunden
    {
        get
        {
            if (Bandsumme is null)
            {
                return null; // stil-check: C25 null heisst „diese Kette traegt kein Soll"
            }

            var verbrauch = 0m;
            foreach (var karte in _karten)
            {
                var kartenpuffer = Pufferrechner.Rechne(karte.ErfassteZeit, karte.Sollband); // stil-check: C02 die Kette liest die Kartenregel, statt sie zweimal zu schreiben
                if (kartenpuffer is not null)
                {
                    verbrauch = verbrauch + kartenpuffer.VerbrauchteStunden;
                }
            }

            return verbrauch;
        }
    }

    // **Kettenpuffer 0 liefert keinen Anteil, sondern `null`** — bei lauter Punktschätzungen gibt
    // es nichts, wovon der Verbrauch ein Anteil wäre. Eine erfundene Zahl stünde hier an der
    // Stelle, an der die ehrliche Antwort „es gibt hier nichts zu verbrauchen" lautet.
    public decimal? VerbrauchsanteilProzent
    {
        get
        {
            var kettenpuffer = KettenpufferStunden;
            var verbrauch = VerbrauchteStunden;
            if (kettenpuffer is null || verbrauch is null)
            {
                return null; // stil-check: C25 null heisst „diese Kette traegt kein Soll"
            }

            var esGibtNichtsZuVerbrauchen = kettenpuffer.Value == 0m;
            if (esGibtNichtsZuVerbrauchen)
            {
                return null; // stil-check: C25 null heisst „diese Kette hat keinen Puffer"
            }

            return GanzeProzent(verbrauch.Value / kettenpuffer.Value);
        }
    }

    // Gewichtet wird nach der **aggressiven** Summe: der Verbrauch wird gegen die Untergrenze
    // gemessen, also muss der Fortschritt es auch — sonst trügen die beiden Achsen der Fieberkurve
    // zwei verschiedene Sollbegriffe.
    public decimal? FortschrittProzent
    {
        get
        {
            var bandsumme = Bandsumme;
            if (bandsumme is null)
            {
                return null; // stil-check: C25 null heisst „diese Kette traegt kein Soll"
            }

            var esGibtKeinSollZuErledigen = bandsumme.VonStunden == 0m;
            if (esGibtKeinSollZuErledigen)
            {
                return null; // stil-check: C25 null heisst „diese Kette traegt kein zu erledigendes Soll"
            }

            return GanzeProzent(ErledigtesSollStunden / bandsumme.VonStunden);
        }
    }

    public int ErledigteKarten
    {
        get
        {
            var erledigte = 0;
            foreach (var karte in _karten)
            {
                if (karte.ErledigtAm is not null)
                {
                    erledigte = erledigte + 1;
                }
            }

            return erledigte;
        }
    }

    // Die Zahl, die in der Fußzeile steht, damit die Kette nicht für vollständig gehalten wird:
    // sie sagt, wie viele Karten in keiner ihrer Größen vorkommen.
    public int KartenOhneSoll
    {
        get
        {
            var ohneSoll = 0;
            foreach (var karte in _karten)
            {
                if (karte.Sollband is null)
                {
                    ohneSoll = ohneSoll + 1;
                }
            }

            return ohneSoll;
        }
    }

    private decimal ErledigtesSollStunden
    {
        get
        {
            var erledigtesSoll = 0m;
            foreach (var karte in _karten)
            {
                var dieseKarteZaehltZumFortschritt = karte.Sollband is not null && karte.ErledigtAm is not null;
                if (dieseKarteZaehltZumFortschritt)
                {
                    erledigtesSoll = erledigtesSoll + karte.Sollband!.VonStunden;
                }
            }

            return erledigtesSoll;
        }
    }

    // Untergrenzen zu Untergrenze, Obergrenzen zu Obergrenze — die Summe eines Bandes ist selbst
    // ein Band, und ohne eine einzige Karte mit Band gibt es sie nicht.
    private Zeitband? Bandsumme
    {
        get
        {
            Zeitband? summe = null;
            foreach (var karte in _karten)
            {
                if (karte.Sollband is null)
                {
                    continue;
                }

                if (summe is null)
                {
                    summe = karte.Sollband;
                    continue;
                }

                summe = new Zeitband(summe.VonStunden + karte.Sollband.VonStunden, summe.BisStunden + karte.Sollband.BisStunden);
            }

            return summe;
        }
    }

    // Kaufmännisch auf ganze Prozent: die Achsen der Fieberkurve tragen Prozentpunkte, und eine
    // Nachkommastelle behauptete eine Genauigkeit, die eine Aufwandsschätzung nicht hat.
    private static decimal GanzeProzent(decimal anteil)
    {
        return Math.Round(anteil * Vollausschlag, MidpointRounding.AwayFromZero);
    }
}
