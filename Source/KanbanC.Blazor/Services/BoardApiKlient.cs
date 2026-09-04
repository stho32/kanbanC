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

    public async Task<IReadOnlyList<BoardUebersicht>> LadeAlleBoards(Archivierung archivstand)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        var boards = await klient.GetFromJsonAsync<List<BoardUebersicht>>(Listenadresse(archivstand));
        if (boards is null)
        {
            return [];
        }

        return boards;
    }

    // Ohne Parameter liefert die WebApi die aktiven Boards; die Standardliste bleibt damit die
    // Standardadresse.
    private static string Listenadresse(Archivierung archivstand)
    {
        if (archivstand.IstArchiviert)
        {
            return $"{BoardsRoute}?archiviert=true";
        }

        return BoardsRoute;
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

    public async Task<Board?> SchalteKartenzahl(long boardId, Kartenzahlanzeige anzeige)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/kartenzahl", anzeige);
        var boardIstUnbekannt = antwort.StatusCode == HttpStatusCode.NotFound;
        if (boardIstUnbekannt)
        {
            return null;
        }

        antwort.EnsureSuccessStatusCode();
        return await antwort.Content.ReadFromJsonAsync<Board>();
    }

    public async Task<ApiErgebnis<Board>> SchalteArchivierung(long boardId, Archivierung archivierung)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/archivierung", archivierung);
        return await ApiAntwortleser.AlsErgebnis<Board>(antwort);
    }

    // 400 und 404 tragen beide eine Zurueckweisung mit Befund und laufen denselben Weg — die
    // Oberflaeche zeigt in beiden Lagen dieselbe Meldung an der Kachel.
    public async Task<ApiErgebnis<Board>> BenenneUm(long boardId, BoardUmbenennenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}", anfrage);
        return await ApiAntwortleser.AlsErgebnis<Board>(antwort);
    }

    public async Task<ApiErgebnis<Board>> LegeBoardAn(BoardAnlegenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync(BoardsRoute, anfrage);
        var anfrageWurdeZurueckgewiesen = antwort.StatusCode == HttpStatusCode.BadRequest;
        if (anfrageWurdeZurueckgewiesen)
        {
            var zurueckweisung = await Zurueckweisungsleser.Lies(antwort);
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
}
