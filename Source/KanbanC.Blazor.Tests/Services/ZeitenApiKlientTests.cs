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
    private const string BeendeterEintrag = """{"zeiteintragId":7,"karte":14,"kontributor":{"kontributorId":3,"name":"Stefan","art":"Mensch","stillgelegtAm":null},"beginn":"2026-09-06T08:04:00+00:00","ende":"2026-09-06T09:40:00+00:00"}""";
    private const string LeeresKartendetail = """{"karte":{"karteId":14,"titel":"Migration schreiben","spalte":1,"beschreibung":null,"faelligAm":null,"startAm":null,"farbe":"Ohne","kontributor":null,"kartennummer":null},"board":1,"boardname":"Entwicklung","spalte":1,"spaltenbezeichnung":"Backlog","verantwortlicher":null,"etiketten":[],"etikettvorschlaege":[],"teilaufgaben":[],"kommentare":[],"anhaenge":[],"dateiverweise":[],"kartenklasse":null,"zeiteintraege":[]}""";
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

    // Deckt auch den zweiten Stopp ab: der Klient ist zustandslos, und die WebApi antwortet dort
    // mit demselben 200 und demselben Eintrag. Ein zweiter Aufruf sagte hier nichts Neues.
    [Test]
    public async Task Wenn_die_WebApi_den_Timer_beendet_dann_traegt_das_Ergebnis_den_Eintrag_mit_Ende()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, BeendeterEintrag, JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.BeendeZeitmessung(14, 7);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.ZeiteintragId, Is.EqualTo(7));
            Assert.That(ergebnis.Wert.Kontributor.KontributorId, Is.EqualTo(3));
            Assert.That(ergebnis.Wert.Beginn, Is.EqualTo(new DateTimeOffset(2026, 9, 6, 8, 4, 0, TimeSpan.Zero)));
            Assert.That(ergebnis.Wert.Ende, Is.EqualTo(new DateTimeOffset(2026, 9, 6, 9, 40, 0, TimeSpan.Zero)));
        });
    }

    // Die Adresse endet auf „ende", und der Aufruf schickt **keinen Rumpf** — weder einen
    // Kontributor noch einen Zeitpunkt. Genau das wäre im Browser nicht zu sehen.
    [Test]
    public async Task Wenn_der_Timer_beendet_wird_dann_lautet_die_Adresse_zeiten_nummer_ende_und_es_geht_kein_Rumpf_mit()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, BeendeterEintrag, JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        await klient.BeendeZeitmessung(14, 7);

        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("PUT http://webapi.test/api/karten/14/zeiten/7/ende"));
        Assert.That(fabrik.GesendeterRumpf, Is.Null);
    }

    [Test]
    public async Task Wenn_die_WebApi_den_unbekannten_Zeiteintrag_zurueckweist_dann_steht_ihr_Befund_im_Ergebnis()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.NotFound,
            """{"befunde":[{"code":"zeiteintrag-unbekannt","meldung":"Einen Zeiteintrag mit der Nummer 777 gibt es an der Karte 14 nicht.","kompensation":"`GET /api/karten/14` abrufen."}]}""",
            JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.BeendeZeitmessung(14, 777);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("zeiteintrag-unbekannt"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("777"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Kompensation, Does.Contain("/api/karten/14"));
        });
    }

    // Ein Serverfehler ist auch beim Stopp keine Zurückweisung.
    [Test]
    public void Wenn_die_WebApi_den_Stopp_mit_500_beantwortet_dann_scheitert_der_Aufruf_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new ZeitenApiKlient(fabrik);

        Assert.That(async () => await klient.BeendeZeitmessung(14, 7), Throws.TypeOf<HttpRequestException>());
    }

    [Test]
    public void Wenn_die_WebApi_beim_Stopp_einen_leeren_Rumpf_liefert_dann_scheitert_der_Aufruf_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, "null", JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        Assert.That(async () => await klient.BeendeZeitmessung(14, 7), Throws.InvalidOperationException);
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

    [Test]
    public async Task Wenn_ein_Zeiteintrag_nachgetragen_wird_dann_lautet_die_Adresse_zeiten_und_der_Rumpf_traegt_Beginn_und_Ende()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created, BeendeterEintrag, JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);
        var beginn = new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero);
        var ende = new DateTimeOffset(2026, 9, 5, 13, 30, 0, TimeSpan.Zero);

        var ergebnis = await klient.TrageNach(14, new ZeiteintragNachtragenAnfrage(3, beginn, ende));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("POST http://webapi.test/api/karten/14/zeiten"));
            Assert.That(fabrik.GesendeterRumpf, Does.Contain("\"kontributor\":3"));
            Assert.That(fabrik.GesendeterRumpf, Does.Contain("12:00:00"));
            Assert.That(fabrik.GesendeterRumpf, Does.Contain("13:30:00"));
            Assert.That(ergebnis.Wert.ZeiteintragId, Is.EqualTo(7));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_den_Nachtrag_zurueckweist_dann_traegt_das_Ergebnis_ihren_Befund()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest,
            """{"befunde":[{"code":"zeiteintrag-ende-vor-beginn","meldung":"Das Ende liegt vor dem Beginn: Beginn 2026-09-05T15:30:00Z, Ende 2026-09-05T14:00:00Z.","kompensation":"Den Aufruf wiederholen."}]}""",
            JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.TrageNach(14, new ZeiteintragNachtragenAnfrage(3, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("zeiteintrag-ende-vor-beginn"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("2026-09-05T15:30:00Z"));
        });
    }

    // Ein Ende von null reist als JSON-null mit: genau daran erkennt die WebApi den Rueckfall auf
    // „laeuft". Ueber den Browser waere ein weggelassenes Feld nicht von null zu unterscheiden.
    [Test]
    public async Task Wenn_ein_Zeiteintrag_wieder_laufen_soll_dann_traegt_der_Rumpf_ein_ende_von_null()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, LaufenderEintrag, JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.Aendere(14, 7, new ZeiteintragAendernAnfrage(3, new DateTimeOffset(2026, 9, 6, 8, 4, 0, TimeSpan.Zero), Ende: null));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("PUT http://webapi.test/api/karten/14/zeiten/7"));
            Assert.That(fabrik.GesendeterRumpf, Does.Contain("\"ende\":null"));
            Assert.That(ergebnis.Wert.Ende, Is.Null);
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Aenderung_zurueckweist_dann_traegt_das_Ergebnis_ihren_Befund()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest,
            """{"befunde":[{"code":"zeiteintrag-laeuft-schon","meldung":"Fuer den Kontributor 3 laeuft an der Karte 14 schon der Zeiteintrag 9.","kompensation":"Den Zeiteintrag 9 stoppen."}]}""",
            JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.Aendere(14, 7, new ZeiteintragAendernAnfrage(3, DateTimeOffset.UtcNow, Ende: null));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("zeiteintrag-laeuft-schon"));
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("Zeiteintrag 9"));
    }

    // **Die zweite Antwortgestalt dieses Klienten:** zurueck kommt das ganze Kartendetail und
    // nicht der geloeschte Eintrag — und der Aufruf traegt keinen Rumpf.
    [Test]
    public async Task Wenn_ein_Zeiteintrag_geloescht_wird_dann_traegt_das_Ergebnis_das_Kartendetail_und_der_Aufruf_keinen_Rumpf()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, LeeresKartendetail, JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.Loesche(14, 7);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("DELETE http://webapi.test/api/karten/14/zeiten/7"));
            Assert.That(fabrik.GesendeterRumpf, Is.Null);
            Assert.That(ergebnis.Wert.Karte.KarteId, Is.EqualTo(14));
            Assert.That(ergebnis.Wert.Zeiteintraege, Is.Empty);
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Loeschung_mit_404_zurueckweist_dann_traegt_das_Ergebnis_ihren_Befund()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.NotFound,
            """{"befunde":[{"code":"zeiteintrag-unbekannt","meldung":"Einen Zeiteintrag mit der Nummer 7 gibt es an der Karte 14 nicht.","kompensation":"`GET /api/karten/14` abrufen."}]}""",
            JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        var ergebnis = await klient.Loesche(14, 7);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("zeiteintrag-unbekannt"));
        Assert.That(() => ergebnis.Wert, Throws.InvalidOperationException);
    }

    [Test]
    public void Wenn_die_WebApi_beim_Loeschen_kein_Kartendetail_liefert_dann_scheitert_der_Aufruf_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, "null", JsonInhaltstyp);
        var klient = new ZeitenApiKlient(fabrik);

        Assert.That(async () => await klient.Loesche(14, 7), Throws.InvalidOperationException);
    }
}
