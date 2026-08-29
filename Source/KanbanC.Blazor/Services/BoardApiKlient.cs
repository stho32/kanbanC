using System.Net;
using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Services;

public sealed class BoardApiKlient
{
    private const string KlientName = "KanbanC";
    private const string BoardsRoute = "api/boards";
    private readonly IHttpClientFactory _klientFabrik;

    public BoardApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
    }

    public async Task<IReadOnlyList<BoardUebersicht>> LadeAlleBoards()
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        var boards = await klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);
        if (boards is null)
        {
            return [];
        }

        return boards;
    }

    public async Task<Board?> LadeBoard(long boardId)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.GetAsync($"{BoardsRoute}/{boardId}");
        var boardIstUnbekannt = antwort.StatusCode == HttpStatusCode.NotFound;
        if (boardIstUnbekannt)
        {
            return null;
        }

        antwort.EnsureSuccessStatusCode();
        return await antwort.Content.ReadFromJsonAsync<Board>();
    }

    public async Task<ApiErgebnis<Board>> LegeBoardAn(BoardAnlegenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync(BoardsRoute, anfrage);
        var anfrageWurdeZurueckgewiesen = antwort.StatusCode == HttpStatusCode.BadRequest;
        if (anfrageWurdeZurueckgewiesen)
        {
            var zurueckweisung = await LeseZurueckweisung(antwort);
            return ApiErgebnis<Board>.Zurueckgewiesen(zurueckweisung);
        }

        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        if (board is null)
        {
            throw new InvalidOperationException("Die WebApi hat kein Board zurückgegeben.");
        }

        return ApiErgebnis<Board>.Erfolg(board);
    }

    private static async Task<Zurueckweisung> LeseZurueckweisung(HttpResponseMessage antwort)
    {
        var rumpfIstLeer = antwort.Content.Headers.ContentLength == 0;
        if (rumpfIstLeer)
        {
            return new Zurueckweisung(["Die WebApi hat die Anfrage zurückgewiesen (HTTP 400)."]);
        }

        var zurueckweisung = await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        if (zurueckweisung is null)
        {
            return new Zurueckweisung(["Die WebApi hat die Anfrage zurückgewiesen (HTTP 400)."]);
        }

        return zurueckweisung;
    }
}
