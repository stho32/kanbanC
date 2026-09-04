using System.Net;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.Blazor.Tests.Services;

public class KontributorenApiKlientTests
{
    private const string JsonTyp = "application/json";

    [Test]
    public async Task Wenn_die_WebApi_den_angelegten_Kontributor_liefert_dann_steht_er_im_Ergebnis()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.Created,
            """{"kontributorId":1,"name":"Stefan","art":"Mensch"}""",
            JsonTyp);
        var klient = new KontributorenApiKlient(fabrik);

        var ergebnis = await klient.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.That(ergebnis.Wert, Is.EqualTo(new Kontributor(1, "Stefan", Kontributorart.Mensch)));
    }

    [Test]
    public async Task Wenn_ein_Agent_angelegt_wird_dann_trifft_der_Aufruf_die_Anlegeroute_und_traegt_die_gewaehlte_Art()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.Created,
            """{"kontributorId":2,"name":"Codex-Agent","art":"Agent"}""",
            JsonTyp);
        var klient = new KontributorenApiKlient(fabrik);

        await klient.LegeAn(new KontributorAnlegenAnfrage("Codex-Agent", Kontributorart.Agent));

        Assert.Multiple(() =>
        {
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("POST http://webapi.test/api/kontributoren"));
            Assert.That(fabrik.GesendeterRumpf, Is.EqualTo("""{"name":"Codex-Agent","art":"Agent"}"""));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Anlage_zurueckweist_dann_stehen_ihre_Befunde_im_Ergebnis()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.BadRequest,
            """
            {"befunde":[
              {"code":"kontributor-name-leer","meldung":"Der Name darf nicht leer sein.","kompensation":"POST /api/kontributoren mit nichtleerem Namen wiederholen."}
            ]}
            """,
            JsonTyp);
        var klient = new KontributorenApiKlient(fabrik);

        var ergebnis = await klient.LegeAn(new KontributorAnlegenAnfrage("", Kontributorart.Mensch));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-name-leer"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Is.EqualTo("Der Name darf nicht leer sein."));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_bei_einer_Zurueckweisung_keinen_lesbaren_Rumpf_liefert_dann_traegt_das_Ergebnis_trotzdem_einen_Befund()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.BadRequest);
        var klient = new KontributorenApiKlient(fabrik);

        var ergebnis = await klient.LegeAn(new KontributorAnlegenAnfrage("", Kontributorart.Mensch));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Wenn_die_Kontributoren_geladen_werden_dann_trifft_der_Aufruf_die_Listenroute_und_liefert_sie_in_der_Reihenfolge_der_WebApi()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.OK,
            """
            [{"kontributorId":2,"name":"Codex-Agent","art":"Agent"},
             {"kontributorId":1,"name":"stefan","art":"Mensch"}]
            """,
            JsonTyp);
        var klient = new KontributorenApiKlient(fabrik);

        var kontributoren = await klient.LadeAlle();

        Assert.Multiple(() =>
        {
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("GET http://webapi.test/api/kontributoren"));
            Assert.That(kontributoren, Is.EqualTo(new[]
            {
                new Kontributor(2, "Codex-Agent", Kontributorart.Agent),
                new Kontributor(1, "stefan", Kontributorart.Mensch),
            }));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_null_statt_einer_Liste_liefert_dann_kommt_eine_leere_Liste_zurueck()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, "null", JsonTyp);
        var klient = new KontributorenApiKlient(fabrik);

        var kontributoren = await klient.LadeAlle();

        Assert.That(kontributoren, Is.Empty);
    }
}
