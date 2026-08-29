using System.Net;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Boards;

namespace KanbanC.Blazor.Tests.Services;

public class SpaltenApiKlientTests
{
    private const string JsonInhaltstyp = "application/json";

    [Test]
    public async Task Wenn_die_WebApi_die_Spalte_anlegt_dann_traegt_das_Ergebnis_die_Spalte_mit_ihrer_Position()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created,
            """{"spalteId":7,"bezeichnung":"Wartet auf Zulieferung","position":4,"istAbschlussspalte":false,"anzeigegrenze":null}""",
            JsonInhaltstyp);
        var klient = new SpaltenApiKlient(fabrik);

        var ergebnis = await klient.LegeSpalteAn(1, new SpalteAnlegenAnfrage("Wartet auf Zulieferung", false, null));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.SpalteId, Is.EqualTo(7));
            Assert.That(ergebnis.Wert.Position, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_Befunde_meldet_dann_stehen_sie_im_Ergebnis()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest,
            """{"befunde":["Die Bezeichnung darf nicht leer sein."]}""", JsonInhaltstyp);
        var klient = new SpaltenApiKlient(fabrik);

        var ergebnis = await klient.LegeSpalteAn(1, new SpalteAnlegenAnfrage("", false, null));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde, Is.EqualTo(new[] { "Die Bezeichnung darf nicht leer sein." }));
        Assert.That(() => ergebnis.Wert, Throws.InvalidOperationException);
    }

    [Test]
    public async Task Wenn_die_WebApi_einen_fremden_Fehlerrumpf_liefert_dann_traegt_die_Zurueckweisung_eine_lesbare_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest,
            """{"title":"One or more validation errors occurred.","status":400}""", JsonInhaltstyp);
        var klient = new SpaltenApiKlient(fabrik);

        var ergebnis = await klient.AendereSpalte(1, 2, new SpalteAendernAnfrage("Neu", false, null));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde, Has.Count.EqualTo(1));
        Assert.That(ergebnis.Zurueckweisung.Befunde[0], Does.Contain("HTTP 400"));
    }

    [Test]
    public async Task Wenn_die_WebApi_kein_JSON_liefert_dann_traegt_die_Zurueckweisung_eine_lesbare_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, "<html>Fehler</html>", "text/html");
        var klient = new SpaltenApiKlient(fabrik);

        var ergebnis = await klient.LegeSpalteAn(1, new SpalteAnlegenAnfrage("Eingang", false, null));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0], Does.Contain("HTTP 400"));
    }

    [Test]
    public async Task Wenn_die_WebApi_404_meldet_dann_erscheint_das_als_lesbare_Zurueckweisung_statt_als_Absturz()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.NotFound);
        var klient = new SpaltenApiKlient(fabrik);

        var ergebnis = await klient.AendereSpalte(1, 999, new SpalteAendernAnfrage("Erfunden", false, null));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0], Does.Contain("gibt es nicht mehr"));
    }

    [Test]
    public void Wenn_die_WebApi_einen_Serverfehler_meldet_dann_bleibt_der_Fehler_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new SpaltenApiKlient(fabrik);

        Assert.That(async () => await klient.LegeSpalteAn(1, new SpalteAnlegenAnfrage("Eingang", false, null)),
            Throws.TypeOf<HttpRequestException>());
    }

    [Test]
    public void Wenn_die_WebApi_auf_das_Anlegen_keine_Spalte_zurueckgibt_dann_meldet_der_Klient_den_Fehler()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created, "null", JsonInhaltstyp);
        var klient = new SpaltenApiKlient(fabrik);

        Assert.That(async () => await klient.LegeSpalteAn(1, new SpalteAnlegenAnfrage("Eingang", false, null)),
            Throws.InvalidOperationException);
    }

    [Test]
    public async Task Wenn_die_WebApi_die_neue_Reihenfolge_liefert_dann_stehen_die_Spalten_im_Ergebnis()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK,
            """[{"spalteId":3,"bezeichnung":"Erledigt","position":1,"istAbschlussspalte":true,"anzeigegrenze":20},""" +
            """{"spalteId":1,"bezeichnung":"Zu erledigen","position":2,"istAbschlussspalte":false,"anzeigegrenze":null}]""",
            JsonInhaltstyp);
        var klient = new SpaltenApiKlient(fabrik);

        var ergebnis = await klient.SetzeReihenfolge(1, new Spaltenreihenfolge([3, 1]));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.That(ergebnis.Wert.Select(s => s.Bezeichnung), Is.EqualTo(new[] { "Erledigt", "Zu erledigen" }));
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Reihenfolge_zurueckweist_dann_stehen_die_Befunde_im_Ergebnis()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest,
            """{"befunde":["Die Reihenfolge muss alle Spalten des Boards nennen."]}""", JsonInhaltstyp);
        var klient = new SpaltenApiKlient(fabrik);

        var ergebnis = await klient.SetzeReihenfolge(1, new Spaltenreihenfolge([3]));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0], Does.Contain("alle Spalten"));
    }
}
