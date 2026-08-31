using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Services;

public sealed class KartenApiKlient
{
    private const string KlientName = "KanbanC";
    private const string BoardsRoute = "api/boards";
    private readonly IHttpClientFactory _klientFabrik;

    public KartenApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
    }

    public async Task<ApiErgebnis<Karte>> LegeKarteAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten", anfrage);
        return await ApiAntwortleser.AlsErgebnis<Karte>(antwort);
    }
}
