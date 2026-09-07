using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// **Der Titel ist Verdachtsmoment und nie Schlüssel.** Entsteht eine Karte, obwohl auf dem Board
// schon eine ohne Kupplung mit demselben Titel steht, wird das gemeldet — zugeordnet wird
// deshalb nichts, und nachgezogen erst recht nicht.
// Die Lage entsteht auf zwei Wegen: ein Mensch hat den Dateiverweis entfernt (`I0019`), oder er
// hat die Karte von Hand angelegt.
// null heißt „kein Verdacht“.
public static class Dublettenhinweis
{
    public static string? Fuer(Kartenentwurf entwurf, IReadOnlyList<Karteniststand> kartenOhneKupplung)
    {
        var aehnliche = Aehnliche(entwurf, kartenOhneKupplung);
        if (aehnliche is null)
        {
            return null;
        }

        return $"ähnlich zu „{Benennung(aehnliche)}“ — Dateiverweis `{entwurf.Dateiverweis}` an der alten Karte nachtragen (`I0019`), dann die neue archivieren (`I0014`).";
    }

    // Erst der ganze Titel, dann die Kennung vorn: ein umbenannter Knoten trägt einen anderen
    // Titel, aber dieselbe `[I0008]`-Klammer.
    private static Karteniststand? Aehnliche(Kartenentwurf entwurf, IReadOnlyList<Karteniststand> kartenOhneKupplung)
    {
        foreach (var stand in kartenOhneKupplung)
        {
            if (string.Equals(stand.Titel, entwurf.Titel, StringComparison.Ordinal))
            {
                return stand;
            }
        }

        var klammer = $"[{entwurf.Knoten.Id}]";
        foreach (var stand in kartenOhneKupplung)
        {
            if (stand.Titel.StartsWith(klammer, StringComparison.Ordinal))
            {
                return stand;
            }
        }

        return null;
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
