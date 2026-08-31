using System.Net;
using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Services;

public sealed class SpaltenApiKlient
{
    private const string KlientName = "KanbanC";
    private const string BoardsRoute = "api/boards";
    private static readonly Zurueckweisung SpalteOderBoardVerschwunden =
        new(["Das Board oder die Spalte gibt es nicht mehr."]);
    private readonly IHttpClientFactory _klientFabrik;

    public SpaltenApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
    }

    public async Task<ApiErgebnis<Spalte>> LegeSpalteAn(long boardId, SpalteAnlegenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync(SpaltenRoute(boardId), anfrage);
        return await AlsErgebnis<Spalte>(antwort);
    }

    public async Task<ApiErgebnis<Spalte>> AendereSpalte(long boardId, long spalteId, SpalteAendernAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{SpaltenRoute(boardId)}/{spalteId}", anfrage);
        return await AlsErgebnis<Spalte>(antwort);
    }

    public async Task<ApiErgebnis<IReadOnlyList<Spalte>>> SetzeReihenfolge(long boardId, Spaltenreihenfolge reihenfolge)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{SpaltenRoute(boardId)}/reihenfolge", reihenfolge);
        return await AlsErgebnis<IReadOnlyList<Spalte>>(antwort);
    }

    public async Task<Zurueckweisung?> EntferneSpalte(long boardId, long spalteId)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.DeleteAsync($"{SpaltenRoute(boardId)}/{spalteId}");
        var entfernenWurdeZurueckgewiesen = antwort.StatusCode == HttpStatusCode.BadRequest;
        if (entfernenWurdeZurueckgewiesen)
        {
            return await Zurueckweisungsleser.Lies(antwort);
        }

        var spalteIstUnbekannt = antwort.StatusCode == HttpStatusCode.NotFound;
        if (spalteIstUnbekannt)
        {
            return SpalteOderBoardVerschwunden;
        }

        antwort.EnsureSuccessStatusCode();
        return null;
    }

    private static string SpaltenRoute(long boardId)
    {
        return $"{BoardsRoute}/{boardId}/spalten";
    }

    private static async Task<ApiErgebnis<T>> AlsErgebnis<T>(HttpResponseMessage antwort)
        where T : class
    {
        var anfrageWurdeZurueckgewiesen = antwort.StatusCode == HttpStatusCode.BadRequest;
        if (anfrageWurdeZurueckgewiesen)
        {
            var zurueckweisung = await Zurueckweisungsleser.Lies(antwort);
            return ApiErgebnis<T>.Zurueckgewiesen(zurueckweisung);
        }

        var spalteIstUnbekannt = antwort.StatusCode == HttpStatusCode.NotFound;
        if (spalteIstUnbekannt)
        {
            return ApiErgebnis<T>.Zurueckgewiesen(SpalteOderBoardVerschwunden);
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
