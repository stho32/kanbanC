using System.Net;
using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.Blazor.Tests.Services;

// Diese Fehlerpfade sind über den Browser nicht auslösbar — der Grund, aus dem es dieses
// Testprojekt gibt.
public class AuswertungenApiKlientTests
{
    private const string JsonInhaltstyp = "application/json";

    private const string EineAuswertung = """
        {
          "zeilen": [
            {
              "karteId": 11,
              "kartennummer": "WBS-30",
              "titel": "[I0030] WBS-Datei importieren",
              "erfassteZeit": "02:24:00",
              "sollband": { "vonStunden": 38.0, "bisStunden": 44.0 },
              "abweichung": { "lage": "UnterDemBand", "ueberschussStunden": null },
              "istArchiviert": false
            },
            {
              "karteId": 12,
              "kartennummer": "WBS-31",
              "titel": "[I0031] Import wiederholen",
              "erfassteZeit": "00:00:00",
              "sollband": null,
              "abweichung": null,
              "istArchiviert": true
            }
          ],
          "summe": {
            "erfassteZeit": "02:24:00",
            "sollband": { "vonStunden": 38.0, "bisStunden": 44.0 },
            "abweichung": { "lage": "UnterDemBand", "ueberschussStunden": null },
            "kartenOhneSoll": 1
          }
        }
        """;

    private const string EinBurndown = """
        {
          "tage": [
            {
              "tag": "2026-09-05",
              "erledigteKarten": [ { "karteId": 11, "kartennummer": "WBS-01", "titel": "[I0001] Board anlegen" } ],
              "offeneKarten": 2
            },
            { "tag": "2026-09-06", "erledigteKarten": [], "offeneKarten": 2 },
            { "tag": "2026-09-07", "erledigteKarten": [], "offeneKarten": 2 }
          ],
          "kopfzahlen": { "offen": 2, "erledigt": 3, "imBestand": 5, "ohneErledigungsdatum": 1 }
        }
        """;

    [Test]
    public async Task Wenn_die_WebApi_die_Auswertung_liefert_dann_traegt_das_Ergebnis_Zeilen_und_Summe()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, EineAuswertung, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        var ergebnis = await klient.LadeSollIst(4, 2);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Zeilen, Has.Count.EqualTo(2));
            Assert.That(ergebnis.Wert.Zeilen[0].Sollband, Is.EqualTo(new Zeitband(38.0m, 44.0m)));
            Assert.That(ergebnis.Wert.Zeilen[0].Abweichung, Is.EqualTo(new Abweichung(Abweichungslage.UnterDemBand, null)));
            Assert.That(ergebnis.Wert.Zeilen[0].ErfassteZeit, Is.EqualTo(TimeSpan.FromMinutes(144)));
            Assert.That(ergebnis.Wert.Zeilen[1].Sollband, Is.Null);
            Assert.That(ergebnis.Wert.Zeilen[1].IstArchiviert, Is.True);
            Assert.That(ergebnis.Wert.Summe.KartenOhneSoll, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Wenn_der_Bestand_abgerufen_wird_dann_lautet_die_Adresse_soll_ist_unter_der_Kartenklasse()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, EineAuswertung, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        await klient.LadeSollIst(4, 2);

        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("GET http://webapi.test/api/boards/4/kartenklassen/2/soll-ist"));
    }

    // **Der 404 dieser Route sagt selbst, welches Ding fehlt** — Grund, Werte und
    // Kompensationsaktion reisen bis in die Oberfläche durch, statt zu einem Sammelsatz zu werden.
    [Test]
    public async Task Wenn_die_WebApi_das_Board_nicht_kennt_dann_traegt_die_Zurueckweisung_den_gemeldeten_Befund()
    {
        const string Zurueckgewiesen = """
            {"befunde":[{"code":"board-unbekannt","meldung":"Ein Board mit der Nummer 999 gibt es nicht.","kompensation":"`GET /api/boards` abrufen und den Aufruf mit einer der gelieferten BoardIds wiederholen."}]}
            """;
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.NotFound, Zurueckgewiesen, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        var ergebnis = await klient.LadeSollIst(999, 2);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        var befund = ergebnis.Zurueckweisung.Befunde.Single();
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("board-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Kartenklasse_einem_fremden_Board_zuschreibt_dann_steht_ihr_eigener_Code_in_der_Zurueckweisung()
    {
        const string Zurueckgewiesen = """
            {"befunde":[{"code":"kartenklasse-fremd","meldung":"Die Kartenklasse 2 gehört zum Board 7, nicht zum Board 4 dieser Karte.","kompensation":"`GET /api/boards/4/kartenklassen` abrufen und den Aufruf mit einer KartenklasseId dieses Boards wiederholen."}]}
            """;
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.NotFound, Zurueckgewiesen, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        var ergebnis = await klient.LadeSollIst(4, 2);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde.Single().Code, Is.EqualTo("kartenklasse-fremd"));
    }

    // Eine Fehlerantwort ohne lesbaren Rumpf wird trotzdem zu einer Zurückweisung mit Befund —
    // keine leere Meldung, kein Absturz.
    [Test]
    public async Task Wenn_die_Fehlerantwort_keinen_lesbaren_Rumpf_hat_dann_kommt_trotzdem_ein_Befund()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.NotFound);
        var klient = new AuswertungenApiKlient(fabrik);

        var ergebnis = await klient.LadeSollIst(4, 2);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde.Single().Kompensation, Is.Not.Empty);
    }

    // Der Ausfall läuft bis zum Aufrufer durch: die Ausfallmeldung entsteht auf dem Schirm über
    // WebApiAufruf.MitAusfallmeldung, nicht im Klienten.
    [Test]
    public void Wenn_die_WebApi_ausfaellt_dann_laeuft_der_Fehler_bis_zum_Aufrufer_durch()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.ServiceUnavailable, string.Empty, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        Assert.That(async () => await klient.LadeSollIst(4, 2), Throws.InstanceOf<HttpRequestException>());
    }

    [Test]
    public async Task Wenn_die_WebApi_den_Burndown_liefert_dann_traegt_das_Ergebnis_Tage_und_Kopfzahlen()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, EinBurndown, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        var ergebnis = await klient.LadeBurndown(4, 2, seit: null);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Tage, Has.Count.EqualTo(3));
            Assert.That(ergebnis.Wert.Tage[0].Tag, Is.EqualTo(new DateOnly(2026, 9, 5)));
            Assert.That(ergebnis.Wert.Tage[0].ErledigteKarten.Single().Kartennummer, Is.EqualTo("WBS-01"));
            Assert.That(ergebnis.Wert.Tage[0].OffeneKarten, Is.EqualTo(2));
            Assert.That(ergebnis.Wert.Tage[1].ErledigteKarten, Is.Empty);
            Assert.That(ergebnis.Wert.Kopfzahlen, Is.EqualTo(new Burndownkopfzahlen(2, 3, 5, 1)));
        });
    }

    [Test]
    public async Task Wenn_der_Burndown_ohne_Zeitraum_abgerufen_wird_dann_traegt_die_Adresse_keinen_Abfrageparameter()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, EinBurndown, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        await klient.LadeBurndown(4, 2, seit: null);

        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("GET http://webapi.test/api/boards/4/kartenklassen/2/burndown"));
    }

    [Test]
    public async Task Wenn_ein_Zeitraum_gewaehlt_ist_dann_steht_er_als_seit_in_der_Adresse()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, EinBurndown, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        await klient.LadeBurndown(4, 2, new DateOnly(2026, 9, 5));

        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("GET http://webapi.test/api/boards/4/kartenklassen/2/burndown?seit=2026-09-05"));
    }

    // Der 400 dieser Route trägt **unseren** Befund: den gelesenen Wert und die erwartete Form.
    // Über den Browser ist dieser Pfad nicht auslösbar — das Datumsfeld liefert nie „gestern“.
    [Test]
    public async Task Wenn_die_WebApi_den_Zeitraum_nicht_lesen_kann_dann_traegt_die_Zurueckweisung_den_gemeldeten_Befund()
    {
        const string Zurueckgewiesen = """
            {"befunde":[{"code":"zeitraum-filter-unlesbar","meldung":"„gestern“ ist kein Datum; „seit“ nimmt die Form „yyyy-MM-dd“.","kompensation":"`/api/boards/4/kartenklassen/2/burndown` ohne Parameter aufrufen."}]}
            """;
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, Zurueckgewiesen, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        var ergebnis = await klient.LadeBurndown(4, 2, new DateOnly(2026, 9, 5));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        var befund = ergebnis.Zurueckweisung.Befunde.Single();
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeitraum-filter-unlesbar"));
            Assert.That(befund.Meldung, Does.Contain("gestern"));
            Assert.That(befund.Kompensation, Does.Contain("ohne Parameter"));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_das_Board_des_Burndowns_nicht_kennt_dann_traegt_die_Zurueckweisung_den_gemeldeten_Befund()
    {
        const string Zurueckgewiesen = """
            {"befunde":[{"code":"board-unbekannt","meldung":"Ein Board mit der Nummer 999 gibt es nicht.","kompensation":"`GET /api/boards` abrufen und den Aufruf mit einer der gelieferten BoardIds wiederholen."}]}
            """;
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.NotFound, Zurueckgewiesen, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        var ergebnis = await klient.LadeBurndown(999, 2, seit: null);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde.Single().Code, Is.EqualTo("board-unbekannt"));
    }

    [Test]
    public void Wenn_die_WebApi_beim_Burndown_ausfaellt_dann_laeuft_der_Fehler_bis_zum_Aufrufer_durch()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.ServiceUnavailable, string.Empty, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        Assert.That(async () => await klient.LadeBurndown(4, 2, seit: null), Throws.InstanceOf<HttpRequestException>());
    }

    // Der Stand trägt **keine Zeilen** — er ist kein zweiter Weg zu den Daten.
    private const string EinZeitexportstand = """
        {
          "eintraege": 5,
          "karten": 3,
          "kontributoren": 2,
          "laufende": 1,
          "von": "2026-08-31",
          "bis": "2026-09-07",
          "dateiname": "kanbanc-release-2-zeiten-2026-08-31_2026-09-07.csv"
        }
        """;

    [Test]
    public async Task Wenn_die_WebApi_den_Zeitexportstand_liefert_dann_traegt_das_Ergebnis_Zaehlung_Grenzen_und_Dateiname()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, EinZeitexportstand, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        var ergebnis = await klient.LadeZeitexportstand(4, 2, von: null, bis: null);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Eintraege, Is.EqualTo(5));
            Assert.That(ergebnis.Wert.Karten, Is.EqualTo(3));
            Assert.That(ergebnis.Wert.Kontributoren, Is.EqualTo(2));
            Assert.That(ergebnis.Wert.Laufende, Is.EqualTo(1));
            Assert.That(ergebnis.Wert.Von, Is.EqualTo(new DateOnly(2026, 8, 31)));
            Assert.That(ergebnis.Wert.Bis, Is.EqualTo(new DateOnly(2026, 9, 7)));
            Assert.That(ergebnis.Wert.Dateiname, Is.EqualTo("kanbanc-release-2-zeiten-2026-08-31_2026-09-07.csv"));
        });
    }

    [Test]
    public async Task Wenn_der_Zeitexportstand_ohne_Grenzen_abgerufen_wird_dann_traegt_die_Adresse_keinen_Abfrageparameter()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, EinZeitexportstand, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        await klient.LadeZeitexportstand(4, 2, von: null, bis: null);

        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("GET http://webapi.test/api/boards/4/kartenklassen/2/zeitexport"));
    }

    [Test]
    public async Task Wenn_beide_Grenzen_gewaehlt_sind_dann_stehen_sie_als_von_und_bis_in_der_Adresse()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, EinZeitexportstand, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        await klient.LadeZeitexportstand(4, 2, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 6));

        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("GET http://webapi.test/api/boards/4/kartenklassen/2/zeitexport?von=2026-09-01&bis=2026-09-06"));
    }

    // Die verdrehte Spanne weist die API zurück; der Schirm zeigt genau diesen Befund. Über den
    // Browser ist dieser Pfad nicht auslösbar — die Oberfläche prüft nicht selbst nach.
    [Test]
    public async Task Wenn_die_WebApi_die_Spanne_als_verdreht_zurueckweist_dann_traegt_die_Zurueckweisung_den_gemeldeten_Befund()
    {
        const string Zurueckgewiesen = """
            {"befunde":[{"code":"zeitraum-filter-verdreht","meldung":"„von“ ist „2026-09-07“ und „bis“ ist „2026-09-01“; „bis“ liegt vor „von“.","kompensation":"`/api/boards/4/kartenklassen/2/zeitexport.csv?von=2026-09-01&bis=2026-09-07` aufrufen — die beiden Grenzen tauschen."}]}
            """;
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, Zurueckgewiesen, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        var ergebnis = await klient.LadeZeitexportstand(4, 2, new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 1));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        var befund = ergebnis.Zurueckweisung.Befunde.Single();
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeitraum-filter-verdreht"));
            Assert.That(befund.Meldung, Does.Contain("2026-09-07"));
            Assert.That(befund.Kompensation, Does.Contain("tauschen"));
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_das_Board_des_Zeitexports_nicht_kennt_dann_traegt_die_Zurueckweisung_den_gemeldeten_Befund()
    {
        const string Zurueckgewiesen = """
            {"befunde":[{"code":"board-unbekannt","meldung":"Ein Board mit der Nummer 999 gibt es nicht.","kompensation":"`GET /api/boards` abrufen und den Aufruf mit einer der gelieferten BoardIds wiederholen."}]}
            """;
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.NotFound, Zurueckgewiesen, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        var ergebnis = await klient.LadeZeitexportstand(999, 2, von: null, bis: null);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde.Single().Code, Is.EqualTo("board-unbekannt"));
    }

    [Test]
    public void Wenn_die_WebApi_beim_Zeitexportstand_ausfaellt_dann_laeuft_der_Fehler_bis_zum_Aufrufer_durch()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.ServiceUnavailable, string.Empty, JsonInhaltstyp);
        var klient = new AuswertungenApiKlient(fabrik);

        Assert.That(async () => await klient.LadeZeitexportstand(4, 2, von: null, bis: null), Throws.InstanceOf<HttpRequestException>());
    }
}
