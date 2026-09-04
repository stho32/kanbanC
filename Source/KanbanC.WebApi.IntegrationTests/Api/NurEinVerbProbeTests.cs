using System.Net;
using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Earned Trust vor der ersten Adresse des einzelnen Kontributors: Sie trägt nur PUT. Beantwortet
// ASP.NET Core ein GET darauf mit 405 und leerem Rumpf — und zählt der Fehlervertrag dieses GET
// als ungeprüft, weil es in den registrierten Routen auftaucht? Davon hängt ab, ob der
// Location-Kopf des Anlegens auf eine Adresse zeigen darf, die kein GET beantwortet.
// Geprüft an der bestehenden Route /api/boards/{boardId}/kartenzahl, die schon heute nur PUT hat.
public class NurEinVerbProbeTests
{
    private const string BoardsRoute = "/api/boards";

    [Test]
    public async Task PROBE_Wenn_eine_Route_nur_PUT_traegt_dann_antwortet_ASP_NET_auf_GET_mit_405_und_leerem_Rumpf()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi);

        using var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{board.BoardId}/kartenzahl");

        var rumpf = await antwort.Content.ReadAsStringAsync();
        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));
        Assert.That(rumpf, Is.Empty);
    }

    [Test]
    public async Task PROBE_Wenn_eine_Route_nur_PUT_traegt_dann_steht_ihr_GET_nicht_in_den_registrierten_Routen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var routen = webApi.Routen;

        Assert.Multiple(() =>
        {
            Assert.That(routen, Does.Contain("PUT /api/boards/{boardId:long}/kartenzahl"));
            Assert.That(routen, Does.Not.Contain("GET /api/boards/{boardId:long}/kartenzahl"));
        });
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        if (board is null)
        {
            throw new InvalidOperationException("Die API hat kein Board zurückgegeben.");
        }

        return board;
    }
}
