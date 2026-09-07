using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Dapper;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Import;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// **Der zweite Lauf.** Dieselbe Route, dieselben Felder — und ab hier: dieselbe Datei ein zweites
// Mal legt nichts doppelt an, zieht nach, was sich in der Datei geändert hat, und lässt stehen,
// was am Board gearbeitet wurde.
public class WbsImportWiederholungTests
{
    private const string BoardsRoute = "/api/boards";

    // Das Fertig-Kriterium des Slice als Testfall.
    [Test]
    public async Task Wenn_derselbe_Aufruf_zweimal_laeuft_dann_steht_dieselbe_Kartenzahl_mit_denselben_Nummern_auf_dem_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);
        var nachDemErsten = await Kartennummern(webApi, aufbau.Board.BoardId);

        using var antwort = await Importiere(webApi, aufbau);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var bericht = await AlsBericht(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.Zero);
            Assert.That(bericht.Geaendert, Is.Zero);
            Assert.That(bericht.Unveraendert, Is.EqualTo(2));
            Assert.That(bericht.Verwaist, Is.Zero);
        });
        Assert.That(await Kartennummern(webApi, aufbau.Board.BoardId), Is.EqualTo(nachDemErsten));
    }

    [Test]
    public async Task Wenn_ein_dritter_Lauf_auf_unveraenderter_Datei_folgt_dann_schreibt_er_ebenfalls_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);
        await Importiere(webApi, aufbau);

        using var antwort = await Importiere(webApi, aufbau);

        var bericht = await AlsBericht(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.Zero);
            Assert.That(bericht.Geaendert, Is.Zero);
            Assert.That(bericht.Unveraendert, Is.EqualTo(2));
        });
    }

    // **Der Zaehlerstand wächst je *neuer* Karte** — die Nummern des ersten Laufs bleiben.
    [Test]
    public async Task Wenn_der_zweite_Lauf_keine_neuen_Knoten_bringt_dann_bleibt_der_Zaehlerstand_stehen()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);

        await Importiere(webApi, aufbau);

        var kartenklassen = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Kartenklasse>>($"{BoardsRoute}/{aufbau.Board.BoardId}/kartenklassen");
        Assert.That(kartenklassen!.Single().Zaehlerstand, Is.EqualTo(2));
    }

    // Vier neue Knoten heben den Stand von 2 auf 6; die alten Nummern rühren sich nicht.
    [Test]
    public async Task Wenn_die_Datei_neue_Knoten_bringt_dann_waechst_der_Zaehlerstand_nur_um_diese()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);

        using var antwort = await Importiere(webApi, aufbau, MitZusaetzlichenKnoten(4));

        var bericht = await AlsBericht(antwort);
        var kartenklassen = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Kartenklasse>>($"{BoardsRoute}/{aufbau.Board.BoardId}/kartenklassen");
        var nummern = await Kartennummern(webApi, aufbau.Board.BoardId);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.EqualTo(4));
            Assert.That(bericht.Unveraendert, Is.EqualTo(2));
            Assert.That(kartenklassen!.Single().Zaehlerstand, Is.EqualTo(6));
            Assert.That(nummern, Does.Contain("WBS-01"));
            Assert.That(nummern, Does.Contain("WBS-06"));
            Assert.That(nummern, Is.Unique);
        });
    }

    // Die Zusage „was die Oberfläche kann, kann die API“ gilt auch für die vier Fächer.
    [Test]
    public async Task Wenn_trocken_auf_ein_eingefahrenes_Board_laeuft_dann_kommt_200_mit_null_angelegt_und_es_wird_nichts_geschrieben()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);
        var vorher = await Kartenzahl(webApi, aufbau.Board.BoardId);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "true"));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var bericht = await AlsBericht(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.Zero);
            Assert.That(bericht.Unveraendert, Is.EqualTo(2));
        });
        Assert.That(await Kartenzahl(webApi, aufbau.Board.BoardId), Is.EqualTo(vorher), "Ein trockener Lauf hat geschrieben.");
    }

    // **Die Datei zieht nach, das Board behält** — der ganze Schnitt zwischen zwei Wahrheiten in
    // einem Testfall.
    [Test]
    public async Task Wenn_sich_die_Datei_geaendert_hat_dann_zieht_die_Karte_nach_und_behaelt_Bahn_Nummer_Zeiten_und_Kommentare()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);
        var karteId = await KarteIdMitTitel(webApi, aufbau.Board.BoardId, "[I0002] Boards auflisten");
        var andereBahn = await AndereBahn(webApi, aufbau.Board.BoardId, karteId);
        await ZiehKarte(webApi, aufbau, karteId, andereBahn);
        await SetzeEtiketten(webApi, karteId, ["Boards führen", "dringend"]);
        await LegeTeilaufgabeAn(webApi, karteId, "Mit Stefan sprechen");
        await SchreibeKommentar(webApi, aufbau, karteId, "Läuft.");

        using var antwort = await Importiere(webApi, aufbau, MitGeaendertemZweitenKnoten());

        var bericht = await AlsBericht(antwort);
        var detail = await Kartendetail(webApi, karteId);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Geaendert, Is.EqualTo(1));
            Assert.That(bericht.Angelegt, Is.Zero);
            Assert.That(detail.Karte.Titel, Is.EqualTo("[I0002] Boards auflisten und filtern"));
            Assert.That(detail.Karte.Beschreibung, Does.Contain("Die gefilterte Liste"));
            Assert.That(detail.Spalte, Is.EqualTo(andereBahn), "Die Karte wurde umgezogen.");
            Assert.That(detail.Karte.Kartennummer, Is.EqualTo("WBS-02"));
            Assert.That(detail.Kommentare, Has.Count.EqualTo(1));
            Assert.That(detail.Etiketten, Does.Contain("dringend"), "Ein fremdes Etikett wurde entfernt.");
            Assert.That(detail.Teilaufgaben.Select(schritt => schritt.Text), Does.Contain("Mit Stefan sprechen"));
        });
    }

    // **Nie stillschweigend:** der Haken wird zurückgenommen und der Grund steht an der Zeile.
    [Test]
    public async Task Wenn_am_Board_abgehakt_wurde_und_die_Datei_den_Knoten_nicht_gruen_fuehrt_dann_wird_der_Haken_zurueckgenommen_und_gemeldet()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);
        var karteId = await KarteIdMitTitel(webApi, aufbau.Board.BoardId, "[I0001] Board anlegen");
        var teilaufgabe = (await Kartendetail(webApi, karteId)).Teilaufgaben.Single(schritt => schritt.Text.StartsWith("B0001", StringComparison.Ordinal));
        await SetzeAbhakung(webApi, karteId, teilaufgabe.TeilaufgabeId, abgehakt: true);

        using var antwort = await Importiere(webApi, aufbau);

        var bericht = await AlsBericht(antwort);
        var detail = await Kartendetail(webApi, karteId);
        var zeile = bericht.Zeilen.Single(berichtszeile => berichtszeile.Kennung == "I0001");
        Assert.Multiple(() =>
        {
            Assert.That(detail.Teilaufgaben.Single(schritt => schritt.TeilaufgabeId == teilaufgabe.TeilaufgabeId).Abgehakt, Is.False);
            Assert.That(bericht.Geaendert, Is.EqualTo(1));
            Assert.That(zeile.Grund, Does.Contain("1 Abhakung zurückgenommen (`B0001`)"));
            Assert.That(zeile.Grund, Does.Contain("auf `rot`"));
            Assert.That(zeile.Grund, Does.Contain("setze den Knoten in der Datei auf `gruen`"));
        });
    }

    // **Gelöscht wird nie** — die Karte bleibt unberührt und wird gemeldet.
    [Test]
    public async Task Wenn_ein_Knoten_aus_der_Datei_verschwindet_dann_bleibt_seine_Karte_unberuehrt_und_wird_gemeldet()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);
        var karteId = await KarteIdMitTitel(webApi, aufbau.Board.BoardId, "[I0002] Boards auflisten");
        var vorher = await Kartendetail(webApi, karteId);

        using var antwort = await Importiere(webApi, aufbau, OhneZweitenKnoten());

        var bericht = await AlsBericht(antwort);
        var nachher = await Kartendetail(webApi, karteId);
        var stehtNochAufDemBoard = await Kartennummern(webApi, aufbau.Board.BoardId);
        var letzte = bericht.Zeilen[^1];
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Verwaist, Is.EqualTo(1));
            Assert.That(letzte.Kennung, Is.EqualTo("I0002"));
            Assert.That(letzte.Wirkung, Is.EqualTo(Importwirkung.Verwaist));
            Assert.That(letzte.Kartennummer, Is.EqualTo("WBS-02"));
            Assert.That(letzte.Grund, Does.Contain("archivieren"));
            Assert.That(nachher.Spalte, Is.EqualTo(vorher.Spalte));
            Assert.That(nachher.Karte.Titel, Is.EqualTo(vorher.Karte.Titel));
            Assert.That(nachher.Karte.Position, Is.EqualTo(vorher.Karte.Position));
            Assert.That(stehtNochAufDemBoard, Does.Contain("WBS-02"), "Die verwaiste Karte wurde archiviert oder gelöscht.");
        });
    }

    // **Der Erledigungszeitpunkt wird von einem zweiten Lauf nicht überschrieben** — die
    // Datumsgruppierung aus R00015 zeigt die Karte weiterhin unter dem Tag ihrer Anlage.
    [Test]
    public async Task Wenn_der_zweite_Lauf_schreibt_dann_bleibt_ErledigtAm_der_gruenen_Karte_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);
        var karteId = await KarteIdMitTitel(webApi, aufbau.Board.BoardId, "[I0001] Board anlegen");
        var vorher = (await Kartendetail(webApi, karteId)).Karte.ErledigtAm;
        SetzeErledigungAufGestern(datenbank, karteId);

        await Importiere(webApi, aufbau, MitGeaendertemErstenKnoten());

        var nachher = (await Kartendetail(webApi, karteId)).Karte.ErledigtAm;
        Assert.Multiple(() =>
        {
            Assert.That(vorher, Is.EqualTo(DateOnly.FromDateTime(DateTime.Today)));
            Assert.That(nachher, Is.EqualTo(DateOnly.FromDateTime(DateTime.Today).AddDays(-1)), "Der zweite Lauf hat ErledigtAm überschrieben.");
        });
    }

    // **Was sich ändert, steht an der Zeile** — „geändert" allein ist keine Auskunft.
    [Test]
    public async Task Wenn_eine_Karte_nachgezogen_wird_dann_sagt_ihre_Zeile_was_sich_aendert()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);

        using var antwort = await Importiere(webApi, aufbau, MitGeaendertemZweitenKnoten());

        var bericht = await AlsBericht(antwort);
        var zeile = bericht.Zeilen.Single(berichtszeile => berichtszeile.Kennung == "I0002");
        Assert.Multiple(() =>
        {
            Assert.That(zeile.Wirkung, Is.EqualTo(Importwirkung.Geaendert));
            Assert.That(zeile.Grund, Does.Contain("Titel neu"));
            Assert.That(zeile.Grund, Does.Contain("Beschreibung neu"));
        });
    }

    // Eine einzelne Karte unter einem fremden Pfad bringt den Lauf **nicht** zu Fall: sie kostet
    // eine Dublette, und die Mehrzahl der Karten hängt weiter am angefragten Pfad.
    [Test]
    public async Task Wenn_nur_eine_einzelne_Karte_unter_einem_anderen_Pfad_haengt_dann_laeuft_der_Import_durch()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau, MitZusaetzlichenKnoten(3));
        var karteId = await KarteIdMitTitel(webApi, aufbau.Board.BoardId, "[I0003] Neuer Knoten 3");
        await EntferneDateiverweis(webApi, karteId);
        await TrageDateiverweisEin(webApi, aufbau, karteId, "Planung/probe.md#I0003");

        using var antwort = await Importiere(webApi, aufbau, MitZusaetzlichenKnoten(3));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var bericht = await AlsBericht(antwort);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.EqualTo(1), "Genau eine Dublette, nicht mehr.");
            Assert.That(bericht.Unveraendert, Is.EqualTo(4));
        });
    }

    // Die drei flächigen Lagen: 400 mit Grund, Werten und Kompensationsaktion — **kein Board
    // berührt**.
    [Test]
    public async Task Wenn_der_Pfad_vom_ersten_Lauf_abweicht_dann_kommt_400_und_keine_Karte_entsteht()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);
        var vorher = await Kartenzahl(webApi, aufbau.Board.BoardId);
        var rumpf = WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "false", mitPfad: false);

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), rumpf);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "abweichender Pfad");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("import-pfad-abweichend"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("Dokumentation/Planung/probe.md"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("Dokumentation/Planung/probe.md"));
        });
        Assert.That(await Kartenzahl(webApi, aufbau.Board.BoardId), Is.EqualTo(vorher));
    }

    [Test]
    public async Task Wenn_die_Schnittebene_vom_ersten_Lauf_abweicht_dann_kommt_400_mit_beiden_Ebenen_und_dem_Weg_ueber_eine_zweite_Kartenklasse()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);
        var vorher = await Kartenzahl(webApi, aufbau.Board.BoardId);
        var rumpf = WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "false");
        rumpf.Add(new StringContent("Bubble"), "schnittebene");

        using var antwort = await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), rumpf);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "abweichende Schnittebene");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("import-schnittebene-abweichend"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("Interaction"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("Bubble"));
            Assert.That(zurueckweisung.Befunde[0].Kompensation, Does.Contain("Kartenklasse"));
        });
        Assert.That(await Kartenzahl(webApi, aufbau.Board.BoardId), Is.EqualTo(vorher));
    }

    [Test]
    public async Task Wenn_zwei_Karten_denselben_Verweis_tragen_dann_kommt_400_mit_beiden_Kartennummern()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);
        var karteId = await KarteIdMitTitel(webApi, aufbau.Board.BoardId, "[I0002] Boards auflisten");
        await TrageDateiverweisEin(webApi, aufbau, karteId, "Dokumentation/Planung/probe.md#I0001");
        var vorher = await Kartenzahl(webApi, aufbau.Board.BoardId);

        using var antwort = await Importiere(webApi, aufbau);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var zurueckweisung = await Fehlerrumpf.Lies(antwort, "doppelter Verweis");
        Assert.Multiple(() =>
        {
            Assert.That(zurueckweisung.Befunde[0].Code, Is.EqualTo("import-verweis-doppelt"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("WBS-01"));
            Assert.That(zurueckweisung.Befunde[0].Meldung, Does.Contain("WBS-02"));
        });
        Assert.That(await Kartenzahl(webApi, aufbau.Board.BoardId), Is.EqualTo(vorher));
    }

    // Der einzelne Ausfall bringt den Lauf **nicht** zu Fall: die Karte entsteht, der Verdacht
    // steht daneben.
    [Test]
    public async Task Wenn_an_einer_Karte_der_Verweis_entfernt_wurde_dann_entsteht_eine_neue_und_der_Dublettenverdacht_steht_an_ihrer_Zeile()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await WbsImportEndpunkteTests.Aufbau(webApi);
        await Importiere(webApi, aufbau);
        var karteId = await KarteIdMitTitel(webApi, aufbau.Board.BoardId, "[I0002] Boards auflisten");
        await EntferneDateiverweis(webApi, karteId);

        using var antwort = await Importiere(webApi, aufbau);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var bericht = await AlsBericht(antwort);
        var zeile = bericht.Zeilen.Single(berichtszeile => berichtszeile.Kennung == "I0002");
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.EqualTo(1));
            Assert.That(zeile.Grund, Does.Contain("ähnlich zu „WBS-02“"));
            Assert.That(zeile.Grund, Does.Contain("I0019"));
        });
        Assert.That(await Kartenzahl(webApi, aufbau.Board.BoardId), Is.EqualTo(3));
    }

    private static string Importroute(long boardId)
    {
        return $"{BoardsRoute}/{boardId}/wbs-import";
    }

    private static async Task<HttpResponseMessage> Importiere(TestWebApi webApi, WbsImportEndpunkteTests.Testaufbau aufbau, string? dateitext = null)
    {
        var rumpf = WbsImportEndpunkteTests.Rumpf(aufbau, trocken: "false", dateitext: dateitext);
        return await webApi.Klient.PostAsync(Importroute(aufbau.Board.BoardId), rumpf);
    }

    private static async Task<Importbericht> AlsBericht(HttpResponseMessage antwort)
    {
        var bericht = await antwort.Content.ReadFromJsonAsync<Importbericht>();
        Assert.That(bericht, Is.Not.Null, "Die API hat keinen Importbericht zurückgegeben.");
        return bericht!;
    }

    // Dieselbe Probedatei wie im ersten Lauf, mit vier zusätzlichen Interactions.
    private static string MitZusaetzlichenKnoten(int anzahl)
    {
        var zeilen = new List<string> { WbsImportEndpunkteTests.Probedatei() };
        for (var nummer = 3; nummer < 3 + anzahl; nummer++)
        {
            zeilen.Add($"| I{nummer:D4} | Interaction | D0001 | Neuer Knoten {nummer} | rot | | | | | | | |");
        }

        return string.Join('\n', zeilen);
    }

    private static string MitGeaendertemErstenKnoten()
    {
        return WbsImportEndpunkteTests.Probedatei()
            .Replace(
                "| I0001 | Interaction | D0001 | Board anlegen | gruen | Ein neues Board entsteht | | | | | R00001 | Aus Vision |",
                "| I0001 | Interaction | D0001 | Board anlegen und benennen | gruen | Ein neues Board entsteht | | | | | R00001 | Aus Vision |",
                StringComparison.Ordinal);
    }

    private static void SetzeErledigungAufGestern(TemporaereDatenbank datenbank, long karteId)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var gestern = DateOnly.FromDateTime(DateTime.Today).AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        verbindung.Execute(@"
            UPDATE Karteerledigung
               SET ErledigtAm = @ErledigtAm
             WHERE Karte = @Karte", new { Karte = karteId, ErledigtAm = gestern });
    }

    private static string MitGeaendertemZweitenKnoten()
    {
        return WbsImportEndpunkteTests.Probedatei()
            .Replace(
                "| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |",
                "| I0002 | Interaction | D0001 | Boards auflisten und filtern | rot | Die gefilterte Liste zeigt die passenden Boards | | | | | R00002 | |",
                StringComparison.Ordinal);
    }

    private static string OhneZweitenKnoten()
    {
        return WbsImportEndpunkteTests.Probedatei()
            .Replace("| I0002 | Interaction | D0001 | Boards auflisten | rot | Die Liste zeigt alle Boards | | | | | R00002 | |", string.Empty, StringComparison.Ordinal)
            .TrimEnd('\n');
    }

    private static async Task<IReadOnlyList<string?>> Kartennummern(TestWebApi webApi, long boardId)
    {
        var board = await LiesBoard(webApi, boardId);
        return board.Spalten.SelectMany(spalte => spalte.Karten).Select(karte => karte.Kartennummer).Order(StringComparer.Ordinal).ToList();
    }

    private static async Task<long> Kartenzahl(TestWebApi webApi, long boardId)
    {
        var board = await LiesBoard(webApi, boardId);
        return board.Spalten.Sum(spalte => spalte.Kartenzahl);
    }

    private static async Task<long> KarteIdMitTitel(TestWebApi webApi, long boardId, string titel)
    {
        var board = await LiesBoard(webApi, boardId);
        return board.Spalten.SelectMany(spalte => spalte.Karten).Single(karte => karte.Titel == titel).KarteId;
    }

    private static async Task<long> AndereBahn(TestWebApi webApi, long boardId, long karteId)
    {
        var board = await LiesBoard(webApi, boardId);
        var jetzige = board.Spalten.Single(spalte => spalte.Karten.Any(karte => karte.KarteId == karteId));
        return board.Spalten.First(spalte => spalte.SpalteId != jetzige.SpalteId).SpalteId;
    }

    private static async Task<Board> LiesBoard(TestWebApi webApi, long boardId)
    {
        var board = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{boardId}");
        Assert.That(board, Is.Not.Null);
        return board!;
    }

    private static async Task<Kartendetail> Kartendetail(TestWebApi webApi, long karteId)
    {
        var detail = await webApi.Klient.GetFromJsonAsync<Kartendetail>($"/api/karten/{karteId}");
        Assert.That(detail, Is.Not.Null);
        return detail!;
    }

    private static async Task ZiehKarte(TestWebApi webApi, WbsImportEndpunkteTests.Testaufbau aufbau, long karteId, long spalteId)
    {
        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{aufbau.Board.BoardId}/karten/{karteId}/lage", new Kartenlage(spalteId, 1, aufbau.Kontributor.KontributorId));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task SetzeEtiketten(TestWebApi webApi, long karteId, IReadOnlyList<string> etiketten)
    {
        var antwort = await webApi.Klient.PutAsJsonAsync($"/api/karten/{karteId}/etiketten", new Kartenetiketten(etiketten));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task LegeTeilaufgabeAn(TestWebApi webApi, long karteId, string text)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/teilaufgaben", new TeilaufgabeAnlegenAnfrage(text));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task SetzeAbhakung(TestWebApi webApi, long karteId, long teilaufgabeId, bool abgehakt)
    {
        var antwort = await webApi.Klient.PutAsJsonAsync($"/api/karten/{karteId}/teilaufgaben/{teilaufgabeId}", new Teilaufgabenstand(abgehakt));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task SchreibeKommentar(TestWebApi webApi, WbsImportEndpunkteTests.Testaufbau aufbau, long karteId, string text)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/kommentare", new KommentarSchreibenAnfrage(text, aufbau.Kontributor.KontributorId));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task TrageDateiverweisEin(TestWebApi webApi, WbsImportEndpunkteTests.Testaufbau aufbau, long karteId, string pfad)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"/api/karten/{karteId}/dateiverweise", new DateiverweisEintragenAnfrage(pfad, aufbau.Kontributor.KontributorId));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task EntferneDateiverweis(TestWebApi webApi, long karteId)
    {
        var detail = await Kartendetail(webApi, karteId);
        var antwort = await webApi.Klient.DeleteAsync($"/api/karten/{karteId}/dateiverweise/{detail.Dateiverweise.Single().DateiverweisId}");
        antwort.EnsureSuccessStatusCode();
    }
}
