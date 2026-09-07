using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Das Muster aus `.claude/app-architectures/Common/snippets/SollIstVergleich.md`, angewandt statt
// erfunden: zwei Kollektionen, ein Schlüssel, eine Gleichheitsregel, vier Fächer.
// **Zwei Typparameter statt einem** — hier sind die Seiten ehrlich verschieden: der Sollentwurf
// trägt seinen Wbsknoten, der Iststand seine KarteId, seine Nummer, seine Spalte und seine Zeiten.
// Ein gemeinsames DTO hätte zwei Hälften, die je eine Seite leer lässt. Gemeinsam ist stattdessen
// das **verglichene** Kartenabbild — und genau das ist der Kern der Musteranweisung.
// Der Schlüssel ist immer der Herkunftsverweis und damit eine Zeichenkette; ein dritter
// Typparameter, der nur an string bände, wäre tote Flexibilität (C16).
public sealed class SollIstVergleicher<TSoll, TIst>
{
    private readonly Func<TSoll, string> _sollschluessel;
    private readonly Func<TIst, string> _istschluessel;
    private readonly Func<TSoll, TIst, bool> _sindGleich;

    public SollIstVergleicher(Func<TSoll, string> sollschluessel, Func<TIst, string> istschluessel, Func<TSoll, TIst, bool> sindGleich)
    {
        _sollschluessel = sollschluessel;
        _istschluessel = istschluessel;
        _sindGleich = sindGleich;
    }

    public SollIstVergleichErgebnis<TSoll, TIst> Vergleiche(IEnumerable<TSoll> sollzustand, IEnumerable<TIst> istzustand)
    {
        var sollliste = sollzustand.ToList();
        var istliste = istzustand.ToList();
        var istNachSchluessel = new Dictionary<string, TIst>(StringComparer.Ordinal); // stil-check: C11 Zuordnung je Herkunftsverweis, kein Domaenenbestand
        foreach (var ist in istliste)
        {
            istNachSchluessel[_istschluessel(ist)] = ist;
        }

        var zuErstellen = new List<TSoll>();
        var zuAktualisieren = new List<(TSoll Soll, TIst Ist)>();
        var unveraendert = new List<(TSoll Soll, TIst Ist)>();
        var getroffeneSchluessel = new HashSet<string>(StringComparer.Ordinal);
        foreach (var soll in sollliste)
        {
            var schluessel = _sollschluessel(soll);
            getroffeneSchluessel.Add(schluessel);
            var esGibtKeineKarteZuDiesemSchluessel = !istNachSchluessel.TryGetValue(schluessel, out var ist);
            if (esGibtKeineKarteZuDiesemSchluessel)
            {
                zuErstellen.Add(soll);
                continue;
            }

            if (_sindGleich(soll, ist!))
            {
                unveraendert.Add((soll, ist!));
                continue;
            }

            zuAktualisieren.Add((soll, ist!));
        }

        var verwaist = new List<TIst>();
        foreach (var ist in istliste)
        {
            var dieDateiKenntDiesenVerweisNichtMehr = !getroffeneSchluessel.Contains(_istschluessel(ist));
            if (dieDateiKenntDiesenVerweisNichtMehr)
            {
                verwaist.Add(ist);
            }
        }

        return new SollIstVergleichErgebnis<TSoll, TIst>(zuErstellen, zuAktualisieren, unveraendert, verwaist);
    }
}
