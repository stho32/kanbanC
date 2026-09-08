using System.Net;
using System.Net.Http.Json;
using System.Text;
using KanbanC.BL.Operations.Export;
using KanbanC.Contracts.Export;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using KanbanC.WebApi.IntegrationTests.Persistenz.Export;
using KanbanC.WebApi.IntegrationTests.Persistenz.Rohdaten;

namespace KanbanC.WebApi.IntegrationTests.Api;

// **Dieselbe Route bedient Browser-Download und Agenten-Abruf.** Geprüft wird an der Antwort
// allein — ohne Schirm.
public class ExportEndpunktTests
{
    private const string BoardsRoute = "/api/boards";
    private const string UnbekanntesBoard = "999";

    [Test]
    public async Task Wenn_die_Boarddatei_abgerufen_wird_dann_kommt_sie_als_Datei_mit_Namen_und_Inhaltstyp()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{aufbau.BoardId}/export.json");

        var erwarteterName = Exportdateiname.Fuer("KanbanC — Release 2", DateOnly.FromDateTime(DateTime.Now)); // stil-check: C03 die Uhr ist hier der Prüfgegenstand
        var angebotenerName = Dateiname(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(antwort.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));
            Assert.That(angebotenerName, Is.EqualTo(erwarteterName));
        });
    }

    // Die Zahlen des Rechenbeispiels an der Route: 24 Karten, 2 Zeiteinträge, 3 Spalten, 1
    // Kartenklasse, 3 Kontributoren — nicht mehr und nicht weniger.
    [Test]
    public async Task Wenn_die_Boarddatei_gelesen_wird_dann_traegt_sie_genau_den_Bestand_des_Boards()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var boardexport = await LiesBoarddatei(webApi, aufbau.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(boardexport.Karten, Has.Count.EqualTo(24));
            Assert.That(boardexport.Zeiteintraege, Has.Count.EqualTo(2));
            Assert.That(boardexport.Spalten, Has.Count.EqualTo(3));
            Assert.That(boardexport.Kartenklassen, Has.Count.EqualTo(1));
            Assert.That(boardexport.Kontributoren, Has.Count.EqualTo(3));
        });
    }

    // Der Kopf sagt die Lücke, statt sie zu verschweigen.
    [Test]
    public async Task Wenn_die_Boarddatei_gelesen_wird_dann_nennt_ihr_Kopf_Fassung_Zeitpunkt_Anwendung_und_die_fehlenden_Anhangbytes()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var boardexport = await LiesBoarddatei(webApi, aufbau.BoardId);

        Assert.Multiple(() =>
        {
            Assert.That(boardexport.Kopf.Anwendung, Is.EqualTo("KanbanC"));
            Assert.That(boardexport.Kopf.Fassung, Is.EqualTo(1));
            Assert.That(boardexport.Kopf.ErzeugtAm, Is.EqualTo(DateTimeOffset.Now).Within(TimeSpan.FromMinutes(5))); // stil-check: C03 die Uhr ist hier der Prüfgegenstand
            Assert.That(boardexport.Kopf.Anhanghinweis, Does.Contain("Bytes"));
            Assert.That(boardexport.Kopf.Anhanghinweis, Does.Contain("Anhänge"));
        });
    }

    // Die Metadaten der Anhänge reisen, die Bytes nicht — genau das, was der Kopf ansagt.
    [Test]
    public async Task Wenn_eine_Karte_einen_Anhang_traegt_dann_stehen_seine_Metadaten_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var boardexport = await LiesBoarddatei(webApi, aufbau.BoardId);

        var anhang = boardexport.Karten.Single(karte => karte.Karte.Karte.KarteId == aufbau.VollstaendigeId).Karte.Anhaenge.Single();
        Assert.Multiple(() =>
        {
            Assert.That(anhang.Dateiname, Is.EqualTo("bericht.pdf"));
            Assert.That(anhang.Dateigroesse, Is.EqualTo(2048));
            Assert.That(anhang.Urheber.KontributorId, Is.EqualTo(aufbau.StefanId));
        });
    }

    // „Ohne die Anwendung lesbar" heißt auch: ein Mensch, der die Datei im Editor öffnet, sieht
    // Umlaute und keine Escape-Folgen, und die Schachtelung ist eingerückt.
    [Test]
    public async Task Wenn_die_Datei_geoeffnet_wird_dann_stehen_Umlaute_im_Klartext_und_die_Schachtelung_eingerueckt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{aufbau.BoardId}/export.json");
        var satz = Encoding.UTF8.GetString(await antwort.Content.ReadAsByteArrayAsync());

        Assert.Multiple(() =>
        {
            Assert.That(satz, Does.Contain("Anhänge"));
            Assert.That(satz, Does.Not.Contain("\\u00E4"));
            Assert.That(satz, Does.Contain("\n  \"board\": {"));
        });
    }

    [Test]
    public async Task Wenn_es_das_Board_nicht_gibt_dann_kommt_404_mit_Grund_Werten_und_Kompensationsaktion()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Exportbeispiel.LegeAn(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{UnbekanntesBoard}/export.json");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Boarddatei eines unbekannten Boards");
        var befund = zurueckweisung.Befunde.Single();
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("board-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain(UnbekanntesBoard));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    // „Leer" und „gibt es nicht" bleiben unterscheidbar: das leere Board ergibt eine vollständige
    // Datei.
    [Test]
    public async Task Wenn_das_Board_keine_Karte_traegt_dann_kommt_200_mit_einer_vollstaendigen_Datei()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var leeresBoard = await Rechenbeispiel.LegeLeeresBoardAn(webApi);

        using var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{leeresBoard}/export.json");
        var boardexport = await LiesBoarddatei(webApi, leeresBoard);

        Assert.Multiple(() =>
        {
            Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(boardexport.Board.Name, Is.EqualTo("Frisch"));
            Assert.That(boardexport.Spalten, Has.Count.EqualTo(3));
            Assert.That(boardexport.Karten, Is.Empty);
            Assert.That(boardexport.Zeiteintraege, Is.Empty);
            Assert.That(boardexport.Kopf.Fassung, Is.EqualTo(1));
        });
    }

    // **Kein Abfrageparameter ändert den Inhalt**: aus dem Vollständigkeitsversprechen würde
    // sonst eine Option.
    [Test]
    public async Task Wenn_Abfrageparameter_mitgegeben_werden_dann_bleibt_die_Datei_dieselbe()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Exportbeispiel.LegeAn(webApi, datenbank);

        var ohneParameter = await LiesBoarddatei(webApi, aufbau.BoardId);
        using var mitParametern = await webApi.Klient.GetAsync($"{BoardsRoute}/{aufbau.BoardId}/export.json?archiviert=true&seitengroesse=5&von=2026-09-01");
        var gefiltert = await LiesAls<Boardexport>(mitParametern);

        Assert.Multiple(() =>
        {
            Assert.That(mitParametern.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(gefiltert.Karten, Has.Count.EqualTo(ohneParameter.Karten.Count));
            Assert.That(gefiltert.Zeiteintraege, Has.Count.EqualTo(ohneParameter.Zeiteintraege.Count));
        });
    }

    // Content-Disposition nennt den Namen je nach Zeichensatz in filename oder filename*.
    private static string Dateiname(HttpResponseMessage antwort)
    {
        var inhaltsangabe = antwort.Content.Headers.ContentDisposition!;
        var derNameStehtInDerErweitertenForm = inhaltsangabe.FileNameStar is not null;
        if (derNameStehtInDerErweitertenForm)
        {
            return inhaltsangabe.FileNameStar!;
        }

        return inhaltsangabe.FileName!;
    }

    private static async Task<Boardexport> LiesBoarddatei(TestWebApi webApi, long boardId)
    {
        using var antwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{boardId}/export.json");
        antwort.EnsureSuccessStatusCode();
        return await LiesAls<Boardexport>(antwort);
    }

    private static async Task<T> LiesAls<T>(HttpResponseMessage antwort)
    {
        var wert = await antwort.Content.ReadFromJsonAsync<T>();
        if (wert is null)
        {
            throw new InvalidOperationException("Die API hat keine Boarddatei zurückgegeben.");
        }

        return wert;
    }
}
