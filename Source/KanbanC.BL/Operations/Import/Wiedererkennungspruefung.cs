using KanbanC.BL.Models.Import;
using KanbanC.Contracts.Fehler;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Operations.Import;

// **Die Grenze zwischen Zurückweisen und Melden liegt am angerichteten Schaden.** Hier stehen die
// drei flächigen Lagen: zwei Karten auf einem Knoten, ein anderer Pfad als beim ersten Lauf, eine
// andere Schnittebene. Jede von ihnen erzeugte auf einen Schlag Dutzende bis Hunderte Dubletten —
// geprüft wird deshalb **vor** der Vorschau, und es entsteht keine Karte.
// Ein **einzelner** entfernter Verweis steht nicht hier: er kostet genau eine Dublette, und eine
// Zurückweisung ließe vierzig richtige Karten an einer falschen scheitern.
// null heißt „diese Datei ist auf diesem Board wiedererkennbar“.
public static class Wiedererkennungspruefung
{
    public static Fehlerbefund? Pruefe(long boardId, Karteniststaende iststaende, string pfad, Schnittebene schnittebene, IReadOnlySet<string> sollknoten)
    {
        var doppelte = iststaende.DoppelteKupplung(pfad);
        var zweiKartenTragenDenselbenVerweis = doppelte.Count > 1;
        if (zweiKartenTragenDenselbenVerweis)
        {
            return Importbefunde.VerweisAnZweiKarten(boardId, Karteniststaende.Kupplung(doppelte[0], pfad)!, Benennung(doppelte[0]), Benennung(doppelte[1]));
        }

        var gekuppelte = iststaende.Gekuppelte(pfad);
        var befundZumPfad = BefundZumPfad(boardId, iststaende, pfad, sollknoten, gekuppelte.Count);
        if (befundZumPfad is not null)
        {
            return befundZumPfad;
        }

        return BefundZurSchnittebene(boardId, gekuppelte, pfad, schnittebene);
    }

    // **Die Grenze liegt am Schaden, nicht am Prinzip.** Zurückgewiesen wird, wenn unter einem
    // *anderen* Pfad mehr Knoten dieser Datei hängen als unter dem angefragten: dann legte der Lauf
    // die Mehrzahl seiner Karten ein zweites Mal an. Eine einzelne Karte unter einem fremden Pfad
    // bringt ihn dagegen nicht zu Fall — sie kostet eine Dublette und wird gemeldet.
    private static Fehlerbefund? BefundZumPfad(long boardId, Karteniststaende iststaende, string pfad, IReadOnlySet<string> sollknoten, int gekuppelteKarten)
    {
        var jePfad = new Dictionary<string, int>(StringComparer.Ordinal); // stil-check: C11 Treffer je Pfad des ersten Laufs, kein Domaenenbestand
        foreach (var herkunft in iststaende.AlleHerkuenfte())
        {
            var derKnotenStehtInDieserDatei = sollknoten.Contains(herkunft.KnotenId) && !string.Equals(herkunft.Pfad, pfad, StringComparison.Ordinal);
            if (derKnotenStehtInDieserDatei)
            {
                jePfad.TryGetValue(herkunft.Pfad, out var bisher);
                jePfad[herkunft.Pfad] = bisher + 1;
            }
        }

        if (jePfad.Count == 0)
        {
            return null;
        }

        var haeufigster = Haeufigster(jePfad);
        var derAngefragtePfadTraegtDieMehrzahl = gekuppelteKarten >= jePfad[haeufigster];
        if (derAngefragtePfadTraegtDieMehrzahl)
        {
            return null;
        }

        return Importbefunde.PfadWeichtVomErstenLaufAb(boardId, haeufigster, pfad, jePfad[haeufigster]);
    }

    // **Die Schnittebene des ersten Laufs ist ablesbar** — aus der Ebene der Knoten, auf die die
    // vorhandenen Karten zeigen: `#I0001` nennt einen Interaction-Knoten. Kein neues Anfragefeld, kein
    // Übersteuerungs-Flag: ein Feld, das nur eine Prüfung abschaltete, wäre tote Flexibilität.
    private static Fehlerbefund? BefundZurSchnittebene(long boardId, IReadOnlyList<Karteniststand> gekuppelte, string pfad, Schnittebene schnittebene)
    {
        var jeEbene = new Dictionary<Wbsebene, int>(); // stil-check: C11 Kartenzahl je Ebene des ersten Laufs, kein Domaenenbestand
        foreach (var stand in gekuppelte)
        {
            var knotenId = Herkunftsverweis.KnotenIdAus(Karteniststaende.Kupplung(stand, pfad)!)!;
            var ebene = Knotenkennung.EbeneAus(knotenId);
            if (ebene is not null)
            {
                jeEbene.TryGetValue(ebene.Value, out var bisher);
                jeEbene[ebene.Value] = bisher + 1;
            }
        }

        if (jeEbene.Count == 0)
        {
            return null;
        }

        var ebeneDesErstenLaufs = HaeufigsteEbene(jeEbene);
        var angefragteEbene = Wbswoerter.AlsWbsebene(schnittebene);
        if (ebeneDesErstenLaufs == angefragteEbene)
        {
            return null;
        }

        return Importbefunde.SchnittebeneWeichtVomErstenLaufAb(boardId, ebeneDesErstenLaufs.ToString(), angefragteEbene.ToString(), jeEbene[ebeneDesErstenLaufs]);
    }

    private static string Haeufigster(Dictionary<string, int> jePfad)
    {
        var haeufigster = string.Empty;
        var hoechste = 0;
        foreach (var eintrag in jePfad)
        {
            if (eintrag.Value > hoechste)
            {
                hoechste = eintrag.Value;
                haeufigster = eintrag.Key;
            }
        }

        return haeufigster;
    }

    private static Wbsebene HaeufigsteEbene(Dictionary<Wbsebene, int> jeEbene)
    {
        var haeufigste = Wbsebene.Interaction;
        var hoechste = 0;
        foreach (var eintrag in jeEbene)
        {
            if (eintrag.Value > hoechste)
            {
                hoechste = eintrag.Value;
                haeufigste = eintrag.Key;
            }
        }

        return haeufigste;
    }

    private static string Benennung(Karteniststand stand)
    {
        if (stand.Kartennummer is null)
        {
            return stand.Titel;
        }

        return stand.Kartennummer;
    }
}
