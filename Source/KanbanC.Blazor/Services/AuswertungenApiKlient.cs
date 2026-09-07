using KanbanC.Contracts.Auswertungen;

namespace KanbanC.Blazor.Services;

// Der Weg der Oberfläche zum Soll-Ist-Vergleich — **derselbe**, den ein Agent ohne Browser geht.
// Die Antwort kommt gerechnet; die Oberfläche summiert nichts nach.
public sealed class AuswertungenApiKlient
{
    private const string KlientName = "KanbanC";
    private const string BoardsRoute = "api/boards";
    private readonly IHttpClientFactory _klientFabrik;

    public AuswertungenApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
    }

    public async Task<ApiErgebnis<SollIstAuswertung>> LadeSollIst(long boardId, long kartenklasseId)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.GetAsync($"{BoardsRoute}/{boardId}/kartenklassen/{kartenklasseId}/soll-ist");
        return await ApiAntwortleser.AlsErgebnisMitGemeldetenBefunden<SollIstAuswertung>(antwort);
    }
}
