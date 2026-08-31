using System.Net;
using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Services;

public static class ApiAntwortleser
{
    public static Zurueckweisung BoardOderSpalteVerschwunden { get; } =
        new(["Das Board oder die Spalte gibt es nicht mehr."]);

    public static async Task<ApiErgebnis<T>> AlsErgebnis<T>(HttpResponseMessage antwort)
        where T : class
    {
        var anfrageWurdeZurueckgewiesen = antwort.StatusCode == HttpStatusCode.BadRequest;
        if (anfrageWurdeZurueckgewiesen)
        {
            var zurueckweisung = await Zurueckweisungsleser.Lies(antwort);
            return ApiErgebnis<T>.Zurueckgewiesen(zurueckweisung);
        }

        var boardOderSpalteIstUnbekannt = antwort.StatusCode == HttpStatusCode.NotFound;
        if (boardOderSpalteIstUnbekannt)
        {
            return ApiErgebnis<T>.Zurueckgewiesen(BoardOderSpalteVerschwunden);
        }

        antwort.EnsureSuccessStatusCode();
        var wert = await antwort.Content.ReadFromJsonAsync<T>();
        if (wert is null)
        {
            throw new InvalidOperationException("Die WebApi hat keine verwertbare Antwort zurückgegeben.");
        }

        return ApiErgebnis<T>.Erfolg(wert);
    }
}
