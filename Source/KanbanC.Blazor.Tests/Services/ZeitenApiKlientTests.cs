using System.Net;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.Blazor.Tests.Services;

// Diese Fehlerpfade sind über den Browser nicht auslösbar — der Grund, aus dem es dieses
// Testprojekt gibt.
public class ZeitenApiKlientTests
{
    private const string JsonInhaltstyp = "application/json";
    private const string LaufenderEintrag = """{"zeiteintragId":7,"karte":14,"kontributor":{"kontributorId":3,"name":"Stefan","art":"Mensch","stillgelegtAm":null},"beginn":"2026-09-06T08:04:00+00:00","ende":null}""";

    [Test]
    public async Task Wenn_die_WebApi_den_Timer_startet_dann_traegt_das_Ergebnis_den_Eintrag_ohne_Ende()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created, LaufenderEintrag, JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.ZeiteintragId, Is.EqualTo(7));
            Assert.That(ergebnis.Wert.Karte, Is.EqualTo(14));
            Assert.That(ergebnis.Wert.Kontributor.Name, Is.EqualTo("Stefan"));
            Assert.That(ergebnis.Wert.Ende, Is.Null);
        });
    }

    // 200 heißt „er lief schon" — für die Oberfläche derselbe Weg wie 201, denn sie zeigt danach
    // so oder so „läuft seit". Der Unterschied ist die Auskunft an den Agenten.
    [Test]
    public async Task Wenn_die_WebApi_mit_200_antwortet_dann_traegt_das_Ergebnis_denselben_Eintrag_wie_bei_201()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, LaufenderEintrag, JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.That(ergebnis.Wert.ZeiteintragId, Is.EqualTo(7));
    }

    // Die Adresse endet auf „laufend" und nicht auf „zeiten": POST …/zeiten gehört dem
    // Nachtragen. Genau diese Abweichung wäre im Browser nicht zu sehen.
    [Test]
    public async Task Wenn_der_Timer_gestartet_wird_dann_lautet_die_Adresse_zeiten_laufend_und_der_Rumpf_traegt_nur_den_Kontributor()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created, LaufenderEintrag, JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        await klient.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3));

        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("POST http://webapi.test/api/karten/14/zeiten/laufend"));
        Assert.That(fabrik.GesendeterRumpf, Is.EqualTo("""{"kontributor":3}"""));
    }

    [Test]
    public async Task Wenn_die_WebApi_den_stillgelegten_Kontributor_zurueckweist_dann_steht_ihr_Befund_im_Ergebnis()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest,
            """{"befunde":[{"code":"kontributor-stillgelegt","meldung":"Der Kontributor mit der Nummer 5 ist stillgelegt und kann keine Zeit mehr erfassen.","kompensation":"`GET /api/kontributoren` abrufen."}]}""",
            JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(5));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-stillgelegt"));
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("Zeit"));
        Assert.That(() => ergebnis.Wert, Throws.InvalidOperationException);
    }

    // Der 404 trägt den **eigenen** Befund der Route und wird nicht durch eine Board-Meldung
    // ersetzt: diese Route kennt kein Board.
    [Test]
    public async Task Wenn_die_WebApi_404_mit_Befund_meldet_dann_bleibt_ihr_Befund_erhalten()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.NotFound,
            """{"befunde":[{"code":"karte-unbekannt","meldung":"Eine Karte mit der Nummer 999 gibt es nicht.","kompensation":"`GET /api/boards` abrufen."}]}""",
            JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.StarteZeitmessung(999, new ZeitmessungStartenAnfrage(3));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("999"));
    }

    [Test]
    public async Task Wenn_die_WebApi_kein_JSON_liefert_dann_traegt_die_Zurueckweisung_eine_lesbare_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, "<html>Fehler</html>", "text/html");
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("HTTP 400"));
    }

    // Ein Serverfehler ist keine Zurückweisung: er trägt keinen Befund und darf nicht als einer
    // erscheinen.
    [Test]
    public void Wenn_die_WebApi_mit_500_antwortet_dann_scheitert_der_Aufruf_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new ZeitenApiKlient(fabrik);

        Assert.That(async () => await klient.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3)),
            Throws.TypeOf<HttpRequestException>());
    }

    [Test]
    public void Wenn_die_WebApi_einen_leeren_Rumpf_liefert_dann_scheitert_der_Aufruf_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created, "null", JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        Assert.That(async () => await klient.StarteZeitmessung(14, new ZeitmessungStartenAnfrage(3)),
            Throws.InvalidOperationException);
    }
}
