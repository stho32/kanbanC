using System.Net;
using System.Text;
using System.Text.Json;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Probe der Frage, die vor dem Endpunkt der Kartenaenderung offen war: Kontributor.StillgelegtAm
// belegt, dass DateOnly? im **Lesen** traegt — ob es auch als **Eingabefeld** einer PUT-Anfrage
// sauber ankommt und wie ein leerer Wert ("" statt null) beantwortet wird, stand nirgends.
// Antwort: null kommt als null an, ein ISO-Text als DateOnly, und "" weist System.Text.Json ab —
// an der Route wird daraus eine 400 von ASP.NET selbst, ohne unseren Befund. Deshalb schickt die
// Oberflaeche fuer ein geleertes Datumsfeld null und nicht den leeren Text. Bleibt als
// Regressionsschutz stehen.
public class DateOnlyEingabeProbeTests
{
    private const string Kartenroute = "/api/karten";

    [Test]
    public void PROBE_Wenn_faelligAm_null_ist_dann_kommt_die_Anfrage_mit_null_an()
    {
        const string rumpf = """{"titel":"WBS-Import","beschreibung":null,"faelligAm":null,"farbe":"Ohne"}""";

        var anfrage = JsonSerializer.Deserialize<KarteAendernAnfrage>(rumpf, JsonSerializerOptions.Web);

        Assert.That(anfrage, Is.EqualTo(new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: null)));
    }

    [Test]
    public void PROBE_Wenn_faelligAm_einen_ISO_Text_traegt_dann_kommt_die_Anfrage_mit_einem_DateOnly_an()
    {
        const string rumpf = """{"titel":"WBS-Import","beschreibung":null,"faelligAm":"2026-09-02","farbe":"Ohne"}""";

        var anfrage = JsonSerializer.Deserialize<KarteAendernAnfrage>(rumpf, JsonSerializerOptions.Web);

        Assert.That(anfrage!.FaelligAm, Is.EqualTo(new DateOnly(2026, 9, 2)));
    }

    [Test]
    public void PROBE_Wenn_faelligAm_der_leere_Text_ist_dann_weist_System_Text_Json_ihn_ab()
    {
        const string rumpf = """{"titel":"WBS-Import","beschreibung":null,"faelligAm":"","farbe":"Ohne"}""";

        var fehler = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<KarteAendernAnfrage>(rumpf, JsonSerializerOptions.Web));

        Assert.That(fehler!.InnerException, Is.TypeOf<FormatException>());
    }

    // Ueber die Route wird daraus eine 400 von ASP.NET selbst — ohne unseren Rumpf, wie bei der
    // unbekannten Kontributorart (KontributorartProbeTests).
    [Test]
    public async Task PROBE_Wenn_faelligAm_an_der_Route_der_leere_Text_ist_dann_antwortet_ASP_NET_selbst_ohne_unseren_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        using var inhalt = new StringContent("""{"titel":"WBS-Import","faelligAm":"","farbe":"Ohne"}""", Encoding.UTF8, "application/json");

        using var antwort = await webApi.Klient.PutAsync($"{Kartenroute}/1", inhalt);

        var rumpf = await antwort.Content.ReadAsStringAsync();
        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(rumpf, Does.Not.Contain("befunde"));
    }
}
