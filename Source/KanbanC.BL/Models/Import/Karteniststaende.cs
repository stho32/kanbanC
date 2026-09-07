using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Models.Import;

// Der Iststand einer Kartenklasse auf einem Board. Sie beantwortet die drei Fragen der
// Wiedererkennung: welche Karte trägt eine Kupplung in diese Datei, welche trägt keine, und
// welche Kupplung liegt doppelt.
// **Die Kupplung ist der Dateiverweis `pfad#ID`** — nie der Titel: der ist änderbar und führt
// nirgendwohin zurück.
public sealed class Karteniststaende
{
    private readonly Karteniststand[] _staende;

    public Karteniststaende(IEnumerable<Karteniststand> staende)
    {
        _staende = staende.ToArray();
    }

    public int Kartenanzahl => _staende.Length;

    public Karteniststand this[int index] => _staende[index];

    public IEnumerator<Karteniststand> GetEnumerator()
    {
        return ((IEnumerable<Karteniststand>)_staende).GetEnumerator();
    }

    // Der Verweis dieser Karte zurück in die Datei unter dem Pfad der Anfrage — oder null, wenn
    // sie keinen trägt.
    public static string? Kupplung(Karteniststand stand, string pfad)
    {
        var kupplungen = KupplungenIn(stand, pfad);
        if (kupplungen.Count == 0)
        {
            return null;
        }

        return kupplungen[0];
    }

    public IReadOnlyList<Karteniststand> Gekuppelte(string pfad)
    {
        var gekuppelte = new List<Karteniststand>();
        foreach (var stand in _staende)
        {
            if (Kupplung(stand, pfad) is not null)
            {
                gekuppelte.Add(stand);
            }
        }

        return gekuppelte;
    }

    public IReadOnlyList<Karteniststand> Ungekuppelte(string pfad)
    {
        var ungekuppelte = new List<Karteniststand>();
        foreach (var stand in _staende)
        {
            if (Kupplung(stand, pfad) is null)
            {
                ungekuppelte.Add(stand);
            }
        }

        return ungekuppelte;
    }

    // Ein Verweis, der an zwei Karten hängt, macht die Frage „welche wäre nachzuziehen?“
    // unentscheidbar — deshalb kommt hier das Paar und nicht nur ein Wahrheitswert.
    // Gezählt wird über **alle** Verweise einer Karte in diese Datei und nicht nur über den einen,
    // den Kupplung wählt: eine kopierte Karte trägt den fremden Verweis oft neben ihrem eigenen.
    public IReadOnlyList<Karteniststand> DoppelteKupplung(string pfad)
    {
        var jeVerweis = new Dictionary<string, List<Karteniststand>>(StringComparer.Ordinal); // stil-check: C11 Karten je Kupplung, kein Domaenenbestand
        foreach (var stand in _staende)
        {
            foreach (var verweis in KupplungenIn(stand, pfad))
            {
                if (!jeVerweis.TryGetValue(verweis, out var gleiche))
                {
                    gleiche = [];
                    jeVerweis[verweis] = gleiche;
                }

                gleiche.Add(stand);
            }
        }

        foreach (var gleiche in jeVerweis.Values)
        {
            if (gleiche.Count > 1)
            {
                return gleiche;
            }
        }

        return [];
    }

    private static IReadOnlyList<string> KupplungenIn(Karteniststand stand, string pfad)
    {
        var kupplungen = new List<string>();
        foreach (var verweis in stand.Dateiverweise)
        {
            var derVerweisFuehrtInDieseDatei = Herkunftsverweis.KnotenIdAus(verweis) is not null
                && string.Equals(Herkunftsverweis.PfadAus(verweis), pfad, StringComparison.Ordinal);
            if (derVerweisFuehrtInDieseDatei)
            {
                kupplungen.Add(verweis);
            }
        }

        return kupplungen;
    }

    // Die Knoten-IDs, auf die die vorhandenen Karten unter **irgendeinem** Pfad zeigen — die
    // Grundlage für „die IDs treffen, die Pfade nicht“.
    public IReadOnlyList<(string Pfad, string KnotenId)> AlleHerkuenfte()
    {
        var herkuenfte = new List<(string Pfad, string KnotenId)>();
        foreach (var stand in _staende)
        {
            foreach (var verweis in stand.Dateiverweise)
            {
                var knotenId = Herkunftsverweis.KnotenIdAus(verweis);
                if (knotenId is not null)
                {
                    herkuenfte.Add((Herkunftsverweis.PfadAus(verweis), knotenId));
                }
            }
        }

        return herkuenfte;
    }
}
