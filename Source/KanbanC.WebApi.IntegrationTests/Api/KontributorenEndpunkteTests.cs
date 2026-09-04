using System.Net;
using System.Net.Http.Json;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

public class KontributorenEndpunkteTests
{
    private const string KontributorenRoute = "/api/kontributoren";

    [Test]
    public async Task Wenn_ein_Kontributor_per_POST_angelegt_wird_dann_zeigt_der_Location_Kopf_auf_seine_eigene_Adresse()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(kontributor, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(antwort.Headers.Location?.ToString(), Is.EqualTo($"{KontributorenRoute}/1"));
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
            new Kontributor(2, "Codex-Agent", Kontributorart.Agent, StillgelegtAm: null),
            new Kontributor(3, "Nina Barth", Kontributorart.Abgebildet, StillgelegtAm: null),
            new Kontributor(1, "stefan", Kontributorart.Mensch, StillgelegtAm: null),
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

    [Test]
    public async Task Wenn_ein_Kontributor_per_PUT_geaendert_wird_dann_antwortet_die_API_mit_200_und_dem_neuen_Stand()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var bert = await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));

        using var antwort = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{bert.KontributorId}", new KontributorAendernAnfrage("Zora", Kontributorart.Mensch));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var geaenderter = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(geaenderter, Is.EqualTo(new Kontributor(bert.KontributorId, "Zora", Kontributorart.Mensch, StillgelegtAm: null)));
    }

    [Test]
    public async Task Wenn_ein_Kontributor_geaendert_wurde_dann_liefert_GET_ihn_an_seiner_neuen_alphabetischen_Stelle_und_die_uebrigen_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));
        var bert = await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));
        await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Cara", Kontributorart.Abgebildet));

        using var geaendert = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{bert.KontributorId}", new KontributorAendernAnfrage("Zora", Kontributorart.Mensch));

        geaendert.EnsureSuccessStatusCode();
        var kontributoren = await webApi.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        Assert.That(kontributoren, Is.EqualTo(new[]
        {
            new Kontributor(1, "Anna", Kontributorart.Mensch, StillgelegtAm: null),
            new Kontributor(3, "Cara", Kontributorart.Abgebildet, StillgelegtAm: null),
            new Kontributor(2, "Zora", Kontributorart.Mensch, StillgelegtAm: null),
        }));
    }

    [Test]
    public async Task Wenn_alle_drei_Arten_als_Ziel_gewaehlt_werden_dann_uebernimmt_die_API_jede_von_ihnen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var kontributor = await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Bert", Kontributorart.Mensch));

        var zumAgenten = await AendereKontributor(webApi, kontributor.KontributorId, new KontributorAendernAnfrage("Bert", Kontributorart.Agent));
        var zumAbgebildeten = await AendereKontributor(webApi, kontributor.KontributorId, new KontributorAendernAnfrage("Bert", Kontributorart.Abgebildet));
        var zumMenschen = await AendereKontributor(webApi, kontributor.KontributorId, new KontributorAendernAnfrage("Bert", Kontributorart.Mensch));

        Assert.Multiple(() =>
        {
            Assert.That(zumAgenten.Art, Is.EqualTo(Kontributorart.Agent));
            Assert.That(zumAbgebildeten.Art, Is.EqualTo(Kontributorart.Abgebildet));
            Assert.That(zumMenschen.Art, Is.EqualTo(Kontributorart.Mensch));
        });
    }

    [Test]
    public async Task Wenn_beim_Aendern_der_Name_leer_ist_dann_antwortet_die_API_mit_400_und_nennt_die_Aenderungsroute_als_naechsten_Schritt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var bert = await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));

        using var antwort = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{bert.KontributorId}", new KontributorAendernAnfrage("", Kontributorart.Mensch));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Kontributor ändern ohne Name");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-name-leer"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain($"PUT /api/kontributoren/{bert.KontributorId}"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Not.Contain("POST"));
        });
    }

    [Test]
    public async Task Wenn_beim_Aendern_der_Name_nur_aus_Leerzeichen_besteht_dann_kommt_dieselbe_Antwort_und_nichts_ist_geschrieben()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var bert = await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));

        using var antwort = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{bert.KontributorId}", new KontributorAendernAnfrage("   ", Kontributorart.Abgebildet));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kontributor-name-leer");
        var kontributoren = await webApi.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        Assert.That(kontributoren, Is.EqualTo(new[] { new Kontributor(bert.KontributorId, "Bert", Kontributorart.Agent, StillgelegtAm: null) }));
    }

    [Test]
    public async Task Wenn_die_KontributorId_unbekannt_ist_dann_antwortet_die_API_mit_404_und_einem_Befund_der_die_Nummer_nennt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));

        using var antwort = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/999", new KontributorAendernAnfrage("Zora", Kontributorart.Mensch));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Kontributor ändern mit unbekannter KontributorId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("GET /api/kontributoren"));
        });
        var kontributoren = await webApi.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        Assert.That(kontributoren, Is.EqualTo(new[] { new Kontributor(1, "Bert", Kontributorart.Agent, StillgelegtAm: null) }));
    }

    [Test]
    public async Task Wenn_die_KontributorId_unbekannt_und_der_Name_leer_ist_dann_antwortet_die_API_mit_400_statt_mit_einem_Serverfehler()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        using var antwort = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/999", new KontributorAendernAnfrage("", Kontributorart.Mensch));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kontributor-name-leer");
    }

    [Test]
    public async Task Wenn_eine_Stilllegung_per_PUT_gesetzt_wird_dann_antwortet_die_API_mit_200_und_gesetztem_StillgelegtAm()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anna = await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));
        Assert.That(anna.StillgelegtAm, Is.Null);

        using var antwort = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{anna.KontributorId}/stilllegung", new Stilllegung(true));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var stillgelegte = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(stillgelegte, Is.EqualTo(new Kontributor(anna.KontributorId, "Anna", Kontributorart.Mensch, DateOnly.FromDateTime(DateTime.Today))));
    }

    [Test]
    public async Task Wenn_eine_Stilllegung_zurueckgenommen_wird_dann_antwortet_die_API_mit_200_und_StillgelegtAm_null()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anna = await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));
        await SetzeStilllegung(webApi, anna.KontributorId, new Stilllegung(true));

        using var antwort = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{anna.KontributorId}/stilllegung", new Stilllegung(false));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var zurueckgeholte = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(zurueckgeholte, Is.EqualTo(new Kontributor(anna.KontributorId, "Anna", Kontributorart.Mensch, StillgelegtAm: null)));
    }

    // Das Rechenbeispiel des Akzeptanzkriteriums: zweimal true, zweimal false, einmal true.
    [Test]
    public async Task Wenn_dieselbe_Richtung_zweimal_gesetzt_wird_dann_aendert_sich_nach_dem_ersten_Mal_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anna = await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));

        var erste = await SetzeStilllegung(webApi, anna.KontributorId, new Stilllegung(true));
        var zweite = await SetzeStilllegung(webApi, anna.KontributorId, new Stilllegung(true));
        await SetzeStilllegung(webApi, anna.KontributorId, new Stilllegung(false));
        var zurueckgeholte = await SetzeStilllegung(webApi, anna.KontributorId, new Stilllegung(false));
        var letzte = await SetzeStilllegung(webApi, anna.KontributorId, new Stilllegung(true));

        Assert.Multiple(() =>
        {
            Assert.That(zweite, Is.EqualTo(erste));
            Assert.That(zurueckgeholte.StillgelegtAm, Is.Null);
            Assert.That(letzte.StillgelegtAm, Is.EqualTo(DateOnly.FromDateTime(DateTime.Today)));
        });
        var kontributoren = await webApi.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        Assert.That(kontributoren, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Wenn_jemand_stillgelegt_ist_dann_traegt_jede_Zeile_der_Liste_ihr_StillgelegtAm_und_die_Stillgelegten_stehen_hinten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anna = await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));
        await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));
        await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Cem", Kontributorart.Abgebildet));

        await SetzeStilllegung(webApi, anna.KontributorId, new Stilllegung(true));

        var kontributoren = await webApi.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        Assert.That(kontributoren, Is.EqualTo(new[]
        {
            new Kontributor(2, "Bert", Kontributorart.Agent, StillgelegtAm: null),
            new Kontributor(3, "Cem", Kontributorart.Abgebildet, StillgelegtAm: null),
            new Kontributor(1, "Anna", Kontributorart.Mensch, DateOnly.FromDateTime(DateTime.Today)),
        }));
    }

    [Test]
    public async Task Wenn_die_KontributorId_der_Stilllegung_unbekannt_ist_dann_antwortet_die_API_mit_404_und_einem_Befund_der_die_Nummer_nennt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));

        using var antwort = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/4711/stilllegung", new Stilllegung(true));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Stilllegung schalten mit unbekannter KontributorId");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("4711"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("GET /api/kontributoren"));
        });
        var kontributoren = await webApi.Klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        Assert.That(kontributoren, Is.EqualTo(new[] { new Kontributor(1, "Anna", Kontributorart.Mensch, StillgelegtAm: null) }));
    }

    // Keine Sperre der Bearbeitung: I0007 bleibt an einem Stillgelegten unverändert nutzbar, und
    // sein Stilllegungsstand bleibt dabei unangetastet.
    [Test]
    public async Task Wenn_ein_stillgelegter_Kontributor_umbenannt_wird_dann_gelingt_das_und_sein_Stilllegungsstand_bleibt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var anna = await LegeKontributorAn(webApi, new KontributorAnlegenAnfrage("Anna", Kontributorart.Mensch));
        var stillgelegte = await SetzeStilllegung(webApi, anna.KontributorId, new Stilllegung(true));

        var umbenannte = await AendereKontributor(webApi, anna.KontributorId, new KontributorAendernAnfrage("Zora", Kontributorart.Agent));

        Assert.That(umbenannte, Is.EqualTo(new Kontributor(anna.KontributorId, "Zora", Kontributorart.Agent, stillgelegte.StillgelegtAm)));
    }

    private static async Task<Kontributor> SetzeStilllegung(TestWebApi webApi, long kontributorId, Stilllegung stilllegung)
    {
        using var antwort = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{kontributorId}/stilllegung", stilllegung);
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(kontributor, Is.Not.Null);
        return kontributor;
    }

    private static async Task<Kontributor> AendereKontributor(TestWebApi webApi, long kontributorId, KontributorAendernAnfrage anfrage)
    {
        using var antwort = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{kontributorId}", anfrage);
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(kontributor, Is.Not.Null);
        return kontributor;
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
