using KanbanC.Contracts.Klassen;

namespace KanbanC.Blazor.Services;

public sealed class KartenklassenApiKlient
{
    private const string KlientName = "KanbanC";
    private const string BoardsRoute = "api/boards";
    private readonly IHttpClientFactory _klientFabrik;

    public KartenklassenApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
    }

    public async Task<ApiErgebnis<IReadOnlyList<Kartenklasse>>> LadeKartenklassen(long boardId)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.GetAsync(KartenklassenRoute(boardId));
        return await ApiAntwortleser.AlsErgebnis<IReadOnlyList<Kartenklasse>>(antwort);
    }

    public async Task<ApiErgebnis<Kartenklasse>> LegeKartenklasseAn(long boardId, KartenklasseAnlegenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync(KartenklassenRoute(boardId), anfrage);
        return await ApiAntwortleser.AlsErgebnis<Kartenklasse>(antwort);
    }

    private static string KartenklassenRoute(long boardId)
    {
        return $"{BoardsRoute}/{boardId}/kartenklassen";
    }
}
