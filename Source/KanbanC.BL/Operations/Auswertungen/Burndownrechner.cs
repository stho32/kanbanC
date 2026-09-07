using KanbanC.BL.Models.Auswertungen;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Operations.Auswertungen;

// **Die eine Stelle, an der „offen an einem Tag“ entschieden wird:**
//
//   Eine Karte gilt an einem Kalendertag als offen, wenn sie zum Kartenbestand gehört und ihr
//   ErledigtAm an diesem Tag entweder fehlt oder nach ihm liegt.
//
// Am Tag ihres ErledigtAm ist sie bereits erledigt. Alle Ränder folgen daraus ohne
// Ausnahmeklausel: vor dem ersten Tag erledigt heißt an keinem Tag offen, ein künftiges Datum
// heißt an jedem Tag offen, kein Datum heißt offen.
// Kurve, Tagestabelle und Kopfzahlen lesen dieselbe Reihe; keine der drei rechnet nach.
public static class Burndownrechner
{
    public static IReadOnlyList<Burndowntag> Rechne(Erledigungsstandkarten bestand, Kalenderachse achse)
    {
        var reihe = new List<Burndowntag>();
        foreach (var tag in achse)
        {
            reihe.Add(new Burndowntag(tag, AnDiesemTagErledigt(bestand, tag), AnDiesemTagNochOffen(bestand, tag)));
        }

        return reihe;
    }

    // Die Kopfzahlen gelten dem ganzen Bestand und nicht dem Ausschnitt — sonst hieße dieselbe
    // Zahl an zwei Stellen Verschiedenes. „Offen“ ist der Wert der Kurve am letzten Tag, damit die
    // Zahl über der Kurve zu deren rechtem Rand passt.
    public static Burndownkopfzahlen Kopfzahlen(Erledigungsstandkarten bestand, IReadOnlyList<Burndowntag> reihe)
    {
        var offen = reihe[^1].OffeneKarten;
        return new Burndownkopfzahlen(offen, bestand.Kartenanzahl - offen, bestand.Kartenanzahl, bestand.OhneErledigungsdatumInAbschlussOderArchiv);
    }

    private static IReadOnlyList<Burndownkarte> AnDiesemTagErledigt(Erledigungsstandkarten bestand, DateOnly tag)
    {
        var erledigte = new List<Burndownkarte>();
        foreach (var karte in bestand)
        {
            if (karte.ErledigtAm == tag)
            {
                erledigte.Add(new Burndownkarte(karte.KarteId, karte.Kartennummer, karte.Titel));
            }
        }

        return erledigte;
    }

    private static int AnDiesemTagNochOffen(Erledigungsstandkarten bestand, DateOnly tag)
    {
        var offene = 0;
        foreach (var karte in bestand)
        {
            if (IstAnDiesemTagOffen(karte, tag))
            {
                offene = offene + 1;
            }
        }

        return offene;
    }

    private static bool IstAnDiesemTagOffen(Erledigungsstandkarte karte, DateOnly tag)
    {
        return karte.ErledigtAm is null || karte.ErledigtAm.Value > tag;
    }
}
