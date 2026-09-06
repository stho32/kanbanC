using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Der Weg des Agenten: dieselben drei Routen, die die Oberflaeche ruft, nur ohne Browser. Eine
// eigene Datei statt eines weiteren Abschnitts in KartenEndpunkteTests — der Anhang ist das
// erste Thema dieses Projekts, dessen Antwort keine JSON ist.
public class AnhangEndpunkteTests
{
    private const string BoardsRoute = "/api/boards";
    private const string KontributorenRoute = "/api/kontributoren";
    private const int EinundvierzigKilobyte = 41000;

    [Test]
    public async Task Wenn_eine_Datei_angehaengt_wird_dann_antwortet_POST_anhaenge_mit_200_und_dem_ganzen_Kartendetail()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsync(
            Anhangroute(aufbau.KarteId),
            Multipart(Bytes(EinundvierzigKilobyte), "wbs-export.md", aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await AlsKartendetail(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(detail.Karte.Titel, Is.EqualTo("Playwright-Lizenz klären"));
            Assert.That(detail.Anhaenge[^1].Dateiname, Is.EqualTo("wbs-export.md"));
            Assert.That(detail.Anhaenge[^1].Dateigroesse, Is.EqualTo(EinundvierzigKilobyte));
            Assert.That(detail.Anhaenge[^1].AnhangId, Is.GreaterThan(0));
            Assert.That(detail.Anhaenge[^1].Urheber, Is.EqualTo(aufbau.Urheber));
        });
    }

    [Test]
    public async Task Wenn_eine_Datei_angehaengt_wird_dann_liegt_ihr_Zeitpunkt_im_Fenster_des_Aufrufs()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var vorher = DateTimeOffset.UtcNow.AddSeconds(-1);

        var detail = await HaengeAn(webApi, aufbau.KarteId, "wbs-export.md", EinundvierzigKilobyte, aufbau.Urheber.KontributorId);

        var nachher = DateTimeOffset.UtcNow.AddSeconds(1);
        Assert.That(detail.Anhaenge[0].Zeitpunkt, Is.GreaterThanOrEqualTo(vorher));
        Assert.That(detail.Anhaenge[0].Zeitpunkt, Is.LessThanOrEqualTo(nachher));
    }

    // Ein mitgeschicktes Zeitpunktfeld aendert nichts: die Route kennt keins.
    [Test]
    public async Task Wenn_der_Aufrufer_einen_Zeitpunkt_mitschickt_dann_steht_trotzdem_der_Zeitpunkt_der_Anwendung_in_der_Antwort()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var rumpf = Multipart(Bytes(100), "wbs-export.md", aufbau.Urheber.KontributorId);
        rumpf.Add(new StringContent("2020-01-01T00:00:00.0000000Z"), "zeitpunkt");

        using var antwort = await webApi.Klient.PostAsync(Anhangroute(aufbau.KarteId), rumpf);

        antwort.EnsureSuccessStatusCode();
        var detail = await AlsKartendetail(antwort);
        Assert.That(detail.Anhaenge[0].Zeitpunkt, Is.GreaterThan(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    // Das Rechenbeispiel aus US-4: die gemeldete Laenge zaehlt nicht.
    [Test]
    public async Task Wenn_eine_falsche_Laenge_gemeldet_wird_dann_steht_die_tatsaechlich_geschriebene_Groesse_in_der_Antwort()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var rumpf = new MultipartFormDataContent();
        var datei = new ByteArrayContent(Bytes(EinundvierzigKilobyte));
        datei.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        datei.Headers.ContentLength = 99999;
        rumpf.Add(datei, "datei", "wbs-export.md");
        rumpf.Add(new StringContent(aufbau.Urheber.KontributorId.ToString(CultureInfo.InvariantCulture)), "kontributor");

        using var antwort = await webApi.Klient.PostAsync(Anhangroute(aufbau.KarteId), rumpf);

        antwort.EnsureSuccessStatusCode();
        var detail = await AlsKartendetail(antwort);
        Assert.That(detail.Anhaenge[0].Dateigroesse, Is.EqualTo(EinundvierzigKilobyte));
    }

    // Das Rechenbeispiel der Anforderung: a.md, b.png, c.pdf, aeltester oben — und GET liefert
    // dieselbe Liste in derselben Reihenfolge.
    [Test]
    public async Task Wenn_drei_Dateien_angehaengt_werden_dann_liefert_GET_sie_in_derselben_Reihenfolge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        await HaengeAn(webApi, aufbau.KarteId, "a.md", 10, aufbau.Urheber.KontributorId);
        await HaengeAn(webApi, aufbau.KarteId, "b.png", 20, aufbau.Urheber.KontributorId);
        var nachDemAnhaengen = await HaengeAn(webApi, aufbau.KarteId, "c.pdf", 30, aufbau.Urheber.KontributorId);

        var gelesen = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(aufbau.KarteId));
        Assert.Multiple(() =>
        {
            Assert.That(nachDemAnhaengen.Anhaenge.Select(anhang => anhang.Dateiname), Is.EqualTo(new[] { "a.md", "b.png", "c.pdf" }));
            Assert.That(gelesen!.Anhaenge.Select(anhang => anhang.Dateiname), Is.EqualTo(new[] { "a.md", "b.png", "c.pdf" }));
            Assert.That(gelesen.Anhaenge.Select(anhang => anhang.AnhangId), Is.EqualTo(nachDemAnhaengen.Anhaenge.Select(anhang => anhang.AnhangId)));
        });
    }

    // Die Anhaenge haengen am Kartendetail und nicht an Karte: das Board liefert seine Karten
    // unveraendert.
    [Test]
    public async Task Wenn_eine_Karte_Anhaenge_traegt_dann_traegt_dieselbe_Karte_am_Board_keine_Anhangliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await HaengeAn(webApi, aufbau.KarteId, "wbs-export.md", EinundvierzigKilobyte, aufbau.Urheber.KontributorId);

        using var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{aufbau.BoardId}");

        antwort.EnsureSuccessStatusCode();
        var rohtext = await antwort.Content.ReadAsStringAsync();
        Assert.That(rohtext, Does.Not.Contain("anhaenge"));
        Assert.That(rohtext, Does.Not.Contain("wbs-export.md"));
    }

    [Test]
    public async Task Wenn_ein_Anhang_gelesen_wird_dann_kommen_die_Bytes_und_der_Originalname_im_Content_Disposition()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var inhalt = Bytes(EinundvierzigKilobyte);
        var angehaengt = await HaengeAn(webApi, aufbau.KarteId, "wbs-export.md", inhalt, aufbau.Urheber.KontributorId);
        var anhangId = angehaengt.Anhaenge[0].AnhangId;

        using var antwort = await webApi.Klient.GetAsync(Anhanginhaltsroute(aufbau.KarteId, anhangId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(async () =>
        {
            Assert.That(Gemeldeter(antwort), Is.EqualTo("wbs-export.md"));
            Assert.That(await antwort.Content.ReadAsByteArrayAsync(), Is.EqualTo(inhalt));
        });
    }

    // Keine JSON und keine Metadaten im Rumpf: die stehen im Kartendetail.
    [Test]
    public async Task Wenn_ein_Anhang_gelesen_wird_dann_traegt_die_Antwort_keine_JSON_und_keine_Metadaten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var angehaengt = await HaengeAn(webApi, aufbau.KarteId, "wbs-export.md", "Playwright-Lizenz klären"u8.ToArray(), aufbau.Urheber.KontributorId);

        using var antwort = await webApi.Klient.GetAsync(Anhanginhaltsroute(aufbau.KarteId, angehaengt.Anhaenge[0].AnhangId));

        var rohtext = await antwort.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(antwort.Content.Headers.ContentType?.MediaType, Is.Not.EqualTo("application/json"));
            Assert.That(rohtext, Is.EqualTo("Playwright-Lizenz klären"));
            Assert.That(rohtext, Does.Not.Contain("dateigroesse"));
        });
    }

    [Test]
    public async Task Wenn_ein_Anhang_entfernt_wird_dann_antwortet_DELETE_mit_200_und_dem_Kartendetail_ohne_ihn()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        await HaengeAn(webApi, aufbau.KarteId, "wbs-export.md", 10, aufbau.Urheber.KontributorId);
        var angehaengt = await HaengeAn(webApi, aufbau.KarteId, "burndown-r2.png", 20, aufbau.Urheber.KontributorId);
        var ersterId = angehaengt.Anhaenge[0].AnhangId;

        using var antwort = await webApi.Klient.DeleteAsync(Anhanginhaltsroute(aufbau.KarteId, ersterId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var detail = await AlsKartendetail(antwort);
        Assert.That(detail.Anhaenge.Select(anhang => anhang.Dateiname), Is.EqualTo(new[] { "burndown-r2.png" }));
        Assert.That(File.Exists(Anhangdatei(datenbank, aufbau.KarteId, ersterId)), Is.False, "Die Datei liegt noch in der Ablage.");
    }

    [Test]
    public async Task Wenn_derselbe_Anhang_ein_zweites_Mal_entfernt_wird_dann_antwortet_DELETE_mit_404_und_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var angehaengt = await HaengeAn(webApi, aufbau.KarteId, "wbs-export.md", 10, aufbau.Urheber.KontributorId);
        var anhangId = angehaengt.Anhaenge[0].AnhangId;
        using var erste = await webApi.Klient.DeleteAsync(Anhanginhaltsroute(aufbau.KarteId, anhangId));
        erste.EnsureSuccessStatusCode();

        using var antwort = await webApi.Klient.DeleteAsync(Anhanginhaltsroute(aufbau.KarteId, anhangId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "anhang-unbekannt");
    }

    // Eine AnhangId, die es gibt, aber an einer anderen Karte: 404, keine Bytes, und die andere
    // Karte behaelt ihren Anhang.
    [Test]
    public async Task Wenn_die_AnhangId_zu_einer_anderen_Karte_gehoert_dann_liefert_GET_404_und_keine_Bytes()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var zweite = await LegeKarteAn(webApi, aufbau.BoardId, "Migration schreiben");
        var angehaengt = await HaengeAn(webApi, zweite.KarteId, "wbs-export.md", 10, aufbau.Urheber.KontributorId);
        var anhangId = angehaengt.Anhaenge[0].AnhangId;

        using var antwort = await webApi.Klient.GetAsync(Anhanginhaltsroute(aufbau.KarteId, anhangId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Fremder Anhang");
        Assert.Multiple(async () =>
        {
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain(anhangId.ToString(CultureInfo.InvariantCulture)));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain(aufbau.KarteId.ToString(CultureInfo.InvariantCulture)));
            var fremde = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(zweite.KarteId));
            Assert.That(fremde!.Anhaenge, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Wenn_die_AnhangId_zu_einer_anderen_Karte_gehoert_dann_entfernt_DELETE_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var zweite = await LegeKarteAn(webApi, aufbau.BoardId, "Migration schreiben");
        var angehaengt = await HaengeAn(webApi, zweite.KarteId, "wbs-export.md", 10, aufbau.Urheber.KontributorId);
        var anhangId = angehaengt.Anhaenge[0].AnhangId;

        using var antwort = await webApi.Klient.DeleteAsync(Anhanginhaltsroute(aufbau.KarteId, anhangId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(File.Exists(Anhangdatei(datenbank, zweite.KarteId, anhangId)), Is.True);
    }

    // Der Fall aus US-2: die Zeile steht, die Datei ist ausserhalb der Anwendung verschwunden.
    [Test]
    public async Task Wenn_die_Datei_zu_einer_vorhandenen_Zeile_fehlt_dann_scheitert_GET_mit_Befund_statt_mit_leerem_Download()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var angehaengt = await HaengeAn(webApi, aufbau.KarteId, "wbs-export.md", 10, aufbau.Urheber.KontributorId);
        var anhangId = angehaengt.Anhaenge[0].AnhangId;
        File.Delete(Anhangdatei(datenbank, aufbau.KarteId, anhangId));

        using var antwort = await webApi.Klient.GetAsync(Anhanginhaltsroute(aufbau.KarteId, anhangId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "anhang-bytes-fehlen");
    }

    [Test]
    public async Task Wenn_der_Dateiname_leer_ist_dann_antwortet_POST_mit_400_und_Befund_und_es_entsteht_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsync(
            Anhangroute(aufbau.KarteId),
            MultipartMitLeeremNamen(Bytes(100), aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "anhang-name-leer");
        await ErwarteKarteOhneAnhang(webApi, aufbau.KarteId);
        Assert.That(Directory.Exists(datenbank.Ablageordner), Is.False);
    }

    [Test]
    public async Task Wenn_die_Datei_null_Bytes_traegt_dann_antwortet_POST_mit_400_und_Befund()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsync(
            Anhangroute(aufbau.KarteId),
            Multipart([], "leer.md", aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "anhang-leer");
        await ErwarteKarteOhneAnhang(webApi, aufbau.KarteId);
    }

    // Der gemeldete Weg eines fremden Rechners steht nirgends.
    [Test]
    public async Task Wenn_der_gemeldete_Name_einen_Pfad_traegt_dann_steht_nur_der_Dateiname_in_der_Zeile()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        await HaengeAn(webApi, aufbau.KarteId, @"C:\Temp\wbs-export.md", 100, aufbau.Urheber.KontributorId);
        var detail = await HaengeAn(webApi, aufbau.KarteId, "ordner/burndown-r2.png", 100, aufbau.Urheber.KontributorId);

        Assert.That(detail.Anhaenge.Select(anhang => anhang.Dateiname), Is.EqualTo(new[] { "wbs-export.md", "burndown-r2.png" }));
    }

    // 5 MB kommen durch — die Voreinstellungen des Rahmens stehen dem nicht mehr im Weg.
    [Test]
    public async Task Wenn_eine_fuenf_Megabyte_grosse_Datei_angehaengt_wird_dann_kommt_sie_durch()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var fuenfMegabyte = 5 * 1024 * 1024;

        var detail = await HaengeAn(webApi, aufbau.KarteId, "burndown-r2.png", fuenfMegabyte, aufbau.Urheber.KontributorId);

        Assert.That(detail.Anhaenge[0].Dateigroesse, Is.EqualTo(fuenfMegabyte));
    }

    // Die Obergrenze gilt am direkten API-Aufruf, der die Oberflaeche nicht benutzt — und der
    // Befund nennt sie in Bytes. Zurueck bleibt weder eine Zeile noch eine halbe Datei.
    [Test]
    public async Task Wenn_die_Datei_ein_Byte_ueber_der_Obergrenze_liegt_dann_antwortet_POST_mit_400_und_der_Befund_nennt_die_Obergrenze_in_Bytes()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsync(
            Anhangroute(aufbau.KarteId),
            Multipart(Bytes((int)Anhangsgrenze.HoechsteDateigroesse + 1), "film.mp4", aufbau.Urheber.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Datei ueber der Obergrenze");
        Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain(Anhangsgrenze.HoechsteDateigroesse.ToString(CultureInfo.InvariantCulture)));
        await ErwarteKarteOhneAnhang(webApi, aufbau.KarteId);
        Assert.That(Directory.Exists(datenbank.Ablageordner), Is.False, "In der Ablage liegt eine halbe Datei.");
    }

    [Test]
    public async Task Wenn_die_KarteId_unbekannt_ist_dann_antworten_alle_drei_Routen_mit_404_und_einem_Befund_ohne_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var angehaengt = await webApi.Klient.PostAsync(Anhangroute(999), Multipart(Bytes(100), "wbs-export.md", aufbau.Urheber.KontributorId));
        using var gelesen = await webApi.Klient.GetAsync(Anhanginhaltsroute(999, 1));
        using var entfernt = await webApi.Klient.DeleteAsync(Anhanginhaltsroute(999, 1));

        foreach (var antwort in new[] { angehaengt, gelesen, entfernt })
        {
            Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Unbekannte Karte");
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("karte-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("999"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("Board"));
        }
    }

    [Test]
    public async Task Wenn_die_KontributorId_unbekannt_ist_dann_antwortet_POST_mit_404_und_dem_Weg_zur_Kontributorenliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);

        using var antwort = await webApi.Klient.PostAsync(Anhangroute(aufbau.KarteId), Multipart(Bytes(100), "wbs-export.md", 999));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Unbekannter Urheber");
        Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-unbekannt"));
        Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("GET /api/kontributoren"));
        await ErwarteKarteOhneAnhang(webApi, aufbau.KarteId);
    }

    // 400 und nicht 404: es fehlt kein Ding, es wurde eine Regel verletzt. Und die Meldung spricht
    // vom Anhang, nicht vom Kommentar und nicht von der Verantwortung.
    [Test]
    public async Task Wenn_die_KontributorId_stillgelegt_ist_dann_antwortet_POST_mit_400_und_die_Meldung_spricht_vom_Anhang()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var maria = await LegeKontributorAn(webApi, "Maria Lenz", Kontributorart.Mensch);
        using var stillgelegt = await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{maria.KontributorId}/stilllegung", new Stilllegung(true));
        stillgelegt.EnsureSuccessStatusCode();

        using var antwort = await webApi.Klient.PostAsync(Anhangroute(aufbau.KarteId), Multipart(Bytes(100), "wbs-export.md", maria.KontributorId));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Stillgelegter Urheber");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("kontributor-stillgelegt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("verantwortlich"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Not.Contain("Kommentar"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("anhängen"));
        });
    }

    // Der Ablageordner entsteht neben der Datenbankdatei, benannt nach ihrem vollen Dateinamen.
    [Test]
    public async Task Wenn_eine_Datei_angehaengt_wird_dann_liegt_sie_im_Ordner_neben_der_Datenbankdatei_unter_KarteId_und_AnhangId()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await KarteMitUrheber(webApi);
        var inhalt = Bytes(EinundvierzigKilobyte);

        var detail = await HaengeAn(webApi, aufbau.KarteId, "wbs-export.md", inhalt, aufbau.Urheber.KontributorId);

        var pfad = Anhangdatei(datenbank, aufbau.KarteId, detail.Anhaenge[0].AnhangId);
        Assert.Multiple(() =>
        {
            Assert.That(datenbank.Ablageordner, Is.EqualTo(datenbank.Dateipfad + "-Files"));
            Assert.That(File.ReadAllBytes(pfad), Is.EqualTo(inhalt));
        });
    }

    private static async Task ErwarteKarteOhneAnhang(TestWebApi webApi, long karteId)
    {
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>(Kartendetailroute(karteId));
        Assert.That(detail!.Anhaenge, Is.Empty);
    }

    private static string Anhangdatei(TemporaereDatenbank datenbank, long karteId, long anhangId)
    {
        return Path.Combine(datenbank.Ablageordner, karteId.ToString(CultureInfo.InvariantCulture), anhangId.ToString(CultureInfo.InvariantCulture));
    }

    // Der Klient liest den Namen aus dem Kopf so, wie ihn ein Browser lesen wuerde: FileNameStar
    // gewinnt, wenn beide da sind.
    private static string? Gemeldeter(HttpResponseMessage antwort)
    {
        var kopf = antwort.Content.Headers.ContentDisposition;
        return (kopf?.FileNameStar ?? kopf?.FileName)?.Trim('"');
    }

    private static async Task<Kartendetail> HaengeAn(TestWebApi webApi, long karteId, string dateiname, int laenge, long kontributorId)
    {
        return await HaengeAn(webApi, karteId, dateiname, Bytes(laenge), kontributorId);
    }

    private static async Task<Kartendetail> HaengeAn(TestWebApi webApi, long karteId, string dateiname, byte[] inhalt, long kontributorId)
    {
        using var antwort = await webApi.Klient.PostAsync(Anhangroute(karteId), Multipart(inhalt, dateiname, kontributorId));
        antwort.EnsureSuccessStatusCode();
        return await AlsKartendetail(antwort);
    }

    // MultipartFormDataContent weist einen leeren Dateinamen selbst ab; ein Browser tut das nicht.
    // Der Kopf wird deshalb von Hand gesetzt, damit der Fall die Anwendung ueberhaupt erreicht.
    private static MultipartFormDataContent MultipartMitLeeremNamen(byte[] inhalt, long kontributorId)
    {
        var rumpf = new MultipartFormDataContent();
        var datei = new ByteArrayContent(inhalt);
        datei.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        datei.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        {
            Name = "\"datei\"",
            FileName = "\"   \"",
        };
        rumpf.Add(datei);
        rumpf.Add(new StringContent(kontributorId.ToString(CultureInfo.InvariantCulture)), "kontributor");
        return rumpf;
    }

    private static MultipartFormDataContent Multipart(byte[] inhalt, string dateiname, long kontributorId)
    {
        var rumpf = new MultipartFormDataContent();
        var datei = new ByteArrayContent(inhalt);
        datei.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        rumpf.Add(datei, "datei", dateiname);
        rumpf.Add(new StringContent(kontributorId.ToString(CultureInfo.InvariantCulture)), "kontributor");
        return rumpf;
    }

    private static byte[] Bytes(int laenge)
    {
        var inhalt = new byte[laenge];
        for (var stelle = 0; stelle < laenge; stelle++)
        {
            inhalt[stelle] = (byte)(stelle % 251);
        }

        return inhalt;
    }

    private static async Task<Kartendetail> AlsKartendetail(HttpResponseMessage antwort)
    {
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        Assert.That(detail, Is.Not.Null);
        return detail!;
    }

    private static string Anhangroute(long karteId)
    {
        return $"/api/karten/{karteId}/anhaenge";
    }

    private static string Anhanginhaltsroute(long karteId, long anhangId)
    {
        return $"/api/karten/{karteId}/anhaenge/{anhangId}";
    }

    private static string Kartendetailroute(long karteId)
    {
        return $"/api/karten/{karteId}";
    }

    private static async Task<Anhangaufbau> KarteMitUrheber(TestWebApi webApi)
    {
        var board = await LegeBoardAn(webApi);
        var karte = await LegeKarteAn(webApi, board.BoardId, "Playwright-Lizenz klären");
        var urheber = await LegeKontributorAn(webApi, "Stefan", Kontributorart.Mensch);
        return new Anhangaufbau(board.BoardId, karte.KarteId, urheber);
    }

    private sealed record Anhangaufbau(long BoardId, long KarteId, Kontributor Urheber);

    private static async Task<Karte> LegeKarteAn(TestWebApi webApi, long boardId, string titel)
    {
        var board = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{boardId}");
        using var antwort = await webApi.Klient.PostAsJsonAsync(
            $"{BoardsRoute}/{boardId}/spalten/{board!.Spalten[0].SpalteId}/karten",
            new KarteAnlegenAnfrage(titel));
        antwort.EnsureSuccessStatusCode();
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        Assert.That(karte, Is.Not.Null);
        return karte!;
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        Assert.That(board, Is.Not.Null);
        return board!;
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name, Kontributorart art)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage(name, art));
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(kontributor, Is.Not.Null);
        return kontributor!;
    }
}
