using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Boards;

// Der Abfrageparameter „archiviert“ kommt als Text herein und wird an der Grenze geprüft: ASP.NET
// weist einen unlesbaren bool-Wert vor dem Handler ab, und zwar mit einer Antwort ohne unseren
// Befund. Ohne Parameter gilt die Standardliste. Die Route kommt als Eingang, damit die
// Kompensation die Adresse nennt, die der Aufrufer wirklich gerufen hat — jede Adresse erklärt
// sich selbst.
public static class Archivfilter
{
    private const string WertIstUnlesbarCode = "archiv-filter-unlesbar";
    private static readonly Archivierung Aktive = new(false);

    public static Ergebnis<Archivierung> Aus(string? abfragewert, string route)
    {
        var derParameterFehlt = string.IsNullOrWhiteSpace(abfragewert);
        if (derParameterFehlt)
        {
            return Ergebnis<Archivierung>.Erfolg(Aktive);
        }

        var wertIstLesbar = bool.TryParse(abfragewert, out var istArchiviert);
        if (wertIstLesbar)
        {
            return Ergebnis<Archivierung>.Erfolg(new Archivierung(istArchiviert));
        }

        return Ergebnis<Archivierung>.Zurueckgewiesen(new Pruefbefunde([UnlesbarerWert(abfragewert!, route)]));
    }

    private static Fehlerbefund UnlesbarerWert(string abfragewert, string route)
    {
        return new Fehlerbefund(
            WertIstUnlesbarCode,
            $"„{abfragewert}“ ist kein Wahrheitswert; „archiviert“ nimmt „true“ oder „false“.",
            $"`{route}?archiviert=true` für die archivierten Einträge aufrufen — oder `{route}` ohne Parameter für die aktiven.");
    }
}
