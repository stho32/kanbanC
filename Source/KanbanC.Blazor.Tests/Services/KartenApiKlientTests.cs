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
    public async Task Wenn_die_WebApi_das_Kartendetail_liefert_dann_ruft_der_Klient_die_boardlose_Adresse_und_reicht_es_durch()
    {
        const string rumpf = """{"karte":{"karteId":14,"titel":"Migration schreiben","position":2},"board":3,"boardname":"Entwicklung","spalte":5,"spaltenbezeichnung":"In Arbeit"}""";
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.LadeKartendetail(14);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("GET http://webapi.test/api/karten/14"));
            Assert.That(ergebnis.Wert.Karte.Titel, Is.EqualTo("Migration schreiben"));
            Assert.That(ergebnis.Wert.Board, Is.EqualTo(3));
            Assert.That(ergebnis.Wert.Boardname, Is.EqualTo("Entwicklung"));
            Assert.That(ergebnis.Wert.Spaltenbezeichnung, Is.EqualTo("In Arbeit"));
        });
    }

    // Der 404 dieser Route traegt einen eigenen Befund; er darf nicht durch die Board-Meldung
    // des ApiAntwortlesers ersetzt werden, denn die Route kennt kein Board.
    [Test]
    public async Task Wenn_die_Karte_unbekannt_ist_dann_reicht_der_Klient_den_Befund_der_WebApi_durch()
    {
        const string rumpf = """{"befunde":[{"code":"karte-unbekannt","meldung":"Eine Karte mit der Nummer 9999 gibt es nicht.","kompensation":"`GET /api/boards` abrufen."}]}""";
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.NotFound, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.LadeKartendetail(9999);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("9999"));
        });
    }

    [Test]
    public async Task Wenn_die_Karte_geaendert_wird_dann_setzt_der_Klient_ein_PUT_auf_die_boardlose_Adresse_mit_allen_vier_Feldern_ab()
    {
        const string rumpf = """{"karte":{"karteId":14,"titel":"WBS-Import","position":2,"beschreibung":"Knoten","faelligAm":"2026-09-02","farbe":"Terrakotta"},"board":3,"boardname":"Entwicklung","spalte":5,"spaltenbezeichnung":"In Arbeit"}""";
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.AendereKarte(14, new KarteAendernAnfrage("WBS-Import", "Knoten", new DateOnly(2026, 9, 2), Kartenfarbe.Terrakotta, Kontributor: null));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("PUT http://webapi.test/api/karten/14"));
            Assert.That(fabrik.GesendeterRumpf, Does.Contain("\"titel\":\"WBS-Import\""));
            Assert.That(fabrik.GesendeterRumpf, Does.Contain("\"faelligAm\":\"2026-09-02\""));
            Assert.That(fabrik.GesendeterRumpf, Does.Contain("\"farbe\":\"Terrakotta\""));
            Assert.That(ergebnis.Wert.Karte.Farbe, Is.EqualTo(Kartenfarbe.Terrakotta));
        });
    }

    // Ein geleertes Datumsfeld reist als null, nicht als leerer Text — den wiese
    // System.Text.Json ab (DateOnlyEingabeProbeTests).
    [Test]
    public async Task Wenn_die_Faelligkeit_geleert_wird_dann_traegt_der_Rumpf_null_und_keinen_leeren_Text()
    {
        const string rumpf = """{"karte":{"karteId":14,"titel":"WBS-Import","position":2,"beschreibung":null,"faelligAm":null,"farbe":"Ohne"},"board":3,"boardname":"Entwicklung","spalte":5,"spaltenbezeichnung":"In Arbeit"}""";
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        await klient.AendereKarte(14, new KarteAendernAnfrage("WBS-Import", null, null, Kartenfarbe.Ohne, Kontributor: null));

        Assert.That(fabrik.GesendeterRumpf, Does.Contain("\"faelligAm\":null"));
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Kartenaenderung_zurueckweist_dann_reicht_der_Klient_ihren_Befund_durch()
    {
        const string rumpf = """{"befunde":[{"code":"kartentitel-leer","meldung":"Der Titel darf nicht leer sein.","kompensation":"`PUT /api/karten/14` mit einem nichtleeren „titel“ wiederholen."}]}""";
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.AendereKarte(14, new KarteAendernAnfrage("", null, null, Kartenfarbe.Ohne, Kontributor: null));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Is.EqualTo("Der Titel darf nicht leer sein."));
    }

    [Test]
    public async Task Wenn_die_Etiketten_gesetzt_werden_dann_setzt_der_Klient_ein_PUT_auf_die_Unterressource_mit_der_ganzen_Liste_ab()
    {
        const string rumpf = """{"karte":{"karteId":14,"titel":"WBS-Import","position":2,"farbe":"Ohne"},"board":3,"boardname":"Entwicklung","spalte":5,"spaltenbezeichnung":"In Arbeit","etiketten":["Doku","Import"],"etikettvorschlaege":[{"text":"Doku","kartenzahl":2}]}""";
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.SetzeEtiketten(14, new Kartenetiketten(["Import", "Doku"]));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("PUT http://webapi.test/api/karten/14/etiketten"));
            Assert.That(fabrik.GesendeterRumpf, Is.EqualTo("""{"etiketten":["Import","Doku"]}"""));
            Assert.That(ergebnis.Wert.Etiketten, Is.EqualTo(new[] { "Doku", "Import" }));
            Assert.That(ergebnis.Wert.Etikettvorschlaege[0].Kartenzahl, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Wenn_eine_leere_Etikettenliste_gesetzt_wird_dann_reist_sie_als_leeres_Feld_und_nicht_als_null()
    {
        const string rumpf = """{"karte":{"karteId":14,"titel":"WBS-Import","position":2,"farbe":"Ohne"},"board":3,"boardname":"Entwicklung","spalte":5,"spaltenbezeichnung":"In Arbeit","etiketten":[],"etikettvorschlaege":[]}""";
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.SetzeEtiketten(14, new Kartenetiketten([]));

        Assert.Multiple(() =>
        {
            Assert.That(fabrik.GesendeterRumpf, Is.EqualTo("""{"etiketten":[]}"""));
            Assert.That(ergebnis.Wert.Etiketten, Is.Empty);
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Etikettenliste_zurueckweist_dann_reicht_der_Klient_ihren_Befund_durch()
    {
        const string rumpf = """{"befunde":[{"code":"etikett-doppelt","meldung":"Das Etikett „Import“ steht zweimal in der Liste.","kompensation":"`PUT /api/karten/14/etiketten` mit „Import“ nur einmal wiederholen."}]}""";
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.SetzeEtiketten(14, new Kartenetiketten(["Import", "Import"]));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("etikett-doppelt"));
    }

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

    [Test]
    public async Task Wenn_die_WebApi_die_Karten_einer_Spalte_liefert_dann_gibt_der_Klient_sie_mit_Erledigungsdatum_zurueck()
    {
        const string rumpf = """
            [
              {"karteId":7,"titel":"Zuerst fertig","position":1,"erledigtAm":"2026-09-04"},
              {"karteId":8,"titel":"Bestandskarte","position":2,"erledigtAm":null}
            ]
            """;
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.LadeKartenDerSpalte(1, 3);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert, Has.Count.EqualTo(2));
            Assert.That(ergebnis.Wert[0].ErledigtAm, Is.EqualTo(new DateOnly(2026, 9, 4)));
            Assert.That(ergebnis.Wert[1].ErledigtAm, Is.Null);
        });
    }

    // Ueber den Browser nicht pruefbar: dass der Klient genau die vereinbarte Route mit GET trifft.
    [Test]
    public async Task Wenn_die_Karten_einer_Spalte_geholt_werden_dann_trifft_der_Klient_die_vereinbarte_Adresse_mit_GET()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, "[]", "application/json");
        var klient = new KartenApiKlient(fabrik);

        await klient.LadeKartenDerSpalte(4, 9);

        Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("GET http://webapi.test/api/boards/4/spalten/9/karten"));
    }

    [Test]
    public async Task Wenn_die_Spalte_beim_Nachladen_unbekannt_ist_dann_meldet_der_Klient_einen_Befund_statt_zu_werfen()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.NotFound);
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.LadeKartenDerSpalte(1, 999);

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.Not.Empty);
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("gibt es nicht mehr"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Kompensation, Is.Not.Empty);
        });
    }

    [Test]
    public void Wenn_die_WebApi_beim_Nachladen_nicht_erreichbar_ist_dann_bleibt_die_Ausnahme_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new KartenApiKlient(fabrik);

        Assert.That(async () => await klient.LadeKartenDerSpalte(1, 3), Throws.TypeOf<HttpRequestException>());
    }

    [Test]
    public async Task Wenn_die_WebApi_die_Archivierung_annimmt_dann_liefert_der_Klient_die_Spalten_ohne_die_Karte()
    {
        const string rumpf = """
            [
              {"spalteId":2,"bezeichnung":"Zu erledigen","position":1,"istAbschlussspalte":false,"anzeigegrenze":null,
               "karten":[{"karteId":7,"titel":"A","position":1}],"kartenzahl":1}
            ]
            """;
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.SchalteArchivierung(1, 8, new Archivierung(true));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert, Has.Count.EqualTo(1));
            Assert.That(ergebnis.Wert[0].Karten[0].Titel, Is.EqualTo("A"));
            Assert.That(ergebnis.Wert[0].Kartenzahl, Is.EqualTo(1));
        });
    }

    // Ueber den Browser ist nicht pruefbar, ob der Klient die vereinbarte Route trifft und den
    // gewuenschten Archivstand mitschickt.
    [Test]
    public async Task Wenn_der_Klient_archiviert_dann_setzt_er_ein_PUT_auf_die_Archivierungsadresse_mit_dem_Archivstand_ab()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, "[]", "application/json");
        var klient = new KartenApiKlient(fabrik);

        await klient.SchalteArchivierung(1, 8, new Archivierung(true));

        Assert.Multiple(() =>
        {
            Assert.That(fabrik.AbgesetzterAufruf, Is.EqualTo("PUT http://webapi.test/api/boards/1/karten/8/archivierung"));
            Assert.That(fabrik.GesendeterRumpf, Is.EqualTo("""{"istArchiviert":true}"""));
        });
    }

    [Test]
    public async Task Wenn_der_Klient_zurueckholt_dann_schickt_er_denselben_Rumpf_mit_false()
    {
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.OK, "[]", "application/json");
        var klient = new KartenApiKlient(fabrik);

        await klient.SchalteArchivierung(1, 8, new Archivierung(false));

        Assert.That(fabrik.GesendeterRumpf, Is.EqualTo("""{"istArchiviert":false}"""));
    }

    // Wie auf allen Wegen des Klienten wird ein 404 zur festen, lesbaren Meldung: den Befund der
    // API liest der Agent an der Route, der Mensch am Bildschirm braucht einen Satz.
    [Test]
    public async Task Wenn_die_Karte_beim_Archivieren_unbekannt_ist_dann_meldet_der_Klient_einen_Befund_statt_zu_werfen()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.NotFound);
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.SchalteArchivierung(1, 999, new Archivierung(true));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.Not.Empty);
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("gibt es nicht mehr"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Kompensation, Is.Not.Empty);
        });
    }

    [Test]
    public async Task Wenn_die_WebApi_das_Archivieren_zurueckweist_dann_reicht_der_Klient_Meldung_und_Code_durch()
    {
        const string rumpf = """
            {"befunde":[{"code":"archiv-stand-unlesbar","meldung":"Der Archivstand war nicht lesbar.",
             "kompensation":"Den Aufruf mit istArchiviert true oder false wiederholen."}]}
            """;
        using var fabrik = TestKlientFabrik.MitAntwort(HttpStatusCode.BadRequest, rumpf, "application/json");
        var klient = new KartenApiKlient(fabrik);

        var ergebnis = await klient.SchalteArchivierung(1, 8, new Archivierung(true));

        Assert.That(ergebnis.WurdeZurueckgewiesen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Code, Is.EqualTo("archiv-stand-unlesbar"));
            Assert.That(ergebnis.Zurueckweisung.Befunde[0].Meldung, Does.Contain("nicht lesbar"));
        });
    }

    [Test]
    public void Wenn_die_WebApi_beim_Archivieren_nicht_erreichbar_ist_dann_bleibt_die_Ausnahme_sichtbar()
    {
        using var fabrik = TestKlientFabrik.MitAntwortOhneRumpf(HttpStatusCode.InternalServerError);
        var klient = new KartenApiKlient(fabrik);

        Assert.That(async () => await klient.SchalteArchivierung(1, 8, new Archivierung(true)),
            Throws.TypeOf<HttpRequestException>());
    }
}
