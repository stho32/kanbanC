using System.Net;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Tests.Services;

// Die Fehlerpfade des Klienten sind über den Browser nicht auslösbar; sie werden hier geprüft.
public class KartenApiKlientTests
{
    [Test]
    public async Task Wenn_die_WebApi_die_Karte_anlegt_dann_liefert_der_Klient_sie_als_Erfolg()
    {
        const string rumpf = """{"karteId":7,"titel":"Migration schreiben","position":3}""";
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.Created, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.LegeKarteAn(1, 2, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.KarteId, Is.EqualTo(7));
            Assert.That(ergebnis.Wert.Titel, Is.EqualTo("Migration schreiben"));
            Assert.That(ergebnis.Wert.Position, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Kartenanlage_zurueckweist_dann_reicht_der_Klient_die_Befunde_aus_dem_Rumpf_durch()
    {
        const string rumpf = """{"befunde":["Der Titel darf nicht leer sein."]}""";
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.LegeKarteAn(1, 2, new KarteAnlegenAnfrage(""));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0], Is.EqualTo("Der Titel darf nicht leer sein."));
    }

    [Test]
    public async Task Wenn_die_Zurueckweisung_keinen_lesbaren_Rumpf_hat_dann_meldet_der_Klient_trotzdem_einen_Befund()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, "kein JSON", "text/plain");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.LegeKarteAn(1, 2, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0], Does.Contain("HTTP 400"));
    }

    [Test]
    public async Task Wenn_die_Spalte_unbekannt_ist_dann_meldet_der_Klient_die_feste_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.NotFound);
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.LegeKarteAn(1, 999, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0], Does.Contain("gibt es nicht mehr"));
    }

    [Test]
    public void Wenn_die_WebApi_einen_Serverfehler_meldet_dann_bleibt_der_Fehler_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new KartenApiKlient(fabrik);

        Assert.That(async () => await klient.LegeKarteAn(1, 2, new KarteAnlegenAnfrage("Migration schreiben")),
            Throws.TypeOf<HttpRequestException>());
    }
}
