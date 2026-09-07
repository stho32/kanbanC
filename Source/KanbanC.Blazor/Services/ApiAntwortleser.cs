using System.Net;
using KanbanC.Contracts.Fehler;

namespace KanbanC.Blazor.Services;

public static class ApiAntwortleser
{
    public static Zurueckweisung BoardOderSpalteVerschwunden { get; } = new([
        new Fehlerbefund(
            "board-oder-spalte-verschwunden",
            "Das Board oder die Spalte gibt es nicht mehr.",
            "`GET /api/boards` abrufen und mit einem vorhandenen Board weiterarbeiten."),
    ]);

    // Für Routen, deren 404 selbst schon sagt, **welches** Ding fehlt und wie man hinkommt: der
    // Soll-Ist-Vergleich nennt die Board- oder Kartenklassennummer samt Kompensationsaktion, und
    // der Sammelsatz oben würde genau diese Auskunft wegwerfen.
    public static async Task<ApiErgebnis<T>> AlsErgebnisMitGemeldetenBefunden<T>(HttpResponseMessage antwort)
        where T : class
    {
        var dieAnfrageWurdeZurueckgewiesen = antwort.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound;
        if (dieAnfrageWurdeZurueckgewiesen)
        {
            var zurueckweisung = await Zurueckweisungsleser.Lies(antwort);
            return ApiErgebnis<T>.Zurueckgewiesen(zurueckweisung);
        }

        antwort.EnsureSuccessStatusCode();
        return ApiErgebnis<T>.Erfolg(await Wert<T>(antwort));
    }

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
        return ApiErgebnis<T>.Erfolg(await Wert<T>(antwort));
    }

    private static async Task<T> Wert<T>(HttpResponseMessage antwort)
        where T : class
    {
        var wert = await antwort.Content.ReadFromJsonAsync<T>();
        if (wert is null)
        {
            throw new InvalidOperationException("Die WebApi hat keine verwertbare Antwort zurückgegeben.");
        }

        return wert;
    }
}
