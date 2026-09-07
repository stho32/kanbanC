using System.Globalization;
using KanbanC.BL.Models;
using KanbanC.BL.Models.Auswertungen;
using KanbanC.Contracts.Fehler;

namespace KanbanC.BL.Operations.Auswertungen;

// Eine Tagesgrenze kommt als Text herein und wird an der Grenze geprüft: ASP.NET weist einen
// unlesbaren DateOnly-Wert vor dem Handler ab, und zwar mit einer Antwort ohne unseren Befund.
// Fehlt der Parameter, wird nicht geschnitten. Die Route kommt als Eingang, damit die Kompensation
// die Adresse nennt, die der Aufrufer wirklich gerufen hat.
// Der **Name** der Grenze kommt ebenfalls als Eingang: der Burndown ruft mit „seit", der
// Zeitexport mit „von" und „bis" — dieselbe Frage zweimal gestellt, und deshalb kein Zwilling.
// Die verdrehte Spanne (`bis` vor `von`) gehört hierher und nicht in den Dienst oder den Schirm:
// sie ist dieselbe Grenze wie die Lesbarkeit.
// Zwilling von Archivfilter.
public static class Zeitraumfilter
{
    private const string WertIstUnlesbarCode = "zeitraum-filter-unlesbar";
    private const string SpanneIstVerdrehtCode = "zeitraum-filter-verdreht";
    private const string Isodatumsformat = "yyyy-MM-dd";
    private const string Beispieltag = "2026-09-05";
    private const string Vongrenze = "von";
    private const string Bisgrenze = "bis";

    public static Ergebnis<Zeitraumwahl> Aus(string? abfragewert, string parametername, string route)
    {
        var grenze = Grenze(abfragewert, parametername, route);
        if (!grenze.IstErfolg)
        {
            return Ergebnis<Zeitraumwahl>.Zurueckgewiesen(grenze.Befunde);
        }

        return Ergebnis<Zeitraumwahl>.Erfolg(new Zeitraumwahl(grenze.Wert.Tag, null));
    }

    public static Ergebnis<Zeitraumwahl> AusPaar(string? von, string? bis, string route)
    {
        var untergrenze = Grenze(von, Vongrenze, route);
        if (!untergrenze.IstErfolg)
        {
            return Ergebnis<Zeitraumwahl>.Zurueckgewiesen(untergrenze.Befunde);
        }

        var obergrenze = Grenze(bis, Bisgrenze, route);
        if (!obergrenze.IstErfolg)
        {
            return Ergebnis<Zeitraumwahl>.Zurueckgewiesen(obergrenze.Befunde);
        }

        var wahl = new Zeitraumwahl(untergrenze.Wert.Tag, obergrenze.Wert.Tag);
        var dieSpanneIstVerdreht = IstVerdreht(wahl);
        if (dieSpanneIstVerdreht)
        {
            return Zurueckgewiesen(VerdrehteSpanne(wahl.Von!.Value, wahl.Bis!.Value, route));
        }

        return Ergebnis<Zeitraumwahl>.Erfolg(wahl);
    }

    // Fehlt eine der beiden Grenzen, gibt es keine Spanne, die verdreht sein könnte.
    private static bool IstVerdreht(Zeitraumwahl wahl)
    {
        if (wahl.Von is null || wahl.Bis is null)
        {
            return false;
        }

        return wahl.Bis.Value < wahl.Von.Value;
    }

    private static Ergebnis<Tagesgrenze> Grenze(string? abfragewert, string parametername, string route)
    {
        var derParameterFehlt = string.IsNullOrWhiteSpace(abfragewert);
        if (derParameterFehlt)
        {
            return Ergebnis<Tagesgrenze>.Erfolg(new Tagesgrenze(null));
        }

        var wertIstLesbar = DateOnly.TryParseExact(abfragewert, Isodatumsformat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tag);
        if (wertIstLesbar)
        {
            return Ergebnis<Tagesgrenze>.Erfolg(new Tagesgrenze(tag));
        }

        return Ergebnis<Tagesgrenze>.Zurueckgewiesen(new Pruefbefunde([UnlesbarerWert(abfragewert!, parametername, route)]));
    }

    private static Fehlerbefund UnlesbarerWert(string abfragewert, string parametername, string route)
    {
        return new Fehlerbefund(
            WertIstUnlesbarCode,
            $"„{abfragewert}“ ist kein Datum; „{parametername}“ nimmt die Form „{Isodatumsformat}“, zum Beispiel „{Beispieltag}“.",
            $"`{route}?{parametername}={Beispieltag}` mit einem Datum in dieser Form aufrufen — oder `{route}` ohne Parameter, dann wird nicht geschnitten.");
    }

    private static Fehlerbefund VerdrehteSpanne(DateOnly von, DateOnly bis, string route)
    {
        var vontag = von.ToString(Isodatumsformat, CultureInfo.InvariantCulture);
        var bistag = bis.ToString(Isodatumsformat, CultureInfo.InvariantCulture);
        return new Fehlerbefund(
            SpanneIstVerdrehtCode,
            $"„{Vongrenze}“ ist „{vontag}“ und „{Bisgrenze}“ ist „{bistag}“; „{Bisgrenze}“ liegt vor „{Vongrenze}“, und diese Spanne enthält keinen Tag.",
            $"`{route}?{Vongrenze}={bistag}&{Bisgrenze}={vontag}` aufrufen — die beiden Grenzen tauschen.");
    }

    private static Ergebnis<Zeitraumwahl> Zurueckgewiesen(Fehlerbefund befund)
    {
        return Ergebnis<Zeitraumwahl>.Zurueckgewiesen(new Pruefbefunde([befund]));
    }

    private sealed record Tagesgrenze(DateOnly? Tag);
}
