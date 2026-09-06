using System.Net;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Klassen;

namespace KanbanC.Blazor.Tests.Services;

// Diese Fehlerpfade sind über den Browser nicht auslösbar — der Grund, aus dem es dieses
// Testprojekt gibt.
public class KartenklassenApiKlientTests
{
    private const string JsonInhaltstyp = "application/json";

    [Test]
    public async Task Wenn_die_WebApi_die_Liste_liefert_dann_traegt_das_Ergebnis_die_Kartenklassen_in_der_gelieferten_Reihenfolge()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK,
            """[{"kartenklasseId":1,"name":"WBS","praefix":"WBS-","zaehlerstand":0},{"kartenklasseId":2,"name":"Bugmeldungen","praefix":"BUG-","zaehlerstand":7}]""",
            JsonInhaltstyp);
        var klient = new KartenklassenApiKlient(fabrik);

        var ergebnis = await klient.LadeKartenklassen(2);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.That(ergebnis.Wert.Select(kartenklasse => kartenklasse.Name), Is.EqualTo(new[] { "WBS", "Bugmeldungen" }));
        Assert.That(ergebnis.Wert[1].Zaehlerstand, Is.EqualTo(7));
    }

    [Test]
    public async Task Wenn_das_Board_keine_Kartenklasse_hat_dann_traegt_das_Ergebnis_eine_leere_Liste()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, "[]", JsonInhaltstyp);
        var klient = new KartenklassenApiKlient(fabrik);

        var ergebnis = await klient.LadeKartenklassen(2);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.That(ergebnis.Wert, Is.Empty);
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Kartenklasse_anlegt_dann_traegt_das_Ergebnis_sie_mit_Zaehlerstand_0()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created,
            """{"kartenklasseId":5,"name":"Dokumentation","praefix":"DOK-","zaehlerstand":0}""", JsonInhaltstyp);
        var klient = new KartenklassenApiKlient(fabrik);

        var ergebnis = await klient.LegeKartenklasseAn(2, new KartenklasseAnlegenAnfrage("Dokumentation", "DOK-"));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.KartenklasseId, Is.EqualTo(5));
            Assert.That(ergebnis.Wert.Praefix, Is.EqualTo("DOK-"));
            Assert.That(ergebnis.Wert.Zaehlerstand, Is.EqualTo(0));
        });
    }

    // Die Route heisst im ganzen Stack kartenklassen — das Artboard zeichnet klassen, und genau
    // diese Abweichung waere im Browser nicht zu sehen.
    [Test]
    public async Task Wenn_die_Liste_abgerufen_wird_dann_lautet_die_Adresse_kartenklassen_und_nicht_klassen()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, "[]", JsonInhaltstyp);
        var klient = new KartenklassenApiKlient(fabrik);

        await klient.LadeKartenklassen(2);

        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("GET http://webapi.test/api/boards/2/kartenklassen"));
    }

    [Test]
    public async Task Wenn_die_Kartenklasse_angelegt_wird_dann_traegt_der_Aufruf_Methode_Adresse_und_die_zwei_Angaben()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created,
            """{"kartenklasseId":1,"name":"WBS","praefix":"WBS-","zaehlerstand":0}""", JsonInhaltstyp);
        var klient = new KartenklassenApiKlient(fabrik);

        await klient.LegeKartenklasseAn(2, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("POST http://webapi.test/api/boards/2/kartenklassen"));
        Assert.That(fabrik.GesendeterRumpf, Is.EqualTo("""{"name":"WBS","praefix":"WBS-"}"""));
    }

    [Test]
    public async Task Wenn_die_WebApi_Befunde_meldet_dann_stehen_sie_im_Ergebnis()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest,
            """{"befunde":[{"code":"kartenklasse-name-leer","meldung":"Eine Klasse braucht einen Namen.","kompensation":"`POST /api/boards/2/kartenklassen` mit einem nichtleeren „name“ wiederholen."}]}""",
            JsonInhaltstyp);
        var klient = new KartenklassenApiKlient(fabrik);

        var ergebnis = await klient.LegeKartenklasseAn(2, new KartenklasseAnlegenAnfrage("", "WBS-"));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde.Select(befund => befund.Meldung), Is.EqualTo(new[] { "Eine Klasse braucht einen Namen." }));
        Assert.That(() => ergebnis.Wert, Throws.InvalidOperationException);
    }

    [Test]
    public async Task Wenn_die_WebApi_404_meldet_dann_erscheint_das_als_lesbare_Zurueckweisung_statt_als_Absturz()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.NotFound);
        var klient = new KartenklassenApiKlient(fabrik);

        var ergebnis = await klient.LadeKartenklassen(999);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("gibt es nicht mehr"));
    }

    [Test]
    public async Task Wenn_die_WebApi_kein_JSON_liefert_dann_traegt_die_Zurueckweisung_eine_lesbare_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, "<html>Fehler</html>", "text/html");
        var klient = new KartenklassenApiKlient(fabrik);

        var ergebnis = await klient.LegeKartenklasseAn(2, new KartenklasseAnlegenAnfrage("WBS", "WBS-"));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("HTTP 400"));
    }
}
