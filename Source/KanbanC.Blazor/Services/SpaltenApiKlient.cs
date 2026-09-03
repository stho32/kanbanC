using System.Net;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Fehler;

namespace KanbanC.Blazor.Services;

public sealed class SpaltenApiKlient
{
    private const string KlientName = "KanbanC";
    private const string BoardsRoute = "api/boards";
    private readonly IHttpClientFactory _klientFabrik;

    public SpaltenApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
    }

    public async Task<ApiErgebnis<Spalte>> LegeSpalteAn(long boardId, SpalteAnlegenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync(SpaltenRoute(boardId), anfrage);
        return await ApiAntwortleser.AlsErgebnis<Spalte>(antwort);
    }

    public async Task<ApiErgebnis<Spalte>> AendereSpalte(long boardId, long spalteId, SpalteAendernAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{SpaltenRoute(boardId)}/{spalteId}", anfrage);
        return await ApiAntwortleser.AlsErgebnis<Spalte>(antwort);
    }

    public async Task<ApiErgebnis<IReadOnlyList<Spalte>>> SetzeReihenfolge(long boardId, Spaltenreihenfolge reihenfolge)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{SpaltenRoute(boardId)}/reihenfolge", reihenfolge);
        return await ApiAntwortleser.AlsErgebnis<IReadOnlyList<Spalte>>(antwort);
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
            return ApiAntwortleser.BoardOderSpalteVerschwunden;
        }

        antwort.EnsureSuccessStatusCode();
        return null;
    }

    private static string SpaltenRoute(long boardId)
    {
        return $"{BoardsRoute}/{boardId}/spalten";
    }
}
