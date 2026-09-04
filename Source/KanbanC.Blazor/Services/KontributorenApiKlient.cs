using System.Net;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Services;

public sealed class KontributorenApiKlient
{
    private const string KlientName = "KanbanC";
    private const string KontributorenRoute = "api/kontributoren";
    private readonly IHttpClientFactory _klientFabrik;

    public KontributorenApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
    }

    public async Task<IReadOnlyList<Kontributor>> LadeAlle()
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        var kontributoren = await klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        if (kontributoren is null)
        {
            return [];
        }

        return kontributoren;
    }

    public async Task<ApiErgebnis<Kontributor>> LegeAn(KontributorAnlegenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync(KontributorenRoute, anfrage);
        var anfrageWurdeZurueckgewiesen = antwort.StatusCode == HttpStatusCode.BadRequest;
        if (anfrageWurdeZurueckgewiesen)
        {
            var zurueckweisung = await Zurueckweisungsleser.Lies(antwort);
            return ApiErgebnis<Kontributor>.Zurueckgewiesen(zurueckweisung);
        }

        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        if (kontributor is null)
        {
            throw new InvalidOperationException("Die WebApi hat keinen Kontributor zurückgegeben.");
        }

        return ApiErgebnis<Kontributor>.Erfolg(kontributor);
    }
}
