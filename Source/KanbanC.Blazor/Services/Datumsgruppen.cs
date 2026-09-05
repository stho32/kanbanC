using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Services;

// Die Karten der Abschlussbahn kommen bereits nach Erledigungsdatum absteigend an; gebündelt wird
// nur, was ohnehin nebeneinander liegt.
//
// Die beiden jüngsten Tage tragen ihren Namen — „Heute“ und „Gestern“ aus dem Artboard. Jeder
// frühere Tag trägt sein Datum, und zwar in der Schreibweise, die der Terminformatierer für die
// Termine des Boards schon verwendet: das Datumsformat der Oberfläche ist eine offene
// Projektentscheidung, und eine zweite Schreibweise daneben nähme sie stillschweigend vorweg.
public static class Datumsgruppen
{
    private const string OhneDatum = "Ohne Datum";
    private const string AmHeutigenTag = "Heute";
    private const string AmVortag = "Gestern";

    public static IReadOnlyList<Datumsgruppe> Bilde(IReadOnlyList<Karte> karten, DateOnly heute)
    {
        var gruppen = new List<Datumsgruppe>();
        var stelle = 0;
        while (stelle < karten.Count)
        {
            var laenge = LaengeDerGruppe(karten, stelle);
            var kartenDerGruppe = karten.Skip(stelle).Take(laenge).ToList();
            gruppen.Add(new Datumsgruppe(Ueberschrift(karten[stelle].ErledigtAm, heute), stelle, kartenDerGruppe));
            stelle = stelle + laenge;
        }

        return gruppen;
    }

    private static int LaengeDerGruppe(IReadOnlyList<Karte> karten, int ersteStelle)
    {
        var datumDerGruppe = karten[ersteStelle].ErledigtAm;
        var laenge = 1;
        while (ersteStelle + laenge < karten.Count && karten[ersteStelle + laenge].ErledigtAm == datumDerGruppe)
        {
            laenge = laenge + 1;
        }

        return laenge;
    }

    private static string Ueberschrift(DateOnly? erledigtAm, DateOnly heute)
    {
        if (erledigtAm is null)
        {
            return OhneDatum;
        }

        if (erledigtAm.Value == heute)
        {
            return AmHeutigenTag;
        }

        if (erledigtAm.Value == heute.AddDays(-1))
        {
            return AmVortag;
        }

        return Terminformatierer.AlsText(erledigtAm);
    }
}
