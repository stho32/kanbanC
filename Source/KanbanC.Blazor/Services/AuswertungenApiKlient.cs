using System.Globalization;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.Blazor.Services;

// Der Weg der Oberfläche zu den Auswertungen — **derselbe**, den ein Agent ohne Browser geht.
// Die Antwort kommt gerechnet; die Oberfläche summiert nichts nach.
public sealed class AuswertungenApiKlient
{
    private const string KlientName = "KanbanC";
    private const string BoardsRoute = "api/boards";
    private const string Isodatumsformat = "yyyy-MM-dd";
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

    // Ohne Zeitraum die Standardachse: ein fehlendes „seit" ist kein Fehler, sondern die Vorgabe.
    public async Task<ApiErgebnis<Burndownauswertung>> LadeBurndown(long boardId, long kartenklasseId, DateOnly? seit)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.GetAsync($"{BoardsRoute}/{boardId}/kartenklassen/{kartenklasseId}/burndown{Zeitraumabfrage(seit)}");
        return await ApiAntwortleser.AlsErgebnisMitGemeldetenBefunden<Burndownauswertung>(antwort);
    }

    private static string Zeitraumabfrage(DateOnly? seit)
    {
        if (seit is null)
        {
            return string.Empty;
        }

        return $"?seit={seit.Value.ToString(Isodatumsformat, CultureInfo.InvariantCulture)}";
    }
}
