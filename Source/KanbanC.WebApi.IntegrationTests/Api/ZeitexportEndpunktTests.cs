using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Dapper;
using KanbanC.Contracts.Auswertungen;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Der Weg des Agenten zur Datei: `zeitexport.csv` liefert die Bytes, `zeitexport` nur den Stand.
// **Beide entstehen aus einem Dienstaufruf** — der Schirm zählt deshalb nie etwas anderes, als in
// der Datei steht.
public class ZeitexportEndpunktTests
{
    private const string BoardsRoute = "/api/boards";
    private const string Isodatumsformat = "yyyy-MM-dd";
    private const string Kopfzeile = "Kartennummer;Kartentitel;Kontributor;Art;Beginn;Ende;Dauer";
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly DateOnly EndeAugust = new(2026, 8, 31);
    private static readonly DateOnly Sechster = new(2026, 9, 6);
    private static readonly DateOnly Siebter = new(2026, 9, 7);
    private static readonly DateTimeOffset UeberZweiMitternachte = new(2026, 8, 31, 22, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UeberZweiMitternachteEnde = new(2026, 9, 2, 5, 40, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AmSechsten = new(2026, 9, 6, 14, 2, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AmSechstenEnde = new(2026, 9, 6, 14, 50, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AmSiebten = new(2026, 9, 7, 9, 12, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AmSiebtenEnde = new(2026, 9, 7, 11, 24, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AmSiebtenZweiter = new(2026, 9, 7, 9, 40, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AmSiebtenZweiterEnde = new(2026, 9, 7, 9, 52, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LaufenderBeginn = new(2026, 9, 7, 13, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Wenn_die_Datei_abgerufen_wird_dann_kommt_200_mit_text_csv_und_dem_gerechneten_Dateinamen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync(Dateiroute(aufbau, string.Empty));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(() =>
        {
            Assert.That(antwort.Content.Headers.ContentType!.MediaType, Is.EqualTo("text/csv"));
            Assert.That(antwort.Content.Headers.ContentDisposition!.FileName, Does.Contain("kanbanc-release-2-zeiten-2026-08-31_2026-09-07.csv"));
        });
    }

    // Das durchgehende Rechenbeispiel über die echte Ablage: fünf Zeilen in Beginn-Folge.
    [Test]
    public async Task Wenn_die_Datei_ohne_Spanne_abgerufen_wird_dann_traegt_sie_fuenf_Zeilen_in_Beginn_Folge()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        var zeilen = await Datensatzzeilen(webApi, aufbau, string.Empty);

        Assert.That(Kartennummern(zeilen), Is.EqualTo(new[] { "WBS-01", "WBS-02", "WBS-03", "WBS-03", "WBS-02" }));
    }

    [Test]
    public async Task Wenn_die_Datei_gelesen_wird_dann_beginnt_sie_mit_dem_BOM_und_der_Kopfzeile()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        var bytes = await Dateibytes(webApi, aufbau, string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(bytes[..3], Is.EqualTo(Utf8Bom));
            Assert.That(Encoding.UTF8.GetString(bytes[3..]), Does.StartWith(Kopfzeile + "\r\n"));
        });
    }

    // Der laufende Eintrag steht mit darin, die archivierte Karte auch — und die Dauer über zwei
    // Mitternachte lautet 31:40 in **einer** Zeile.
    [Test]
    public async Task Wenn_die_Datei_gelesen_wird_dann_glaettet_sie_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        var satz = Encoding.UTF8.GetString((await Dateibytes(webApi, aufbau, string.Empty))[3..]);

        Assert.Multiple(() =>
        {
            Assert.That(satz, Does.Contain(";31:40"));
            Assert.That(satz, Does.Contain("WBS-01"));
            Assert.That(satz, Does.EndWith(";;\r\n"));
            Assert.That(satz, Does.Not.Contain("Summe"));
        });
    }

    [Test]
    public async Task Wenn_der_Stand_abgerufen_wird_dann_traegt_er_die_Zaehlung_und_keine_Zeilen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync(Standroute(aufbau, string.Empty));
        var rumpf = await antwort.Content.ReadAsStringAsync();
        var stand = await LiesStand(webApi, aufbau, string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(stand.Eintraege, Is.EqualTo(5));
            Assert.That(stand.Karten, Is.EqualTo(3));
            Assert.That(stand.Kontributoren, Is.EqualTo(2));
            Assert.That(stand.Laufende, Is.EqualTo(1));
            Assert.That(stand.Von, Is.EqualTo(EndeAugust));
            Assert.That(stand.Bis, Is.EqualTo(Siebter));
            Assert.That(stand.Dateiname, Is.EqualTo("kanbanc-release-2-zeiten-2026-08-31_2026-09-07.csv"));
            Assert.That(rumpf, Does.Not.Contain("zeilen"));
            Assert.That(rumpf, Does.Not.Contain("WBS-"));
        });
    }

    // Geschnitten wird am **Beginn**: der Eintrag vom 31.08. fällt heraus, obwohl er bis zum 02.09.
    // lief — und er wird dabei nicht gekürzt.
    [Test]
    public async Task Wenn_von_gesetzt_ist_dann_faellt_der_Eintrag_mit_frueherem_Beginn_ganz_heraus()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        var zeilen = await Datensatzzeilen(webApi, aufbau, "?von=2026-09-01");

        Assert.Multiple(() =>
        {
            Assert.That(zeilen, Has.Length.EqualTo(4));
            Assert.That(Kartennummern(zeilen), Has.None.EqualTo("WBS-01"));
        });
    }

    [Test]
    public async Task Wenn_beide_Grenzen_denselben_Tag_nennen_dann_bleibt_genau_der_Eintrag_dieses_Tages()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        var stand = await LiesStand(webApi, aufbau, "?von=2026-09-06&bis=2026-09-06");
        var zeilen = await Datensatzzeilen(webApi, aufbau, "?von=2026-09-06&bis=2026-09-06");

        Assert.Multiple(() =>
        {
            Assert.That(zeilen, Has.Length.EqualTo(1));
            Assert.That(stand.Von, Is.EqualTo(Sechster));
            Assert.That(stand.Bis, Is.EqualTo(Sechster));
        });
    }

    // „bis" gilt bis zum Ende seines Tages.
    [Test]
    public async Task Wenn_nur_bis_gesetzt_ist_dann_bleiben_die_beiden_Eintraege_bis_zu_diesem_Tag()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        var zeilen = await Datensatzzeilen(webApi, aufbau, "?bis=2026-09-06");

        Assert.That(Kartennummern(zeilen), Is.EqualTo(new[] { "WBS-01", "WBS-02" }));
    }

    // Ein leerer Ausschnitt ist **200 mit der Kopfzeile allein** — nicht 404 und kein leerer Rumpf.
    // Die gelieferten Grenzen stehen dann beide auf heute, und der Dateiname nennt sie.
    [Test]
    public async Task Wenn_der_Ausschnitt_leer_bleibt_dann_kommt_200_mit_der_Kopfzeile_allein()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync(Dateiroute(aufbau, "?von=2026-09-08"));
        var bytes = await antwort.Content.ReadAsByteArrayAsync();
        var stand = await LiesStand(webApi, aufbau, "?von=2026-09-08");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.Multiple(() =>
        {
            Assert.That(Encoding.UTF8.GetString(bytes[3..]), Is.EqualTo(Kopfzeile + "\r\n"));
            Assert.That(stand.Eintraege, Is.Zero);
            Assert.That(stand.Von, Is.EqualTo(Heute()));
            Assert.That(stand.Bis, Is.EqualTo(Heute()));
            Assert.That(stand.Dateiname, Does.Contain(Heute().ToString(Isodatumsformat, CultureInfo.InvariantCulture)));
        });
    }

    [Test]
    public async Task Wenn_der_Bestand_keinen_Zeiteintrag_fuehrt_dann_kommt_200_mit_der_Kopfzeile_und_nicht_404()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi, "Frisch");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);
        var aufbau = new Probeaufbau(board.BoardId, kartenklasse.KartenklasseId);

        using var antwort = await webApi.Klient.GetAsync(Dateiroute(aufbau, string.Empty));
        var bytes = await antwort.Content.ReadAsByteArrayAsync();

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(Encoding.UTF8.GetString(bytes[3..]), Is.EqualTo(Kopfzeile + "\r\n"));
    }

    [TestCase("?von=gestern", "von")]
    [TestCase("?bis=uebermorgen", "bis")]
    public async Task Wenn_eine_Grenze_unlesbar_ist_dann_kommt_400_mit_dem_gelesenen_Wert_und_ihrem_Namen(string abfrage, string parametername)
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync(Dateiroute(aufbau, abfrage));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var befund = (await Fehlerrumpf.Lies(antwort, "Zeitexport mit unlesbarer Grenze")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeitraum-filter-unlesbar"));
            Assert.That(befund.Meldung, Does.Contain(parametername));
            Assert.That(befund.Meldung, Does.Contain(Isodatumsformat));
            Assert.That(befund.Kompensation, Does.Contain(Dateiroute(aufbau, string.Empty)));
        });
    }

    // Die verdrehte Spanne wird an der Grenze zurückgewiesen — nicht im Dienst und nicht im Schirm.
    [Test]
    public async Task Wenn_bis_vor_von_liegt_dann_kommt_400_mit_beiden_Werten_und_der_Kompensation()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync(Standroute(aufbau, "?von=2026-09-07&bis=2026-09-01"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var befund = (await Fehlerrumpf.Lies(antwort, "Zeitexport mit verdrehter Spanne")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeitraum-filter-verdreht"));
            Assert.That(befund.Meldung, Does.Contain("2026-09-07"));
            Assert.That(befund.Meldung, Does.Contain("2026-09-01"));
            Assert.That(befund.Kompensation, Does.Contain("tauschen"));
        });
    }

    [Test]
    public async Task Wenn_eine_Grenze_fehlt_dann_ist_das_kein_Fehler()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        using var nurVon = await webApi.Klient.GetAsync(Dateiroute(aufbau, "?von=2026-09-01"));
        using var nurBis = await webApi.Klient.GetAsync(Dateiroute(aufbau, "?bis=2026-09-06"));

        Assert.Multiple(() =>
        {
            Assert.That(nurVon.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(nurBis.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task Wenn_es_das_Board_nicht_gibt_dann_kommt_404_mit_Grund_und_Kompensationsaktion()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync(Dateiroute(new Probeaufbau(999, aufbau.KartenklasseId), string.Empty));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var befund = (await Fehlerrumpf.Lies(antwort, "Zeitexport mit unbekannter BoardId")).Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("board-unbekannt"));
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/boards"));
        });
    }

    [Test]
    public async Task Wenn_es_die_Kartenklasse_nicht_gibt_dann_kommt_404_mit_ihrem_eigenen_Code()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        using var antwort = await webApi.Klient.GetAsync(Standroute(new Probeaufbau(aufbau.BoardId, 999), string.Empty));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kartenklasse-unbekannt");
    }

    [Test]
    public async Task Wenn_die_Kartenklasse_einem_fremden_Board_gehoert_dann_kommt_404_mit_dem_eigenen_Code()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);
        var nachbar = await LegeBoardAn(webApi, "Beschaffung");
        var fremdeKartenklasse = await LegeKartenklasseAn(webApi, nachbar.BoardId);

        using var antwort = await webApi.Klient.GetAsync(Dateiroute(new Probeaufbau(aufbau.BoardId, fremdeKartenklasse.KartenklasseId), string.Empty));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kartenklasse-fremd");
    }

    // **Ein Dienstaufruf für beide Routen**: die Zahl im Stand ist die Zahl der Zeilen in der Datei.
    [Test]
    public async Task Wenn_Stand_und_Datei_denselben_Ausschnitt_rufen_dann_nennen_sie_dieselbe_Zahl()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeRechenbeispielAn(webApi, datenbank);

        var stand = await LiesStand(webApi, aufbau, "?von=2026-09-01");
        var zeilen = await Datensatzzeilen(webApi, aufbau, "?von=2026-09-01");

        Assert.That(zeilen, Has.Length.EqualTo(stand.Eintraege));
    }

    private static string[] Kartennummern(string[] zeilen)
    {
        var nummern = new List<string>();
        foreach (var zeile in zeilen)
        {
            nummern.Add(zeile.Split(';')[0]);
        }

        return nummern.ToArray();
    }

    // Die Kopfzeile heraus; der Umbruch im maskierten Titel ist ein nacktes „\n" und trennt keine
    // Zeile.
    private static async Task<string[]> Datensatzzeilen(TestWebApi webApi, Probeaufbau aufbau, string abfrage)
    {
        var bytes = await Dateibytes(webApi, aufbau, abfrage);
        var satz = Encoding.UTF8.GetString(bytes[3..]);
        return satz.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)[1..];
    }

    private static async Task<byte[]> Dateibytes(TestWebApi webApi, Probeaufbau aufbau, string abfrage)
    {
        using var antwort = await webApi.Klient.GetAsync(Dateiroute(aufbau, abfrage));
        antwort.EnsureSuccessStatusCode();
        return await antwort.Content.ReadAsByteArrayAsync();
    }

    private static async Task<Zeitexportstand> LiesStand(TestWebApi webApi, Probeaufbau aufbau, string abfrage)
    {
        var stand = await webApi.Klient.GetFromJsonAsync<Zeitexportstand>(Standroute(aufbau, abfrage));
        Assert.That(stand, Is.Not.Null, "Die API hat keinen Zeitexportstand zurückgegeben.");
        return stand!;
    }

    private static string Dateiroute(Probeaufbau aufbau, string abfrage)
    {
        return $"{BoardsRoute}/{aufbau.BoardId}/kartenklassen/{aufbau.KartenklasseId}/zeitexport.csv{abfrage}";
    }

    private static string Standroute(Probeaufbau aufbau, string abfrage)
    {
        return $"{BoardsRoute}/{aufbau.BoardId}/kartenklassen/{aufbau.KartenklasseId}/zeitexport{abfrage}";
    }

    private static DateOnly Heute()
    {
        return DateOnly.FromDateTime(DateTime.Today); // stil-check: C03 dieselbe Uhr wie die WebApi, deren Leerfall der Test prüft
    }

    // Das Rechenbeispiel der Anforderung: fünf Einträge auf drei Karten, zwei Kontributoren, einer
    // läuft, einer geht über zwei Mitternachte, WBS-01 ist archiviert und trägt einen Titel mit
    // Semikolon, Anführungszeichen und Umbruch.
    private static async Task<Probeaufbau> LegeRechenbeispielAn(TestWebApi webApi, TemporaereDatenbank datenbank)
    {
        var board = await LegeBoardAn(webApi, "KanbanC — Release 2");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId);
        var spalteId = board.Spalten[0].SpalteId;
        var agent = await LegeKontributorAn(webApi, "Claude-Agent", Kontributorart.Agent);
        var stefan = await LegeKontributorAn(webApi, "Stefan", Kontributorart.Mensch);
        var aufbau = new Probeaufbau(board.BoardId, kartenklasse.KartenklasseId);

        var erste = await LegeKlassenkarteAn(webApi, aufbau, "Titel; mit \"Zitat\"\nund Umbruch", spalteId);
        var zweite = await LegeKlassenkarteAn(webApi, aufbau, "Timer stoppen", spalteId);
        var dritte = await LegeKlassenkarteAn(webApi, aufbau, "WBS-Datei importieren", spalteId);
        using var archiviert = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}/karten/{erste}/archivierung", new Archivierung(true));
        archiviert.EnsureSuccessStatusCode();

        await TrageZeitNach(webApi, erste, agent.KontributorId, UeberZweiMitternachte, UeberZweiMitternachteEnde);
        await TrageZeitNach(webApi, zweite, stefan.KontributorId, AmSechsten, AmSechstenEnde);
        await TrageZeitNach(webApi, dritte, agent.KontributorId, AmSiebten, AmSiebtenEnde);
        await TrageZeitNach(webApi, dritte, stefan.KontributorId, AmSiebtenZweiter, AmSiebtenZweiterEnde);
        var laufender = await StarteZeitmessung(webApi, zweite, stefan.KontributorId);
        DatiereBeginnUm(datenbank, laufender, LaufenderBeginn);
        return aufbau;
    }

    // Ein Timer beginnt immer jetzt; für ein festes Rechenbeispiel schreibt der Test den Beginn um
    // — derselbe Zustand, den ein am 07.09. gestarteter Timer hätte.
    private static void DatiereBeginnUm(TemporaereDatenbank datenbank, long zeiteintragId, DateTimeOffset beginn)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var betroffeneZeilen = verbindung.Execute(@"
            UPDATE Zeiteintrag
               SET Beginn = @Beginn
             WHERE ZeiteintragId = @ZeiteintragId",
            new { ZeiteintragId = zeiteintragId, Beginn = beginn.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) });
        Assert.That(betroffeneZeilen, Is.EqualTo(1), $"Der Zeiteintrag {zeiteintragId} war nicht umzudatieren.");
    }

    private static async Task TrageZeitNach(TestWebApi webApi, long karteId, long kontributorId, DateTimeOffset beginn, DateTimeOffset ende)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/zeiten", new ZeiteintragNachtragenAnfrage(kontributorId, beginn, ende));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task<long> StarteZeitmessung(TestWebApi webApi, long karteId, long kontributorId)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/zeiten/laufend", new ZeitmessungStartenAnfrage(kontributorId));
        antwort.EnsureSuccessStatusCode();
        var zeiteintrag = (await antwort.Content.ReadFromJsonAsync<Zeiteintrag>())!;
        return zeiteintrag.ZeiteintragId;
    }

    private static async Task<long> LegeKlassenkarteAn(TestWebApi webApi, Probeaufbau aufbau, string titel, long spalteId)
    {
        using var angelegt = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{aufbau.BoardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        angelegt.EnsureSuccessStatusCode();
        var karte = (await angelegt.Content.ReadFromJsonAsync<Karte>())!;
        using var zugeordnet = await webApi.Klient.PutAsJsonAsync($"/api/karten/{karte.KarteId}/kartenklasse", new KartenklasseZuordnenAnfrage(aufbau.KartenklasseId));
        zugeordnet.EnsureSuccessStatusCode();
        return karte.KarteId;
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name, Kontributorart art)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync("/api/kontributoren", new KontributorAnlegenAnfrage(name, art));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Kontributor>())!;
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, string name)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage(name, BoardArt.Projekt, null, null));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Board>())!;
    }

    private static async Task<Kartenklasse> LegeKartenklasseAn(TestWebApi webApi, long boardId)
    {
        using var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/kartenklassen", new KartenklasseAnlegenAnfrage("WBS", "WBS-"));
        antwort.EnsureSuccessStatusCode();
        return (await antwort.Content.ReadFromJsonAsync<Kartenklasse>())!;
    }

    private sealed record Probeaufbau(long BoardId, long KartenklasseId);
}
