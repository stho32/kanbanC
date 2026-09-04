using System.Net;
using System.Net.Http.Json;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

public class KontributorenEndpunkteTests
{
    private const string KontributorenRoute = "/api/kontributoren";

    [Test]
    public async Task Wenn_ein_Kontributor_per_POST_angelegt_wird_dann_antwortet_die_API_mit_201_Location_und_KontributorId_1()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(kontributor, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(antwort.Headers.Location?.ToString(), Is.EqualTo(KontributorenRoute));
            Assert.That(kontributor.KontributorId, Is.EqualTo(1));
            Assert.That(kontributor.Name, Is.EqualTo("Stefan"));
            Assert.That(kontributor.Art, Is.EqualTo(Kontributorart.Mensch));
        });
    }

    [Test]
    public async Task Wenn_die_Art_als_Text_Agent_oder_Abgebildet_ankommt_dann_wird_sie_genauso_angelegt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var agent = await LegeAusRohemJson(webApi, """{"name":"Codex-Agent","art":"Agent"}""");
        var abgebildete = await LegeAusRohemJson(webApi, """{"name":"Nina Barth","art":"Abgebildet"}""");

        Assert.Multiple(() =>
        {
            Assert.That(agent.Art, Is.EqualTo(Kontributorart.Agent));
            Assert.That(abgebildete.Art, Is.EqualTo(Kontributorart.Abgebildet));
        });
    }

    [Test]
    public async Task Wenn_noch_kein_Kontributor_angelegt_ist_dann_liefert_GET_eine_leere_Liste_mit_200()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        using var antwort = await webApi.Klient.GetAsync(KontributorenRoute);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var kontributoren = await antwort.Content.ReadFromJsonAsync<List<Kontributor>>();
        Assert.That(kontributoren, Is.Empty);
    }

    [Test]
    public async Task Wenn_drei_Kontributoren_in_gemischter_Schreibweise_angelegt_sind_dann_liefert_GET_sie_alphabetisch_mit_allen_drei_Arten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("stefan", Kontributorart.Mensch));
        await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Codex-Agent", Kontributorart.Agent));
        await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Nina Barth", Kontributorart.Abgebildet));

        var kontributoren = await webApi.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);

        Assert.That(kontributoren, Is.EqualTo(new[]
        {
            new Kontributor(2, "Codex-Agent", Kontributorart.Agent),
            new Kontributor(3, "Nina Barth", Kontributorart.Abgebildet),
            new Kontributor(1, "stefan", Kontributorart.Mensch),
        }));
    }

    [Test]
    public async Task Wenn_zwei_Kontributoren_denselben_Namen_tragen_dann_weist_die_API_den_zweiten_nicht_zurueck()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anfrage = new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch);

        var erster = await LegeKontributorAn(webApi, anfrage);
        var zweiter = await LegeKontributorAn(webApi, anfrage);

        var kontributoren = await webApi.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        Assert.Multiple(() =>
        {
            Assert.That(erster.KontributorId, Is.EqualTo(1));
            Assert.That(zweiter.KontributorId, Is.EqualTo(2));
            Assert.That(kontributoren, Has.Count.EqualTo(2));
        });
    }


    [Test]
    public async Task Wenn_der_Name_leer_ist_dann_antwortet_die_API_mit_400_und_einem_Befund_statt_mit_500()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        using var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage("", Kontributorart.Mensch));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Kontributor anlegen ohne Name");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-name-leer"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("Name"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("POST /api/kontributoren"));
        });
    }

    [Test]
    public async Task Wenn_der_Name_nur_aus_Leerzeichen_besteht_dann_kommt_dieselbe_Antwort()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        using var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage("   ", Kontributorart.Agent));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kontributor-name-leer");
    }

    [Test]
    public async Task Wenn_eine_Anlage_zurueckgewiesen_wurde_dann_liefert_GET_unveraendert_die_Kontributoren_von_vorher()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var listeVorher = await webApi.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);

        using var abgewiesen = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage("", Kontributorart.Mensch));

        Assert.That(abgewiesen.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var listeNachher = await webApi.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        Assert.That(listeNachher, Is.EqualTo(listeVorher));
        Assert.That(listeNachher, Has.Count.EqualTo(1));
    }

    private static async Task<Kontributor> LegeAusRohemJson(TestWebApi webApi, string rumpf)
    {
        using var inhalt = new StringContent(rumpf, System.Text.Encoding.UTF8, "application/json");
        using var antwort = await webApi.Klient.PostAsync(KontributorenRoute, inhalt);
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(kontributor, Is.Not.Null);
        return kontributor;
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, KontributorAnlegenAnfrage anfrage)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, anfrage);
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(kontributor, Is.Not.Null);
        return kontributor;
    }
}
