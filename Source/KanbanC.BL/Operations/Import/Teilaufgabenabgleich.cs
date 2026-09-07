using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Was aus den Teilaufgaben einer wiedererkannten Karte wird — **abgeglichen über die ID vorn**
// (`B0405 …`) und nicht über den ganzen Text: sonst verlöre ein umbenannter Knoten seine Zeile
// samt Haken und bekäme eine neue daneben.
// **Eine Teilaufgabe ohne ID-Präfix ist fremd**: ein Mensch hat sie angelegt, sie wird nie
// geändert und nie entfernt.
public static class Teilaufgabenabgleich
{
    public static Teilaufgabenabgleichergebnis Gleiche(IReadOnlyList<Teilaufgabenentwurf> entwuerfe, IReadOnlyList<Teilaufgabenstand> vorhandene)
    {
        var fremd = new List<Teilaufgabenstand>();
        var ausDerDatei = new Dictionary<string, Teilaufgabenstand>(StringComparer.Ordinal); // stil-check: C11 vorhandene Schritte je Knoten-ID, kein Domaenenbestand
        foreach (var teilaufgabe in vorhandene)
        {
            var kennung = Knotenkennung.FuehrendeKennung(teilaufgabe.Text);
            if (kennung is null)
            {
                fremd.Add(teilaufgabe);
                continue;
            }

            ausDerDatei[kennung] = teilaufgabe;
        }

        var anzulegen = new List<Teilaufgabenentwurf>();
        var zuAendern = new List<Teilaufgabenaenderung>();
        var unveraendert = new List<Teilaufgabenstand>();
        var getroffeneKennungen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entwurf in entwuerfe)
        {
            OrdneEntwurfEin(entwurf, ausDerDatei, getroffeneKennungen, anzulegen, zuAendern, unveraendert);
        }

        var zuEntfernen = new List<Teilaufgabenstand>();
        foreach (var eintrag in ausDerDatei)
        {
            var dieDateiKenntDiesenKnotenNichtMehr = !getroffeneKennungen.Contains(eintrag.Key);
            if (dieDateiKenntDiesenKnotenNichtMehr)
            {
                zuEntfernen.Add(eintrag.Value);
            }
        }

        return new Teilaufgabenabgleichergebnis(anzulegen, zuAendern, unveraendert, zuEntfernen, fremd);
    }

    private static void OrdneEntwurfEin(
        Teilaufgabenentwurf entwurf,
        Dictionary<string, Teilaufgabenstand> ausDerDatei,
        HashSet<string> getroffeneKennungen,
        List<Teilaufgabenentwurf> anzulegen,
        List<Teilaufgabenaenderung> zuAendern,
        List<Teilaufgabenstand> unveraendert)
    {
        var kennung = Knotenkennung.FuehrendeKennung(entwurf.Text);
        var derEntwurfTraegtKeineKennung = kennung is null;
        if (derEntwurfTraegtKeineKennung)
        {
            anzulegen.Add(entwurf);
            return;
        }

        getroffeneKennungen.Add(kennung!);
        var dieKarteKenntDiesenKnotenNochNicht = !ausDerDatei.TryGetValue(kennung!, out var vorhandene);
        if (dieKarteKenntDiesenKnotenNochNicht)
        {
            anzulegen.Add(entwurf);
            return;
        }

        var derTextUndDerHakenStimmenUeberein = string.Equals(vorhandene!.Text, entwurf.Text, StringComparison.Ordinal) && vorhandene.Abgehakt == entwurf.Abgehakt;
        if (derTextUndDerHakenStimmenUeberein)
        {
            unveraendert.Add(vorhandene);
            return;
        }

        // **Die Datei gewinnt auch beim Haken** — und der alte Haken reist mit, damit die Zeile
        // sagen kann, was geschieht, statt es stillschweigend zu tun.
        zuAendern.Add(new Teilaufgabenaenderung(vorhandene.TeilaufgabeId, entwurf.Text, vorhandene.Position, entwurf.Abgehakt, vorhandene.Abgehakt));
    }
}
