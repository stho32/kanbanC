using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Import;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Der Weg des Agenten: dieselbe Route, die die Oberfläche ruft, nur ohne Browser — und mit
// `trocken=true` dieselbe Antwort, die der Mensch als Schritt 2 sieht.
public class WbsImportEndpunkteTests
{
    private const string BoardsRoute = "/api/boards";
    private const string KontributorenRoute = "/api/kontributoren";

    [Test]
    public async Task Wenn_trocken_gesetzt_ist_dann_kommt_200_mit_Bilanz_und_es_steht_keine_Karte_mehr_auf_dem_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "true"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var bericht = await AlsBericht(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.EqualTo(2));
            Assert.That(bericht.Geaendert, Is.Zero);
            Assert.That(bericht.Unveraendert, Is.Zero);
        });
        Assert.That(await Kartenzahl(webApi, aufbau.Board.BoardId), Is.Zero, "Ein trockener Lauf hat geschrieben.");
    }

    // Die Vorgabe ist true: ein vergessenes Feld erzeugt **nichts**.
    [Test]
    public async Task Wenn_trocken_fehlt_dann_wird_nichts_geschrieben()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: null));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await Kartenzahl(webApi, aufbau.Board.BoardId), Is.Zero);
    }

    // Die Schnittebene reist als **Wort**, wie Ereignisweg und Kontributorart — ein Agent liest,
    // was dasteht.
    [Test]
    public async Task Wenn_die_Schnittebene_als_Wort_kommt_dann_wird_sie_verstanden()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        var rumpf = Rumpf(aufbau, trocken: "true");
        rumpf.Add(new StringContent("Dialog"), "schnittebene");

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), rumpf);

        var bericht = await AlsBericht(antwort);
        Assert.That(bericht.Angelegt, Is.EqualTo(1), "Auf Dialog-Schnitt entsteht genau eine Karte.");
    }

    [Test]
    public async Task Wenn_die_Schnittebene_fehlt_dann_gilt_Interaction()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "true"));

        var bericht = await AlsBericht(antwort);
        Assert.That(bericht.Angelegt, Is.EqualTo(bericht.Kartenzahlen.Interaction));
    }

    [Test]
    public async Task Wenn_die_Bilanz_kommt_dann_nennt_sie_die_Kartenzahl_je_Wahl_der_Schnittebene()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "true"));

        var bericht = await AlsBericht(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Kartenzahlen.Dialog, Is.EqualTo(1));
            Assert.That(bericht.Kartenzahlen.Interaction, Is.EqualTo(2));
            Assert.That(bericht.Zeilen, Has.Count.EqualTo(6));
            Assert.That(bericht.Zeilen[0].Wirkung, Is.EqualTo(Importwirkung.Zielboard));
        });
    }

    [Test]
    public async Task Wenn_ohne_trocken_geschrieben_wird_dann_kommt_201_und_die_Karten_stehen_auf_dem_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "false"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var bericht = await AlsBericht(antwort);
        var board = await LiesBoard(webApi, aufbau.Board.BoardId);
        var abschlussspalte = board.Spalten.Single(spalte => spalte.IstAbschlussspalte);
        var ersteSpalte = board.Spalten.OrderBy(spalte => spalte.Position).First();
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.EqualTo(2));
            Assert.That(abschlussspalte.Karten.Single().Titel, Is.EqualTo("[I0001] Board anlegen"));
            Assert.That(abschlussspalte.Karten.Single().Kartennummer, Is.EqualTo("WBS-01"));
            Assert.That(abschlussspalte.Karten.Single().ErledigtAm, Is.EqualTo(DateOnly.FromDateTime(DateTime.Today)));
            Assert.That(ersteSpalte.Karten.Single().Titel, Is.EqualTo("[I0002] Boards auflisten"));
            Assert.That(ersteSpalte.Karten.Single().Kartennummer, Is.EqualTo("WBS-02"));
            Assert.That(ersteSpalte.Karten.Single().ErledigtAm, Is.Null);
        });
    }

    // **Das Fertig-Kriterium von F0058 als Testfall:** die 201-Antwort ist der Bericht, und jede
    // angelegte Zeile nennt eine Karte, die es wirklich gibt — nachgewiesen durch den Abruf unter
    // genau dieser KarteId.
    [Test]
    public async Task Wenn_ohne_trocken_geschrieben_wird_dann_zeigt_jede_angelegte_Zeile_auf_eine_wirklich_vorhandene_Karte()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "false"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var bericht = await AlsBericht(antwort);
        var angelegte = bericht.Zeilen.Where(zeile => zeile.Wirkung == Importwirkung.Angelegt).ToList();
        Assert.That(angelegte, Has.Count.EqualTo(2));
        foreach (var zeile in angelegte)
        {
            Assert.That(zeile.KarteId, Is.Not.Null, $"Die Zeile {zeile.Kennung} nennt keine KarteId.");
            var detail = await webApi.Klient.GetFromJsonAsync<KanbanC.Contracts.Karten.Kartendetail>($"/api/karten/{zeile.KarteId!.Value}");
            Assert.That(detail, Is.Not.Null, $"Die KarteId {zeile.KarteId} findet keine Karte.");
            Assert.That(detail!.Karte.Kartennummer, Is.EqualTo(zeile.Kartennummer));
            Assert.That(detail.Karte.Titel, Does.Contain(zeile.Kennung));
        }
    }

    // **Die Ankuendigung nennt keine Nummer**: die Karte gibt es noch nicht, und der Unterschied
    // zwischen „so sähe es aus" und „so ist es jetzt" ist Absicht, keine Luecke.
    [Test]
    public async Task Wenn_trocken_gesetzt_ist_dann_bleiben_die_angelegten_Zeilen_ohne_Nummer_und_ohne_KarteId()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "true"));

        var bericht = await AlsBericht(antwort);
        var angelegte = bericht.Zeilen.Where(zeile => zeile.Wirkung == Importwirkung.Angelegt).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(angelegte.Select(zeile => zeile.Kartennummer), Is.All.Null);
            Assert.That(angelegte.Select(zeile => zeile.KarteId), Is.All.Null);
        });
        Assert.That(await Kartenzahl(webApi, aufbau.Board.BoardId), Is.Zero, "Ein trockener Lauf hat geschrieben.");
    }

    // Der Kopf steht **im Bericht**: sonst haette ihn weder der Agent noch der kopierte Text. Der
    // Pfad ist der der Anfrage, nicht der Dateiname allein.
    [Test]
    public async Task Wenn_der_Bericht_kommt_dann_nennt_sein_Laufkopf_Zeitpunkt_Urheber_und_den_Pfad_der_Anfrage()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        var vorDemLauf = DateTimeOffset.UtcNow.AddSeconds(-1);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "false"));

        var bericht = await AlsBericht(antwort);
        Assert.That(bericht.Laufkopf, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Laufkopf!.Urhebernummer, Is.EqualTo(aufbau.Kontributor.KontributorId));
            Assert.That(bericht.Laufkopf.Urhebername, Is.EqualTo("Stefan"));
            Assert.That(bericht.Laufkopf.Pfad, Is.EqualTo("Dokumentation/Planung/probe.md"));
            Assert.That(bericht.Laufkopf.Zeitpunkt, Is.GreaterThanOrEqualTo(vorDemLauf));
            Assert.That(bericht.Laufkopf.Zeitpunkt, Is.LessThanOrEqualTo(DateTimeOffset.UtcNow.AddSeconds(1)));
        });
    }

    // Fehlt der Pfad an der Anfrage, nennt der Kopf den Dateinamen — dieselbe Regel wie beim
    // Dateiverweis der Karten.
    [Test]
    public async Task Wenn_der_Pfad_fehlt_dann_nennt_der_Laufkopf_den_Dateinamen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "false", mitPfad: false));

        var bericht = await AlsBericht(antwort);
        Assert.That(bericht.Laufkopf!.Pfad, Is.EqualTo("probe.md"));
    }

    // **Übersprungen und verwaist bleiben im Ergebnis stehen** — die übersprungene mit Kennung
    // und Grund, die verwaiste zusätzlich mit Nummer und KarteId, weil archiviert werden soll.
    [Test]
    public async Task Wenn_eine_Zeile_uebersprungen_und_eine_Karte_verwaist_ist_dann_stehen_beide_im_Bericht()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        using var ersterLauf = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "false"));
        ersterLauf.EnsureSuccessStatusCode();

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "false", dateitext: ProbedateiOhneZweiteInteractionMitVerworfener()));

        var bericht = await AlsBericht(antwort);
        var uebersprungene = bericht.Zeilen.Single(zeile => zeile.Kennung == "I0003");
        var verwaiste = bericht.Zeilen.Single(zeile => zeile.Wirkung == Importwirkung.Verwaist);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Uebersprungen, Is.EqualTo(1));
            Assert.That(uebersprungene.Wirkung, Is.EqualTo(Importwirkung.Uebersprungen));
            Assert.That(uebersprungene.Grund, Does.Contain("verworfen"));
            Assert.That(uebersprungene.Kartennummer, Is.Null);
            Assert.That(uebersprungene.KarteId, Is.Null);
            Assert.That(bericht.Verwaist, Is.EqualTo(1));
            Assert.That(verwaiste.Kennung, Is.EqualTo("I0002"));
            Assert.That(verwaiste.Kartennummer, Is.EqualTo("WBS-02"));
            Assert.That(verwaiste.KarteId, Is.Not.Null);
        });
    }

    [Test]
    public async Task Wenn_geschrieben_wurde_dann_traegt_die_Karte_Etikett_Teilaufgaben_mit_Haken_und_ihren_Dateiverweis()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "false"));

        antwort.EnsureSuccessStatusCode();
        var board = await LiesBoard(webApi, aufbau.Board.BoardId);
        var karteId = board.Spalten.Single(spalte => spalte.IstAbschlussspalte).Karten.Single().KarteId;
        var detail = await webApi.Klient.GetFromJsonAsync<KanbanC.Contracts.Karten.Kartendetail>($"/api/karten/{karteId}");
        Assert.That(detail, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(detail!.Etiketten, Is.EqualTo(new[] { "Boards führen" }));
            Assert.That(detail.Teilaufgaben.Select(schritt => schritt.Text), Is.EqualTo(new[] { "F0001 Board anlegen und abrufen", "B0001 Standardspalten erzeugen" }));
            Assert.That(detail.Teilaufgaben[0].Abgehakt, Is.True);
            Assert.That(detail.Teilaufgaben[1].Abgehakt, Is.False);
            Assert.That(detail.Dateiverweise.Single().Pfad, Is.EqualTo("Dokumentation/Planung/probe.md#I0001"));
            Assert.That(detail.Dateiverweise.Single().Urheber.KontributorId, Is.EqualTo(aufbau.Kontributor.KontributorId));
            Assert.That(detail.Karte.Beschreibung, Does.Contain("Anforderung: R00001"));
            Assert.That(detail.Karte.Kontributor, Is.Null, "Verantwortlicher, Fälligkeit und Farbe bleiben leer.");
            Assert.That(detail.Karte.FaelligAm, Is.Null);
        });
    }

    // Fehlt das Feld pfad, gilt der Dateiname — dann ist der Verweis kürzer, aber nicht falsch.
    [Test]
    public async Task Wenn_der_Pfad_fehlt_dann_traegt_der_Dateiverweis_den_Dateinamen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), Rumpf(aufbau, trocken: "false", mitPfad: false));

        antwort.EnsureSuccessStatusCode();
        var board = await LiesBoard(webApi, aufbau.Board.BoardId);
        var karteId = board.Spalten.Single(spalte => spalte.IstAbschlussspalte).Karten.Single().KarteId;
        var detail = await webApi.Klient.GetFromJsonAsync<KanbanC.Contracts.Karten.Kartendetail>($"/api/karten/{karteId}");
        Assert.That(detail!.Dateiverweise.Single().Pfad, Is.EqualTo("probe.md#I0001"));
    }

    [Test]
    public async Task Wenn_das_Board_unbekannt_ist_dann_kommt_404_mit_der_Boardnummer_und_dem_Weg_zur_Boardliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var antwort = await webApi.Klient.PostAsync(Importroute(999), Rumpf(aufbau, trocken: "false"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Import auf ein unbekanntes Board");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("board-unbekannt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("999"));
        });
    }

    [Test]
    public async Task Wenn_die_Datei_keine_WBS_ist_dann_kommt_400_mit_Dateiname_und_dem_Weg_ueber_planung_anlegen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        var rumpf = Rumpf(aufbau, trocken: "false", dateitext: "# Protokoll", dateiname: "Protokoll.md");

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), rumpf);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "keine WBS-Datei");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("wbs-datei-unlesbar"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("Protokoll.md"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("/planung anlegen"));
        });
        Assert.That(await Kartenzahl(webApi, aufbau.Board.BoardId), Is.Zero, "Eine zurückgewiesene Datei hat ein Board berührt.");
    }

    [Test]
    public async Task Wenn_das_Board_keine_Kartenklasse_fuehrt_dann_kommt_eine_Zurueckweisung_mit_dem_Weg_ueber_den_Layout_Modus()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        var ohneKlasse = await LegeBoardAn(webApi, "Ideen");

        using var antwort = await webApi.Klient.PostAsync(Importroute(ohneKlasse.BoardId), Rumpf(aufbau, trocken: "false"));

        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Board ohne Kartenklasse");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("board-ohne-kartenklasse"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("Ideen"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("Layout-Modus"));
        });
        var kartenklassen = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Kartenklasse>>($"{BoardsRoute}/{ohneKlasse.BoardId}/kartenklassen");
        Assert.That(kartenklassen, Is.Empty, "Der Import hat eine Kartenklasse angelegt.");
    }

    [Test]
    public async Task Wenn_die_Kartenklasse_einem_anderen_Board_gehoert_dann_wird_zurueckgewiesen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        var zweites = await LegeBoardAn(webApi, "Zweites");
        var fremde = await LegeKartenklasseAn(webApi, zweites.BoardId, "BUG", "BUG-");
        var rumpf = Rumpf(aufbau, trocken: "false", kartenklasseId: fremde.KartenklasseId);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), rumpf);

        await Fehlerrumpf.ErwarteBefundMitCode(antwort, "kartenklasse-fremd");
        Assert.That(await Kartenzahl(webApi, aufbau.Board.BoardId), Is.Zero);
    }

    [Test]
    public async Task Wenn_der_Urheber_fehlt_dann_kommt_eine_Zurueckweisung_mit_dem_Weg_zur_Kontributorenliste()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        var rumpf = Rumpf(aufbau, trocken: "false", mitKontributor: false);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), rumpf);

        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "Import ohne Urheber");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("import-urheber-fehlt"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain(KontributorenRoute));
        });
    }

    // UNIQUE(Kartenklasse, Zaehlerstand) hält auch über viele Vergaben in einem Zug.
    [Test]
    public async Task Wenn_viele_Karten_in_einem_Zug_entstehen_dann_bleibt_jede_Nummer_einmalig()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        var rumpf = Rumpf(aufbau, trocken: "false", dateitext: GrosseProbedatei(41));

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), rumpf);

        antwort.EnsureSuccessStatusCode();
        var board = await LiesBoard(webApi, aufbau.Board.BoardId);
        var nummern = board.Spalten.SelectMany(spalte => spalte.Karten).Select(karte => karte.Kartennummer).ToList();
        var kartenklassen = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Kartenklasse>>($"{BoardsRoute}/{aufbau.Board.BoardId}/kartenklassen");
        Assert.Multiple(() =>
        {
            Assert.That(nummern, Has.Count.EqualTo(41));
            Assert.That(nummern, Is.Unique);
            Assert.That(nummern, Does.Contain("WBS-01"));
            Assert.That(nummern, Does.Contain("WBS-41"));
            Assert.That(kartenklassen!.Single().Zaehlerstand, Is.EqualTo(41));
        });
    }

    [Test]
    public void Wenn_die_Routen_gelesen_werden_dann_steht_die_Importroute_nur_mit_POST_darin()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);

        var routen = webApi.Routen;

        Assert.Multiple(() =>
        {
            Assert.That(routen, Does.Contain("POST /api/boards/{boardId:long}/wbs-import"));
            Assert.That(routen, Does.Not.Contain("GET /api/boards/{boardId:long}/wbs-import"));
        });
    }

    private static string Importroute(long boardId)
    {
        return $"{BoardsRoute}/{boardId}/wbs-import";
    }

    private static async Task<Importbericht> AlsBericht(HttpResponseMessage antwort)
    {
        antwort.EnsureSuccessStatusCode();
        var bericht = await antwort.Content.ReadFromJsonAsync<Importbericht>();
        Assert.That(bericht, Is.Not.Null, "Die API hat keinen Importbericht zurückgegeben.");
        return bericht!;
    }

    internal static MultipartFormDataContent Rumpf(
        Testaufbau aufbau,
        string? trocken,
        bool mitPfad = true,
        bool mitKontributor = true,
        long? kartenklasseId = null,
        string? dateitext = null,
        string dateiname = "probe.md")
    {
        var rumpf = new MultipartFormDataContent();
        var datei = new ByteArrayContent(Encoding.UTF8.GetBytes(dateitext ?? Probedatei()));
        datei.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        rumpf.Add(datei, "datei", dateiname);
        rumpf.Add(new StringContent((kartenklasseId ?? aufbau.Kartenklasse.KartenklasseId).ToString(CultureInfo.InvariantCulture)), "klasse");
        if (mitPfad)
        {
            rumpf.Add(new StringContent("Dokumentation/Planung/probe.md"), "pfad");
        }

        if (mitKontributor)
        {
            rumpf.Add(new StringContent(aufbau.Kontributor.KontributorId.ToString(CultureInfo.InvariantCulture)), "kontributor");
        }

        if (trocken is not null)
        {
            rumpf.Add(new StringContent(trocken), "trocken");
        }

        return rumpf;
    }

    internal static string Probedatei()
    {
        return string.Join(
            '\n',
            "---",
            "application: Probe",
            "sprache: de",
            "zuletzt: 2026-09-07",
            "---",
            string.Empty,
            "## Knoten",
            string.Empty,
            "| ID | Ebene | Eltern | Name | Status | Fertig-Kriterium | Eingabe → Ausgabe | Aufwand | Ausbaustufe | Braucht | Requirement | Notiz |",
            "|---|---|---|---|---|---|---|---|---|---|---|---|",
            "| A0001 | Application | — | Probe | gelb | alle Dialogs gruen | | | | | | |",
            "| D0001 | Dialog | A0001 | Boards führen | gelb | alle Interactions gruen | | | | | | |",
            "| I0001 | Interaction | D0001 | Board anlegen | gruen | Ein neues Board entsteht | | | | | R00001 | Aus Vision |",
            "| F0001 | Feature | I0001 | Board anlegen und abrufen | gruen | AK Board anlegen | | | | | R00001 | |",
            "| B0001 | Bubble | F0001 | Standardspalten erzeugen | rot | Test gruen | — → Vorlage → 3 Spalten | 2 | | | | Operation |",
            "| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |");
    }

    // Dieselbe Datei ohne I0002 — die Karte dazu wird damit verwaist — und mit einem Knoten im
    // Status verworfen, der übersprungen wird.
    private static string ProbedateiOhneZweiteInteractionMitVerworfener()
    {
        return string.Join(
            '\n',
            "---",
            "application: Probe",
            "sprache: de",
            "zuletzt: 2026-09-07",
            "---",
            string.Empty,
            "## Knoten",
            string.Empty,
            "| ID | Ebene | Eltern | Name | Status | Fertig-Kriterium | Eingabe → Ausgabe | Aufwand | Ausbaustufe | Braucht | Requirement | Notiz |",
            "|---|---|---|---|---|---|---|---|---|---|---|---|",
            "| A0001 | Application | — | Probe | gelb | alle Dialogs gruen | | | | | | |",
            "| D0001 | Dialog | A0001 | Boards führen | gelb | alle Interactions gruen | | | | | | |",
            "| I0001 | Interaction | D0001 | Board anlegen | gruen | Ein neues Board entsteht | | | | | R00001 | Aus Vision |",
            "| F0001 | Feature | I0001 | Board anlegen und abrufen | gruen | AK Board anlegen | | | | | R00001 | |",
            "| B0001 | Bubble | F0001 | Standardspalten erzeugen | rot | Test gruen | — → Vorlage → 3 Spalten | 2 | | | | Operation |",
            "| I0003 | Interaction | D0001 | Boards verwerfen | verworfen | Zaehlt nicht zum Umfang | | | | | | |");
    }

    private static string GrosseProbedatei(int interactions)
    {
        var zeilen = new List<string>
        {
            "---",
            "application: Probe",
            "---",
            "| ID | Ebene | Eltern | Name | Status | Fertig-Kriterium | Eingabe → Ausgabe | Aufwand | Ausbaustufe | Braucht | Requirement | Notiz |",
            "|---|---|---|---|---|---|---|---|---|---|---|---|",
            "| A0001 | Application | — | Probe | gelb | | | | | | | |",
            "| D0001 | Dialog | A0001 | Bahn | gelb | | | | | | | |",
        };
        for (var nummer = 1; nummer <= interactions; nummer++)
        {
            var status = nummer % 2 == 0 ? "gruen" : "rot";
            zeilen.Add($"| I{nummer:D4} | Interaction | D0001 | Knoten {nummer} | {status} | | | | | | | |");
        }

        return string.Join('\n', zeilen);
    }

    private static async Task<long> Kartenzahl(TestWebApi webApi, long boardId)
    {
        var board = await LiesBoard(webApi, boardId);
        return board.Spalten.Sum(spalte => spalte.Kartenzahl);
    }

    private static async Task<Board> LiesBoard(TestWebApi webApi, long boardId)
    {
        var board = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{boardId}");
        Assert.That(board, Is.Not.Null);
        return board!;
    }

    internal static async Task<Testaufbau> Aufbau(TestWebApi webApi)
    {
        var board = await LegeBoardAn(webApi, "Entwicklung");
        var kartenklasse = await LegeKartenklasseAn(webApi, board.BoardId, "WBS", "WBS-");
        var kontributor = await LegeKontributorAn(webApi, "Stefan");
        return new Testaufbau(board, kartenklasse, kontributor);
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, string name)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage(name, BoardArt.Projekt, null, null));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        return board!;
    }

    private static async Task<Kartenklasse> LegeKartenklasseAn(TestWebApi webApi, long boardId, string name, string praefix)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/kartenklassen", new KartenklasseAnlegenAnfrage(name, praefix));
        antwort.EnsureSuccessStatusCode();
        var kartenklasse = await antwort.Content.ReadFromJsonAsync<Kartenklasse>();
        return kartenklasse!;
    }

    private static async Task<Kontributor> LegeKontributorAn(TestWebApi webApi, string name)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage(name, Kontributorart.Mensch));
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        return kontributor!;
    }

    internal sealed record Testaufbau(Board Board, Kartenklasse Kartenklasse, Kontributor Kontributor);
}
