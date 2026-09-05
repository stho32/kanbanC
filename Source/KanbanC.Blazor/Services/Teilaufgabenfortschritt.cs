using System.Globalization;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Services;

// Der Fortschritt wird gerechnet, nicht gespeichert — Muster Bahnenkopfzahl. Ein Zählfeld am
// Kartendetail wäre eine zweite Wahrheit neben der Liste, die ihn trägt, und liefe beim ersten
// nebenläufigen Abhaken auseinander.
// Die leere Liste beantwortet der Abschnitt selbst mit „Keine Teilaufgaben · anlegen"; hier steht
// sie trotzdem nicht offen: der Anteil einer leeren Liste ist 0 %, und ohne diesen Zweig teilte
// die Rechnung durch null.
public static class Teilaufgabenfortschritt
{
    private const int VolleAnzeige = 100;

    public static string AlsText(IReadOnlyList<Teilaufgabe> teilaufgaben)
    {
        var abgehakte = Abgehakte(teilaufgaben);
        return $"{abgehakte.ToString(CultureInfo.InvariantCulture)} von {teilaufgaben.Count.ToString(CultureInfo.InvariantCulture)}";
    }

    public static int AlsProzent(IReadOnlyList<Teilaufgabe> teilaufgaben)
    {
        var esGibtNichtsAbzuhaken = teilaufgaben.Count == 0;
        if (esGibtNichtsAbzuhaken)
        {
            return 0;
        }

        return Abgehakte(teilaufgaben) * VolleAnzeige / teilaufgaben.Count;
    }

    private static int Abgehakte(IReadOnlyList<Teilaufgabe> teilaufgaben)
    {
        return teilaufgaben.Count(teilaufgabe => teilaufgabe.Abgehakt);
    }
}
