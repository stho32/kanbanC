using System.Net;
using System.Text;
using System.Text.Json;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Earned Trust fuer JsonStringEnumConverter: Erreicht ein unbekannter Text in "art" den Handler,
// oder weist ihn die Deserialisierung vorher ab? Davon haengt ab, ob der Validator die unbekannte
// Kontributorart ueberhaupt aus einem JSON-Rumpf zu sehen bekommt.
public class KontributorartProbeTests
{
    private const string KontributorenRoute = "/api/kontributoren";

    [Test]
    public void PROBE_Wenn_die_Art_einen_unbekannten_Text_traegt_dann_scheitert_die_Deserialisierung()
    {
        var rumpf = """{"name":"Stefan","art":"Chef"}""";

        Assert.That(
            () => JsonSerializer.Deserialize<KontributorAnlegenAnfrage>(rumpf, JsonSerializerOptions.Web),
            Throws.TypeOf<JsonException>());
    }

    [Test]
    public void PROBE_Wenn_die_Art_einen_bekannten_Text_traegt_dann_kommt_sie_als_Aufzaehlungswert_an()
    {
        var rumpf = """{"name":"Codex-Agent","art":"Agent"}""";

        var anfrage = JsonSerializer.Deserialize<KontributorAnlegenAnfrage>(rumpf, JsonSerializerOptions.Web);

        Assert.That(anfrage, Is.EqualTo(new KontributorAnlegenAnfrage("Codex-Agent", Kontributorart.Agent)));
    }

    [Test]
    public async Task PROBE_Wenn_die_Art_an_der_Route_einen_unbekannten_Text_traegt_dann_antwortet_ASP_NET_selbst_ohne_unseren_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        using var inhalt = new StringContent("""{"name":"Stefan","art":"Chef"}""", Encoding.UTF8, "application/json");

        using var antwort = await webApi.Klient.PostAsync(KontributorenRoute, inhalt);

        var rumpf = await antwort.Content.ReadAsStringAsync();
        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(rumpf, Does.Not.Contain("befunde"));
    }
}
