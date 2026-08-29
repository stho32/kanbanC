using System.Text.Json;
using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Services;

public static class Zurueckweisungsleser
{
    private static readonly Zurueckweisung OhneLesbareBefunde =
        new(["Die WebApi hat die Anfrage zurückgewiesen (HTTP 400)."]);

    public static async Task<Zurueckweisung> Lies(HttpResponseMessage antwort)
    {
        var gemeldeteZurueckweisung = await LiesAusRumpf(antwort);
        if (gemeldeteZurueckweisung is null)
        {
            return OhneLesbareBefunde;
        }

        var befundeFehlen = gemeldeteZurueckweisung.Befunde is null || gemeldeteZurueckweisung.Befunde.Count == 0;
        if (befundeFehlen)
        {
            return OhneLesbareBefunde;
        }

        return gemeldeteZurueckweisung;
    }

    private static async Task<Zurueckweisung?> LiesAusRumpf(HttpResponseMessage antwort)
    {
        var rumpfIstLeer = antwort.Content.Headers.ContentLength == 0;
        if (rumpfIstLeer)
        {
            return null;
        }

        try
        {
            return await antwort.Content.ReadFromJsonAsync<Zurueckweisung>();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
