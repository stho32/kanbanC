using System.Globalization;

namespace KanbanC.Blazor.Services;

// Die absolute Adresse, unter der der Browser die Zeitexportdatei holt. Sie zeigt **direkt auf die
// WebApi** und nicht auf eine Blazor-Route: über den Blazor-Prozess flössen die Bytes zweimal über
// das Netz und zusätzlich durch den SignalR-Kreislauf, und der Browser bekäme keinen echten
// Download mit Name, Fortschritt und Abbruch.
// Pure Logik in der Oberflächenschicht, Muster Anhangadresse: die Basisadresse kommt als
// Parameter, damit die Rechnung ohne Browser und ohne Konfiguration prüfbar bleibt.
// Eine fehlende Grenze steht nicht in der Abfrage — sie schneidet dann nicht.
public static class Zeitexportadresse
{
    private const string Isodatumsformat = "yyyy-MM-dd";

    public static string Fuer(string oeffentlicheBasisAdresse, long boardId, long kartenklasseId, DateOnly? von, DateOnly? bis)
    {
        var basis = oeffentlicheBasisAdresse.TrimEnd('/');
        return $"{basis}/api/boards/{boardId}/kartenklassen/{kartenklasseId}/zeitexport.csv{Zeitraumabfrage(von, bis)}";
    }

    // Auch der Klient, der nur den Stand holt, hängt diese Abfrage an — dieselbe Zusage über
    // dieselben zwei Parameternamen, deshalb an einer Stelle.
    public static string Zeitraumabfrage(DateOnly? von, DateOnly? bis)
    {
        var grenzen = new List<string>();
        if (von is not null)
        {
            grenzen.Add($"von={Tag(von.Value)}");
        }

        if (bis is not null)
        {
            grenzen.Add($"bis={Tag(bis.Value)}");
        }

        if (grenzen.Count == 0)
        {
            return string.Empty;
        }

        return "?" + string.Join("&", grenzen);
    }

    private static string Tag(DateOnly grenze)
    {
        return grenze.ToString(Isodatumsformat, CultureInfo.InvariantCulture);
    }
}
