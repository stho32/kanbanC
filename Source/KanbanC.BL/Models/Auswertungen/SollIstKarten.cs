using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Models.Auswertungen;

// Die Karten eines Bestands in Kartennummernfolge — und die zwei Fragen, die die Summenzeile an
// sie stellt: **wie viele tragen kein Soll** und **was ist die Summe der Bänder**.
// Beide Antworten wohnen hier und nicht im Dienst, weil sie Aussagen über genau diese Menge sind.
public sealed class SollIstKarten
{
    private readonly SollIstKarte[] _karten;

    public SollIstKarten(IEnumerable<SollIstKarte> karten)
    {
        _karten = karten.ToArray();
    }

    public int Kartenanzahl => _karten.Length;

    public SollIstKarte this[int index] => _karten[index];

    public IEnumerator<SollIstKarte> GetEnumerator()
    {
        return ((IEnumerable<SollIstKarte>)_karten).GetEnumerator();
    }

    public TimeSpan ErfassteZeit
    {
        get
        {
            var summe = TimeSpan.Zero;
            foreach (var karte in _karten)
            {
                summe = summe + karte.ErfassteZeit;
            }

            return summe;
        }
    }

    // Die Zahl, die unter der Tabelle steht, damit die Summe nicht für vollständig gehalten wird.
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

    // Untergrenzen zu Untergrenze, Obergrenzen zu Obergrenze — die Summe eines Bandes ist selbst
    // ein Band. Gezählt werden nur Karten mit Band; trägt keine eines, gibt es keine Bandsumme
    // und nicht 0,0–0,0.
    public Zeitband? Bandsumme
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
}
