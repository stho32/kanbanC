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
        Assert.That(ergebnis.Wert, Is.EqualTo(new Kontributor(1, "Stefan", Kontributorart.Mensch, StillgelegtAm: null)));
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
                new Kontributor(2, "Codex-Agent", Kontributorart.Agent, StillgelegtAm: null),
                new Kontributor(1, "stefan", Kontributorart.Mensch, StillgelegtAm: null),
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

    [Test]
    public void Wenn_die_WebApi_auf_das_Anlegen_einen_leeren_Kontributor_liefert_dann_faellt_das_auf()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created, "null", JsonTyp);
        var klient = new KontributorenApiKlient(fabrik);

        Assert.That(
            async () => await klient.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch)),
            Throws.InvalidOperationException.With.Message.Contains("keinen Kontributor"));
    }

    [Test]
    public void Wenn_die_WebApi_beim_Anlegen_mit_einem_Serverfehler_antwortet_dann_schlaegt_der_Aufruf_durch()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new KontributorenApiKlient(fabrik);

        Assert.That(
            async () => await klient.LegeAn(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch)),
            Throws.InstanceOf<HttpRequestException>());
    }

    [Test]
    public async Task Wenn_ein_Kontributor_geaendert_wird_dann_trifft_der_Aufruf_seine_Adresse_mit_PUT_und_traegt_Name_und_Art()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.OK,
            """{"kontributorId":2,"name":"Zora","art":"Mensch"}""",
            JsonTyp);
        var klient = new KontributorenApiKlient(fabrik);

        var ergebnis = await klient.Aendere(2, new KontributorAendernAnfrage("Zora", Kontributorart.Mensch));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("PUT http://webapi.test/api/kontributoren/2"));
            Assert.That(fabrik.GesendeterRumpf, Is.EqualTo("""{"name":"Zora","art":"Mensch"}"""));
            Assert.That(ergebnis.Wert, Is.EqualTo(new Kontributor(2, "Zora", Kontributorart.Mensch, StillgelegtAm: null)));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Aenderung_mit_400_zurueckweist_dann_stehen_ihre_Befunde_im_Ergebnis()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.BadRequest,
            """
            {"befunde":[
              {"code":"kontributor-name-leer","meldung":"Der Name darf nicht leer sein.","kompensation":"`PUT /api/kontributoren/2` mit einem nichtleeren „name“ wiederholen."}
            ]}
            """,
            JsonTyp);
        var klient = new KontributorenApiKlient(fabrik);

        var ergebnis = await klient.Aendere(2, new KontributorAendernAnfrage("", Kontributorart.Mensch));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-name-leer"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Kompensation, Does.Contain("PUT /api/kontributoren/2"));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Aenderung_mit_404_beantwortet_dann_steht_ihr_eigener_Befund_da_und_nicht_die_Board_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(
            HttpStatusCode.NotFound,
            """
            {"befunde":[
              {"code":"kontributor-unbekannt","meldung":"Einen Kontributor mit der Nummer 999 gibt es nicht.","kompensation":"`GET /api/kontributoren` abrufen."}
            ]}
            """,
            JsonTyp);
        var klient = new KontributorenApiKlient(fabrik);

        var ergebnis = await klient.Aendere(999, new KontributorAendernAnfrage("Zora", Kontributorart.Mensch));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("Board"));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_bei_einer_abgewiesenen_Aenderung_keinen_lesbaren_Rumpf_liefert_dann_traegt_das_Ergebnis_trotzdem_einen_Befund()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.NotFound);
        var klient = new KontributorenApiKlient(fabrik);

        var ergebnis = await klient.Aendere(999, new KontributorAendernAnfrage("Zora", Kontributorart.Mensch));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde, Has.Count.EqualTo(1));
    }

    [Test]
    public void Wenn_die_WebApi_auf_die_Aenderung_einen_leeren_Kontributor_liefert_dann_faellt_das_auf()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, "null", JsonTyp);
        var klient = new KontributorenApiKlient(fabrik);

        Assert.That(
            async () => await klient.Aendere(2, new KontributorAendernAnfrage("Zora", Kontributorart.Mensch)),
            Throws.InvalidOperationException.With.Message.Contains("keinen Kontributor"));
    }

    [Test]
    public void Wenn_die_WebApi_beim_Aendern_mit_einem_Serverfehler_antwortet_dann_schlaegt_der_Aufruf_durch()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new KontributorenApiKlient(fabrik);

        Assert.That(
            async () => await klient.Aendere(2, new KontributorAendernAnfrage("Zora", Kontributorart.Mensch)),
            Throws.InstanceOf<HttpRequestException>());
    }
}
