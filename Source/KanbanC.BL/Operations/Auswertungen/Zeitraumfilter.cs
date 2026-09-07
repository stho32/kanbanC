using System.Globalization;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Auswertungen;

// Der Abfrageparameter „seit“ kommt als Text herein und wird an der Grenze geprüft: ASP.NET weist
// einen unlesbaren DateOnly-Wert vor dem Handler ab, und zwar mit einer Antwort ohne unseren
// Befund. Ohne Parameter gilt die Standardachse. Die Route kommt als Eingang, damit die
// Kompensation die Adresse nennt, die der Aufrufer wirklich gerufen hat.
// Zwilling von Archivfilter.
public static class Zeitraumfilter
{
    private const string WertIstUnlesbarCode = "zeitraum-filter-unlesbar";
    private const string Isodatumsformat = "yyyy-MM-dd";
    private static readonly Zeitraumwahl OhneSchnitt = new(null);

    public static Ergebnis<Zeitraumwahl> Aus(string? abfragewert, string route)
    {
        var derParameterFehlt = string.IsNullOrWhiteSpace(abfragewert);
        if (derParameterFehlt)
        {
            return Ergebnis<Zeitraumwahl>.Erfolg(OhneSchnitt);
        }

        var wertIstLesbar = DateOnly.TryParseExact(abfragewert, Isodatumsformat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var seit);
        if (wertIstLesbar)
        {
            return Ergebnis<Zeitraumwahl>.Erfolg(new Zeitraumwahl(seit));
        }

        return Ergebnis<Zeitraumwahl>.Zurueckgewiesen(new Pruefbefunde([UnlesbarerWert(abfragewert!, route)]));
    }

    private static Fehlerbefund UnlesbarerWert(string abfragewert, string route)
    {
        return new Fehlerbefund(
            WertIstUnlesbarCode,
            $"„{abfragewert}“ ist kein Datum; „seit“ nimmt die Form „{Isodatumsformat}“, zum Beispiel „2026-09-05“.",
            $"`{route}?seit=2026-09-05` mit einem Datum in dieser Form aufrufen — oder `{route}` ohne Parameter für die Standardachse.");
    }
}
