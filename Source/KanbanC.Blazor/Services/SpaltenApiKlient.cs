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
        return await AlsSpaltenErgebnis(antwort);
    }

    public async Task<ApiErgebnis<Spalte>> AendereSpalte(long boardId, long spalteId, SpalteAendernAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{SpaltenRoute(boardId)}/{spalteId}", anfrage);
        return await AlsSpaltenErgebnis(antwort);
    }

    private static string SpaltenRoute(long boardId)
    {
        return $"{BoardsRoute}/{boardId}/spalten";
    }

    private static async Task<ApiErgebnis<Spalte>> AlsSpaltenErgebnis(HttpResponseMessage antwort)
    {
        var anfrageWurdeZurueckgewiesen = antwort.StatusCode == HttpStatusCode.BadRequest;
        if (anfrageWurdeZurueckgewiesen)
        {
            var zurueckweisung = await Zurueckweisungsleser.Lies(antwort);
            return ApiErgebnis<Spalte>.Zurueckgewiesen(zurueckweisung);
        }

        var spalteIstUnbekannt = antwort.StatusCode == HttpStatusCode.NotFound;
        if (spalteIstUnbekannt)
        {
            return ApiErgebnis<Spalte>.Zurueckgewiesen(SpalteOderBoardVerschwunden);
        }

        antwort.EnsureSuccessStatusCode();
        var spalte = await antwort.Content.ReadFromJsonAsync<Spalte>();
        if (spalte is null)
        {
            throw new InvalidOperationException("Die WebApi hat keine Spalte zurückgegeben.");
        }

        return ApiErgebnis<Spalte>.Erfolg(spalte);
    }
}
