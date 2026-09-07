using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// **Die eine Regel des Slice:** welcher Knoten eine Karte wird. Die Schnittebene ist dabei eine
// **Untergrenze und keine Auswahl** — ein Knoten oberhalb, der keinen Nachfahren auf der
// Schnittebene hat, wird selbst zur Karte, sonst verschwände sein Teilbaum still.
// An der echten Planungsdatei gemessen kostet die Regel bei der Vorgabe nichts (41 = 41); sichtbar
// wird sie bei Feature-Schnitt (79 statt 54) und Bubble-Schnitt (445 statt 435).
public static class Kartenknotenwahl
{
    public static IReadOnlyList<Wbsknoten> Waehle(Wbsbaum baum, Wbsebene zielebene)
    {
        var karten = new List<Wbsknoten>();
        var kartenkennungen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var knoten in baum)
        {
            if (WirdOhneVorfahrenpruefungZurKarte(baum, knoten, zielebene))
            {
                karten.Add(knoten);
                kartenkennungen.Add(knoten.Id);
            }
        }

        NimmVerwaisteKnotenUnterhalbAuf(baum, zielebene, karten, kartenkennungen);
        karten.Sort((links, rechts) => links.Zeilennummer.CompareTo(rechts.Zeilennummer));
        return karten;
    }

    // Der Rettungsanker für eine Datei, in der eine Kette die Schnittebene überspringt: ein Knoten
    // unterhalb, über dem keine Karte steht, hätte sonst keinen Ort. In der echten Planungsdatei
    // tritt der Fall nicht auf — und er ist der Unterschied zwischen „vollständig“ und
    // „vollständig, solange die Datei sich benimmt“.
    private static void NimmVerwaisteKnotenUnterhalbAuf(Wbsbaum baum, Wbsebene zielebene, List<Wbsknoten> karten, HashSet<string> kartenkennungen)
    {
        foreach (var knoten in baum)
        {
            var derKnotenLiegtUnterhalbOhneKarteUeberSich = LiegtUnterhalbOhneKarteUeberSich(baum, knoten, zielebene, kartenkennungen);
            if (derKnotenLiegtUnterhalbOhneKarteUeberSich)
            {
                karten.Add(knoten);
                kartenkennungen.Add(knoten.Id);
            }
        }
    }

    private static bool LiegtUnterhalbOhneKarteUeberSich(Wbsbaum baum, Wbsknoten knoten, Wbsebene zielebene, HashSet<string> kartenkennungen)
    {
        if (knoten.Ebene <= zielebene)
        {
            return false;
        }

        if (kartenkennungen.Contains(knoten.Id))
        {
            return false;
        }

        return !HatKarteAlsVorfahr(baum, knoten, kartenkennungen);
    }

    // Die Application wird das **gewählte** Zielboard und nie eine Karte — der Import legt kein
    // Board an.
    private static bool WirdOhneVorfahrenpruefungZurKarte(Wbsbaum baum, Wbsknoten knoten, Wbsebene zielebene)
    {
        if (knoten.Ebene == Wbsebene.Application)
        {
            return false;
        }

        if (knoten.Ebene == zielebene)
        {
            return true;
        }

        var derKnotenLiegtOberhalbOhneNachfahrenAufDerSchnittebene = knoten.Ebene < zielebene && !baum.HatNachfahrenAuf(knoten.Id, zielebene);
        return derKnotenLiegtOberhalbOhneNachfahrenAufDerSchnittebene;
    }

    private static bool HatKarteAlsVorfahr(Wbsbaum baum, Wbsknoten knoten, HashSet<string> kartenkennungen)
    {
        foreach (var vorfahr in baum.VorfahrenVonObenNachUnten(knoten))
        {
            if (kartenkennungen.Contains(vorfahr.Id))
            {
                return true;
            }
        }

        return false;
    }

    // Die Karte, unter der ein Knoten hängt: der **nächste** Vorfahre, der selbst eine Karte ist.
    // Damit steht jeder Knoten genau einmal auf dem Board — als Karte, als Teilaufgabe der
    // nächsten Karte über ihm oder als Etikett.
    public static Wbsknoten? NaechsteKarteUeber(Wbsbaum baum, Wbsknoten knoten, HashSet<string> kartenkennungen)
    {
        var vorfahren = baum.VorfahrenVonObenNachUnten(knoten);
        for (var stelle = vorfahren.Count - 1; stelle >= 0; stelle--)
        {
            if (kartenkennungen.Contains(vorfahren[stelle].Id))
            {
                return vorfahren[stelle];
            }
        }

        return null;
    }
}
