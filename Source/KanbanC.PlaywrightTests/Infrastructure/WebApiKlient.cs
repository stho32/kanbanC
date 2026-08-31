using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.PlaywrightTests.Infrastructure;

// Der Weg des Agenten: dieselben Routen, die die Oberfläche ruft, nur ohne Browser.
public sealed class WebApiKlient : IDisposable
{
    private const string BoardsRoute = "api/boards";
    private readonly HttpClient _klient;

    public WebApiKlient(string webApiAdresse)
    {
        _klient = new HttpClient { BaseAddress = new Uri(webApiAdresse + "/") };
    }

    public async Task<Board> LadeBoard(long boardId)
    {
        var board = await _klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{boardId}");
        if (board is null)
        {
            throw new InvalidOperationException("Die WebApi hat kein Board zurückgegeben.");
        }

        return board;
    }

    public async Task<Karte> LegeKarteAn(long boardId, long spalteId, string titel)
    {
        var antwort = await _klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        antwort.EnsureSuccessStatusCode();
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        if (karte is null)
        {
            throw new InvalidOperationException("Die WebApi hat keine Karte zurückgegeben.");
        }

        return karte;
    }

    public void Dispose()
    {
        _klient.Dispose();
    }
}
