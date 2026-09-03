using System.Net;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Boards;
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
        const string rumpf = """{"befunde":[{"code":"kartentitel-leer","meldung":"Der Titel darf nicht leer sein.","kompensation":"Den Aufruf mit nichtleerem Titel wiederholen."}]}""";
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.LegeKarteAn(1, 2, new KarteAnlegenAnfrage(""));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Is.EqualTo("Der Titel darf nicht leer sein."));
    }

    [Test]
    public async Task Wenn_die_Zurueckweisung_keinen_lesbaren_Rumpf_hat_dann_meldet_der_Klient_trotzdem_einen_Befund()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, "kein JSON", "text/plain");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.LegeKarteAn(1, 2, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("HTTP 400"));
    }

    [Test]
    public async Task Wenn_die_Spalte_unbekannt_ist_dann_meldet_der_Klient_die_feste_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.NotFound);
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.LegeKarteAn(1, 999, new KarteAnlegenAnfrage("Migration schreiben"));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("gibt es nicht mehr"));
    }

    [Test]
    public void Wenn_die_WebApi_einen_Serverfehler_meldet_dann_bleibt_der_Fehler_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new KartenApiKlient(fabrik);

        Assert.That(async () => await klient.LegeKarteAn(1, 2, new KarteAnlegenAnfrage("Migration schreiben")),
            Throws.TypeOf<HttpRequestException>());
    }

    [Test]
    public async Task Wenn_die_WebApi_den_Zug_ausfuehrt_dann_liefert_der_Klient_die_Spalten_in_der_neuen_Reihenfolge()
    {
        const string rumpf = """
            [
              {"spalteId":1,"bezeichnung":"Zu erledigen","position":1,"istAbschlussspalte":false,"anzeigegrenze":null,"karten":[]},
              {"spalteId":2,"bezeichnung":"In Arbeit","position":2,"istAbschlussspalte":false,"anzeigegrenze":null,
               "karten":[{"karteId":7,"titel":"Endpunkt bauen","position":1}]}
            ]
            """;
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.VerschiebeKarte(1, 7, new Kartenlage(2, 1));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert, Has.Count.EqualTo(2));
            Assert.That(ergebnis.Wert[0].Karten, Is.Empty);
            Assert.That(ergebnis.Wert[1].Karten[0].Titel, Is.EqualTo("Endpunkt bauen"));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_den_Zug_zurueckweist_dann_reicht_der_Klient_Meldung_und_Code_durch()
    {
        const string rumpf = """
            {"befunde":[{"code":"position-ausserhalb","meldung":"Position 5 liegt ausserhalb der Zielspalte.",
             "kompensation":"GET /api/boards/1 abrufen und mit Position 1 bis 4 wiederholen."}]}
            """;
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.VerschiebeKarte(1, 7, new Kartenlage(2, 5));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("Position 5"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("position-ausserhalb"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Kompensation, Does.Contain("/api/boards/1"));
        });
    }

    [Test]
    public async Task Wenn_die_Karte_beim_Zug_unbekannt_ist_dann_meldet_der_Klient_die_feste_Meldung()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.NotFound);
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.VerschiebeKarte(1, 999, new Kartenlage(2, 1));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("gibt es nicht mehr"));
    }

    [Test]
    public void Wenn_die_WebApi_beim_Zug_nicht_erreichbar_ist_dann_bleibt_die_Ausnahme_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new KartenApiKlient(fabrik);

        Assert.That(async () => await klient.VerschiebeKarte(1, 7, new Kartenlage(2, 1)),
            Throws.TypeOf<HttpRequestException>());
    }
}
