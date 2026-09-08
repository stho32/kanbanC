using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using KanbanC.Contracts.Boardimport;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.WebApi.IntegrationTests.Infrastructure;
using KanbanC.WebApi.IntegrationTests.Persistenz.Boardimport;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Der Weg des Agenten: **eine** Route, **ein** Aufruf, **eine** Transaktion — und mit
// trocken=true dieselbe Antwort, die der Mensch im Schirm sieht, ohne dass etwas entsteht.
public class BoardimportEndpunktTests
{
    private const string BoardsRoute = "/api/boards";
    private const string KontributorenRoute = "/api/kontributoren";
    private const string Importroute = "/api/boards/import";
    private const string Dateiname = "kanbanc-release-2-2026-09-08.kanbanc.json";

    // Der Zaehlerstand, den das Exportbeispiel der Kartenklasse und der Karte K24 gibt: WBS-32.
    private const int Exportbeispielzaehlerstand = 32;

    [Test]
    public async Task Wenn_trocken_gesetzt_ist_dann_kommt_200_mit_zehn_Zahlen_und_ohne_BoardId()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var antwort = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "true"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var bericht = await AlsBericht(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.BoardId, Is.Null, "Das Board entsteht erst beim Schreiben.");
            Assert.That(bericht.Boardname, Is.EqualTo("KanbanC — Release 2"));
            Assert.That(bericht.Zahlen.Spalten, Is.EqualTo(3));
            Assert.That(bericht.Zahlen.Kartenklassen, Is.EqualTo(1));
            Assert.That(bericht.Zahlen.Karten, Is.EqualTo(24));
            Assert.That(bericht.Zahlen.Zeiteintraege, Is.EqualTo(2));
            Assert.That(bericht.Anhanghinweis, Does.Contain("ohne Inhalt"));
        });
        var boardzahl = await Boardzahl(webApi);
        var personenzahl = await Personenzahl(webApi);
        Assert.Multiple(() =>
        {
            Assert.That(boardzahl, Is.EqualTo(2), "Ein trockener Lauf hat geschrieben.");
            Assert.That(personenzahl, Is.EqualTo(2), "Ein trockener Lauf hat Personen angelegt.");
        });
    }

    // **Die doppelten Namen stehen vor dem Schreiben da**: Stefan gibt es hier schon.
    [Test]
    public async Task Wenn_die_Vorschau_kommt_dann_nennt_sie_die_Namen_die_es_hier_schon_gibt()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var antwort = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "true"));

        var bericht = await AlsBericht(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.DoppelteNamen, Does.Contain("Stefan"));
            Assert.That(bericht.DoppelteNamen, Does.Not.Contain("Claude-Agent"));
        });
    }

    // Die Vorgabe ist true: ein vergessenes Feld erzeugt **nichts** — und es gäbe keinen Weg
    // zurück, weil im ganzen Bestand kein Board gelöscht werden kann.
    [Test]
    public async Task Wenn_trocken_fehlt_dann_kommt_200_und_es_wird_nichts_geschrieben()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var antwort = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await Boardzahl(webApi), Is.EqualTo(2));
    }

    [Test]
    public async Task Wenn_trocken_falsch_ist_dann_kommt_201_mit_neuer_BoardId_und_denselben_Zahlen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var vorschau = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "true"));
        using var antwort = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "false"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var bericht = await AlsBericht(antwort);
        var vorschaubericht = await AlsBericht(vorschau);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.BoardId, Is.EqualTo(3), "Die Nummer der Datei war 1; das neue Board bekommt eine neue.");
            Assert.That(bericht.Zahlen, Is.EqualTo(vorschaubericht.Zahlen));
        });
        Assert.That(await Boardzahl(webApi), Is.EqualTo(3));
        using var geoeffnet = await webApi.Klient.GetAsync($"{BoardsRoute}/{bericht.BoardId}");
        Assert.That(geoeffnet.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // **Kein Wiedererkennen**: dieselbe Datei zweimal eingelesen ergibt zwei unabhaengige Boards.
    [Test]
    public async Task Wenn_dieselbe_Datei_zweimal_eingelesen_wird_dann_entstehen_zwei_unabhaengige_Boards()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var ersterLauf = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "false"));
        using var zweiterLauf = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "false"));

        var erstes = await AlsBericht(ersterLauf);
        var zweites = await AlsBericht(zweiterLauf);
        Assert.That(zweites.BoardId, Is.Not.EqualTo(erstes.BoardId));
        Assert.That(await Boardzahl(webApi), Is.EqualTo(4));
    }

    // **Was ein Mensch liest, bleibt gleich**: die Kartennummer reist unveraendert mit, und der
    // Zählerstand der Klasse steht danach so, wie er in der Datei stand.
    [Test]
    public async Task Wenn_das_Board_entstanden_ist_dann_tragen_die_Karten_dieselben_Nummern_wie_in_der_Datei()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var antwort = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "false"));

        var bericht = await AlsBericht(antwort);
        var karten = await Karten(webApi, bericht.BoardId!.Value);
        var vollstaendige = karten.Single(karte => karte.Karte.Titel == "K24");
        var klassenlose = karten.Single(karte => karte.Karte.Titel == "K23");
        var archivierte = karten.Single(karte => karte.Karte.Titel == "K22");
        Assert.Multiple(() =>
        {
            Assert.That(vollstaendige.Karte.Kartennummer, Is.EqualTo("WBS-32"));
            Assert.That(klassenlose.Kartenklasse, Is.Null, "Die Karte ohne Klasse hat eine bekommen.");
            Assert.That(klassenlose.Karte.Kartennummer, Is.Null);
            Assert.That(archivierte.Archivstand.IstArchiviert, Is.True, "Die archivierte Karte kam aktiv an.");
            Assert.That(vollstaendige.Kartenklasse!.Zaehlerstand, Is.EqualTo(Exportbeispielzaehlerstand), "Der Zaehlerstand der Klasse ist nicht mitgereist.");
        });
    }

    // **Die Rechnung geht bis zur naechsten Karte auf**: der Zaehlerstand der Klasse reist mit,
    // also bekommt die naechste Karte am importierten Board die Nummer danach — und nicht wieder
    // die Eins.
    [Test]
    public async Task Wenn_am_importierten_Board_eine_Karte_angelegt_wird_dann_zaehlt_die_Klasse_hinter_dem_Stand_der_Datei_weiter()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();
        using var antwort = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "false"));
        var bericht = await AlsBericht(antwort);

        var neueKarteId = await LegeKarteAn(webApi, bericht.BoardId!.Value);
        var kartenklasseId = await KartenklasseDesBoards(webApi, bericht.BoardId.Value);
        using var zugeordnet = await webApi.Klient.PutAsJsonAsync($"/api/karten/{neueKarteId}/kartenklasse", new KartenklasseZuordnenAnfrage(kartenklasseId));

        zugeordnet.EnsureSuccessStatusCode();
        var karten = await Karten(webApi, bericht.BoardId.Value);
        var angelegte = karten.Single(karte => karte.Karte.KarteId == neueKarteId);
        Assert.That(angelegte.Karte.Kartennummer, Is.EqualTo($"WBS-{Exportbeispielzaehlerstand + 1}"), "Die Klasse hat wieder bei eins angefangen.");
    }

    // Die Bahn traegt ihre Abschlussmarke und ihre Anzeigegrenze weiter — und
    // UX_Spalte_Board_Bezeichnung greift nicht, obwohl „Betrieb" auch eine Spalte „Erledigt"
    // fuehrt: die Eindeutigkeit gilt je Board, und das Board ist neu.
    [Test]
    public async Task Wenn_das_Board_entstanden_ist_dann_traegt_die_Abschlussspalte_ihre_Marke_und_ihre_Anzeigegrenze()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var antwort = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "false"));

        var bericht = await AlsBericht(antwort);
        using var boardantwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{bericht.BoardId}");
        var board = await boardantwort.Content.ReadFromJsonAsync<Board>();
        var erledigt = board!.Spalten.Single(spalte => spalte.Bezeichnung == "Erledigt");
        Assert.Multiple(() =>
        {
            Assert.That(erledigt.IstAbschlussspalte, Is.True);
            Assert.That(erledigt.Anzeigegrenze, Is.EqualTo(20));
        });
    }

    // **Anhänge: Zeile ja, Bytes nein** — und der Abruf antwortet mit dem vorhandenen Befund,
    // nicht mit einem Absturz und nicht mit einem leeren Download, der wie ein Erfolg aussaehe.
    [Test]
    public async Task Wenn_ein_importierter_Anhang_abgerufen_wird_dann_antwortet_der_Befund_anhang_bytes_fehlen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();
        using var antwort = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "false"));
        var bericht = await AlsBericht(antwort);

        var karten = await Karten(webApi, bericht.BoardId!.Value);
        var anhang = karten.SelectMany(karte => karte.Anhaenge).Single();
        using var abruf = await webApi.Klient.GetAsync($"/api/karten/{karten.Single(karte => karte.Anhaenge.Count > 0).Karte.KarteId}/anhaenge/{anhang.AnhangId}");

        Assert.That(abruf.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(abruf, "Abruf eines importierten Anhangs");
        Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("anhang-bytes-fehlen"));
    }

    // Der stillgelegte Kontributor kommt stillgelegt an, und der laufende Zeiteintrag bleibt
    // laufend: ein Import, der daraus etwas anderes machte, erfände einen Zustand.
    [Test]
    public async Task Wenn_das_Board_entstanden_ist_dann_bleiben_Stilllegung_und_laufender_Zeiteintrag_erhalten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = await Boardimportbeispiel.FremdeBoarddatei();

        using var antwort = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "false"));

        var bericht = await AlsBericht(antwort);
        using var zeitenantwort = await webApi.Klient.GetAsync($"{BoardsRoute}/{bericht.BoardId}/zeiten");
        var zeiten = await zeitenantwort.Content.ReadFromJsonAsync<List<KanbanC.Contracts.Zeiten.Zeiteintrag>>();
        var laufender = zeiten!.Single(zeiteintrag => zeiteintrag.Ende is null);
        Assert.Multiple(() =>
        {
            Assert.That(laufender.Kontributor.Name, Is.EqualTo("Zora"));
            Assert.That(laufender.Kontributor.StillgelegtAm, Is.Not.Null, "Der stillgelegte Kontributor kam aktiv an.");
            Assert.That(laufender.Kontributor.KontributorId, Is.Not.EqualTo(2), "Der Kontributor der Datei wurde mit einem vorhandenen zusammengefuehrt.");
        });
    }

    [Test]
    public async Task Wenn_die_Datei_kein_JSON_ist_dann_kommt_400_mit_Befund_und_es_ist_nichts_geschrieben()
    {
        await ErwarteZurueckweisungOhneWirkung(Encoding.UTF8.GetBytes("Das ist ein Protokoll."), "boarddatei-unlesbar");
    }

    [Test]
    public async Task Wenn_die_Datei_gueltiges_JSON_ohne_Exportkopf_ist_dann_kommt_400_und_es_ist_nichts_geschrieben()
    {
        await ErwarteZurueckweisungOhneWirkung(Encoding.UTF8.GetBytes("{\"titel\":\"Eine ganz andere Datei\"}"), "boarddatei-unlesbar");
    }

    [Test]
    public async Task Wenn_die_Fassung_fremd_ist_dann_nennt_der_Befund_die_gefundene_und_die_erwartete_Zahl()
    {
        var datei = await Boardimportbeispiel.FremdeBoarddatei();
        var verdreht = Encoding.UTF8.GetString(datei).Replace("\"fassung\": 1", "\"fassung\": 2", StringComparison.Ordinal);

        var befund = await ErwarteZurueckweisungOhneWirkung(Encoding.UTF8.GetBytes(verdreht), "boarddatei-fassung-fremd");

        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Does.Contain("2"));
            Assert.That(befund.Meldung, Does.Contain("1"));
        });
    }

    [Test]
    public async Task Wenn_der_Anwendungsname_fremd_ist_dann_kommt_400_und_es_ist_nichts_geschrieben()
    {
        var datei = await Boardimportbeispiel.FremdeBoarddatei();
        var verdreht = Encoding.UTF8.GetString(datei).Replace("\"anwendung\": \"KanbanC\"", "\"anwendung\": \"Trello\"", StringComparison.Ordinal);

        var befund = await ErwarteZurueckweisungOhneWirkung(Encoding.UTF8.GetBytes(verdreht), "boarddatei-fassung-fremd");

        Assert.That(befund.Meldung, Does.Contain("Trello"));
    }

    // **Ein Verweis ins Leere weist die ganze Datei zurück** und nennt Zeilenart, Feld und
    // Nummer — ein halb eingelesenes Board ließe sich nicht mehr entfernen.
    [Test]
    public async Task Wenn_eine_Karte_auf_eine_fehlende_Spalte_zeigt_dann_nennt_der_Befund_Zeilenart_Feld_und_Nummer()
    {
        var datei = await Boardimportbeispiel.FremdeBoarddatei();
        var verdreht = MitOffenemSpaltenverweis(Encoding.UTF8.GetString(datei));

        var befund = await ErwarteZurueckweisungOhneWirkung(Encoding.UTF8.GetBytes(verdreht), "boarddatei-verweis-offen");

        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Does.Contain("Karte"));
            Assert.That(befund.Meldung, Does.Contain("spalte"));
            Assert.That(befund.Meldung, Does.Contain("999"));
        });
    }

    // **Geprueft wird auch bei trocken=false, und zwar vorher.**
    [Test]
    public async Task Wenn_ein_Verweis_ins_Leere_zeigt_und_geschrieben_werden_soll_dann_wird_vorher_zurueckgewiesen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);
        var datei = Encoding.UTF8.GetBytes(MitOffenemSpaltenverweis(Encoding.UTF8.GetString(await Boardimportbeispiel.FremdeBoarddatei())));

        using var antwort = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "false"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var boardzahl = await Boardzahl(webApi);
        var personenzahl = await Personenzahl(webApi);
        Assert.Multiple(() =>
        {
            Assert.That(boardzahl, Is.EqualTo(2));
            Assert.That(personenzahl, Is.EqualTo(2));
        });
    }

    private static string MitOffenemSpaltenverweis(string dateitext)
    {
        var stelle = dateitext.IndexOf("\"spalte\":", StringComparison.Ordinal);
        var ende = dateitext.IndexOf(',', stelle);
        return string.Concat(dateitext.AsSpan(0, stelle), "\"spalte\": 999", dateitext.AsSpan(ende));
    }

    private static async Task<KanbanC.Contracts.Fehler.Fehlerbefund> ErwarteZurueckweisungOhneWirkung(byte[] datei, string erwarteterCode)
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        await Boardimportbeispiel.LegeZweiBoardsAn(webApi, datenbank, mitArchiviertemProjektboard: false);

        using var antwort = await webApi.Klient.PostAsync(Importroute, Rumpf(datei, trocken: "true"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Boardimport mit einer Datei, die kein Board wiederherstellen kann");
        Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo(erwarteterCode));
        var boardzahl = await Boardzahl(webApi);
        var personenzahl = await Personenzahl(webApi);
        Assert.Multiple(() =>
        {
            Assert.That(boardzahl, Is.EqualTo(2), "Nach einer Zurueckweisung ist etwas geschrieben worden.");
            Assert.That(personenzahl, Is.EqualTo(2), "Nach einer Zurueckweisung sind Personen entstanden.");
        });
        return zurueckweisung.Befunde[0];
    }

    internal static MultipartFormDataContent Rumpf(byte[] datei, string? trocken, string dateiname = Dateiname)
    {
        var rumpf = new MultipartFormDataContent();
        var inhalt = new ByteArrayContent(datei);
        inhalt.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        rumpf.Add(inhalt, "datei", dateiname);
        if (trocken is not null)
        {
            rumpf.Add(new StringContent(trocken), "trocken");
        }

        return rumpf;
    }

    internal static async Task<Boardimportbericht> AlsBericht(HttpResponseMessage antwort)
    {
        var bericht = await antwort.Content.ReadFromJsonAsync<Boardimportbericht>();
        return bericht!;
    }

    private static async Task<int> Boardzahl(TestWebApi webApi)
    {
        var boards = await webApi.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);
        return boards!.Count;
    }

    private static async Task<int> Personenzahl(TestWebApi webApi)
    {
        var personen = await webApi.Klient.GetFromJsonAsync<List<KanbanC.Contracts.Kontributoren.Kontributor>>(KontributorenRoute);
        return personen!.Count;
    }

    private static async Task<long> LegeKarteAn(TestWebApi webApi, long boardId)
    {
        var board = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{boardId}");
        using var antwort = await webApi.Klient.PostAsJsonAsync(
            $"{BoardsRoute}/{boardId}/spalten/{board!.Spalten[0].SpalteId}/karten",
            new KarteAnlegenAnfrage("Die naechste Karte"));
        antwort.EnsureSuccessStatusCode();
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        return karte!.KarteId;
    }

    private static async Task<long> KartenklasseDesBoards(TestWebApi webApi, long boardId)
    {
        var klassen = await webApi.Klient.GetFromJsonAsync<List<Kartenklasse>>($"{BoardsRoute}/{boardId}/kartenklassen");
        return klassen!.Single().KartenklasseId;
    }

    private static async Task<List<Rohdatenkarte>> Karten(TestWebApi webApi, long boardId)
    {
        var karten = await webApi.Klient.GetFromJsonAsync<List<Rohdatenkarte>>($"{BoardsRoute}/{boardId}/karten");
        return karten!;
    }
}
