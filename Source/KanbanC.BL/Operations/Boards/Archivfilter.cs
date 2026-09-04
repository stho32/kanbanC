using KanbanC.BL.Models;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Boards;

// Der Abfrageparameter „archiviert" kommt als Text herein und wird an der Grenze geprüft: ASP.NET
// weist einen unlesbaren bool-Wert vor dem Handler ab, und zwar mit einer Antwort ohne unseren
// Befund. Ohne Parameter gilt die Standardliste.
public static class Archivfilter
{
    private const string Listenroute = "GET /api/boards";
    private const string WertIstUnlesbarCode = "archiv-filter-unlesbar";
    private static readonly Archivierung Aktive = new(false);

    public static Ergebnis<Archivierung> Aus(string? abfragewert)
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

        return Ergebnis<Archivierung>.Zurueckgewiesen(new Pruefbefunde([UnlesbarerWert(abfragewert!)]));
    }

    private static Fehlerbefund UnlesbarerWert(string abfragewert)
    {
        return new Fehlerbefund(
            WertIstUnlesbarCode,
            $"„{abfragewert}“ ist kein Wahrheitswert; „archiviert“ nimmt „true“ oder „false“.",
            $"`{Listenroute}?archiviert=true` für die archivierten Boards aufrufen — oder `{Listenroute}` ohne Parameter für die aktiven.");
    }
}
