using KanbanC.BL.Models.Import;

namespace KanbanC.BL.Operations.Import;

// Findet die Knotentabelle in der Datei und gibt ihre Datenzeilen heraus. Erkannt wird sie an
// ihrer Kopfzeile — an den fünf Spalten, die eine Knotentabelle ausmachen —, nicht an ihrer
// Stelle im Text: eine WBS-Datei trägt auch andere Tabellen, und die Stelle der einen ist keine
// Zusage.
public static class Knotentabellenleser
{
    public const string Kopfspalten = "ID, Ebene, Eltern, Name, Status";

    private static readonly string[] Erkennungsspalten = ["id", "ebene", "eltern", "name", "status"];

    public static IReadOnlyList<Knotentabellenzeile> LiesDatenzeilen(IReadOnlyList<string> zeilen)
    {
        var kopfstelle = KopfzeilenStelle(zeilen);
        var esGibtKeineKnotentabelle = kopfstelle < 0;
        if (esGibtKeineKnotentabelle)
        {
            return [];
        }

        var datenzeilen = new List<Knotentabellenzeile>();
        for (var stelle = kopfstelle + 1; stelle < zeilen.Count; stelle++)
        {
            var zeile = zeilen[stelle];
            var dieTabelleEndetHier = !Zeilenzerleger.IstTabellenzeile(zeile);
            if (dieTabelleEndetHier)
            {
                return datenzeilen;
            }

            var zellen = Zeilenzerleger.Zerlege(zeile);
            if (IstTrennzeile(zellen))
            {
                continue;
            }

            datenzeilen.Add(new Knotentabellenzeile(stelle + 1, zellen));
        }

        return datenzeilen;
    }

    private static int KopfzeilenStelle(IReadOnlyList<string> zeilen)
    {
        for (var stelle = 0; stelle < zeilen.Count; stelle++)
        {
            if (!Zeilenzerleger.IstTabellenzeile(zeilen[stelle]))
            {
                continue;
            }

            if (IstKopfzeile(Zeilenzerleger.Zerlege(zeilen[stelle])))
            {
                return stelle;
            }
        }

        return -1;
    }

    private static bool IstKopfzeile(Wbszellen zellen)
    {
        if (zellen.Zellenanzahl < Erkennungsspalten.Length)
        {
            return false;
        }

        for (var stelle = 0; stelle < Erkennungsspalten.Length; stelle++)
        {
            if (!string.Equals(zellen[stelle].Trim(), Erkennungsspalten[stelle], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    // Die Strichzeile unter dem Kopf ist Markdown-Beiwerk und kein Knoten. Sie wird übergangen und
    // nicht als übersprungene Zeile gemeldet — eine Meldung über sie wäre Rauschen.
    private static bool IstTrennzeile(Wbszellen zellen)
    {
        if (zellen.Zellenanzahl == 0)
        {
            return false;
        }

        foreach (var zelle in zellen)
        {
            var zelleIstEinStrichmuster = zelle.Length > 0 && zelle.All(zeichen => zeichen is '-' or ':' or ' ');
            if (!zelleIstEinStrichmuster)
            {
                return false;
            }
        }

        return true;
    }
}
