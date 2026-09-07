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

// **Die Probe des Slice: die echte Planungsdatei zweimal einfahren.** Idempotenz ist die eine
// nicht-funktionale Zusage von R00034, und sie wird hier gemessen statt behauptet — an der
// eingefrorenen Kopie, die schon der Leser prüft, durch Dienst und Repository auf einer echten
// SQLite-Datei.
// Die Datei zerfällt in 540 Knoten mit Notizen bis 8.000 Zeichen; ob der Vergleich darauf wirklich
// Zeichen für Zeichen zusammenfällt — Zeilenenden, Leerraum an den Rändern, die Reihenfolge der
// Etiketten und 489 Teilaufgaben —, sagt nur dieser Lauf.
public class WbsImportWiederholungsprobeTests
{
    private const string BoardsRoute = "/api/boards";
    private const string Herkunftspfad = "Dokumentation/Planung/kanbanc.md";

    [Test]
    public async Task PROBE_Wenn_die_echte_Planungsdatei_zweimal_eingefahren_wird_dann_legt_der_zweite_Lauf_nichts_an_und_aendert_nichts()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var ersterLauf = await Importiere(webApi, aufbau);
        Assert.That(ersterLauf.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Der erste Lauf ist nicht durchgekommen.");
        var ersterBericht = await AlsBericht(ersterLauf);

        using var zweiterLauf = await Importiere(webApi, aufbau);

        Assert.That(zweiterLauf.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var zweiterBericht = await AlsBericht(zweiterLauf);
        Assert.Multiple(() =>
        {
            Assert.That(ersterBericht.Angelegt, Is.EqualTo(EingefroreneWbsdatei.KartenBeiInteractionschnitt), "Der erste Lauf hat nicht 41 Karten angelegt.");
            Assert.That(zweiterBericht.Angelegt, Is.Zero);
            Assert.That(zweiterBericht.Geaendert, Is.Zero);
            Assert.That(zweiterBericht.Unveraendert, Is.EqualTo(EingefroreneWbsdatei.KartenBeiInteractionschnitt));
            Assert.That(zweiterBericht.Verwaist, Is.Zero);
        });
    }

    // Der dritte Lauf schreibt ebenfalls nichts — und die Kartenzahl auf dem Board steht über alle
    // drei Läufe bei 41.
    [Test]
    public async Task PROBE_Wenn_die_echte_Planungsdatei_dreimal_eingefahren_wird_dann_stehen_immer_dieselben_41_Karten_auf_dem_Board()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using (var ersterLauf = await Importiere(webApi, aufbau))
        {
            ersterLauf.EnsureSuccessStatusCode();
        }

        var nachDemErsten = await Kartennummern(webApi, aufbau);
        using (var zweiterLauf = await Importiere(webApi, aufbau))
        {
            zweiterLauf.EnsureSuccessStatusCode();
        }

        using var dritterLauf = await Importiere(webApi, aufbau);

        var dritterBericht = await AlsBericht(dritterLauf);
        var nachDemDritten = await Kartennummern(webApi, aufbau);
        var kartenklassen = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Kartenklasse>>($"{BoardsRoute}/{aufbau.Board.BoardId}/kartenklassen");
        Assert.Multiple(() =>
        {
            Assert.That(nachDemErsten, Has.Count.EqualTo(EingefroreneWbsdatei.KartenBeiInteractionschnitt));
            Assert.That(nachDemDritten, Is.EqualTo(nachDemErsten), "Die Kartennummern haben sich über drei Läufe verändert.");
            Assert.That(dritterBericht.Angelegt, Is.Zero);
            Assert.That(dritterBericht.Geaendert, Is.Zero);
            Assert.That(kartenklassen!.Single().Zaehlerstand, Is.EqualTo(EingefroreneWbsdatei.KartenBeiInteractionschnitt), "Der Zaehlerstand ist weitergewachsen.");
        });
    }

    private static async Task<HttpResponseMessage> Importiere(TestWebApi webApi, Probeaufbau aufbau)
    {
        var rumpf = new MultipartFormDataContent();
        var datei = new ByteArrayContent(Encoding.UTF8.GetBytes(EingefroreneWbsdatei.Text()));
        datei.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        rumpf.Add(datei, "datei", EingefroreneWbsdatei.Dateiname);
        rumpf.Add(new StringContent(aufbau.KartenklasseId.ToString(CultureInfo.InvariantCulture)), "klasse");
        rumpf.Add(new StringContent(Herkunftspfad), "pfad");
        rumpf.Add(new StringContent(aufbau.KontributorId.ToString(CultureInfo.InvariantCulture)), "kontributor");
        rumpf.Add(new StringContent("false"), "trocken");
        return await webApi.Klient.PostAsync($"{BoardsRoute}/{aufbau.Board.BoardId}/wbs-import", rumpf);
    }

    private static async Task<Importbericht> AlsBericht(HttpResponseMessage antwort)
    {
        var bericht = await antwort.Content.ReadFromJsonAsync<Importbericht>();
        Assert.That(bericht, Is.Not.Null, "Die API hat keinen Importbericht zurückgegeben.");
        return bericht!;
    }

    // Gelesen wird über die Kartenklasse und nicht über das Board: die Anzeigegrenze der
    // Abschlussspalte kürzt die Boardantwort, und eine gekürzte Liste sagt nichts über
    // Idempotenz.
    private static async Task<IReadOnlyList<string?>> Kartennummern(TestWebApi webApi, Probeaufbau aufbau)
    {
        var karten = await webApi.Klient.GetFromJsonAsync<IReadOnlyList<Klassenkarte>>($"{BoardsRoute}/{aufbau.Board.BoardId}/kartenklassen/{aufbau.KartenklasseId}/karten");
        Assert.That(karten, Is.Not.Null);
        return karten!.Select(klassenkarte => klassenkarte.Karte.Kartennummer).Order(StringComparer.Ordinal).ToList();
    }

    private static async Task<Probeaufbau> Aufbau(TestWebApi webApi)
    {
        var boardantwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage("KanbanC — Umsetzung", BoardArt.Projekt, null, null));
        boardantwort.EnsureSuccessStatusCode();
        var board = (await boardantwort.Content.ReadFromJsonAsync<Board>())!;

        var klassenantwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{board.BoardId}/kartenklassen", new KartenklasseAnlegenAnfrage("WBS", "WBS-"));
        klassenantwort.EnsureSuccessStatusCode();
        var kartenklasse = (await klassenantwort.Content.ReadFromJsonAsync<Kartenklasse>())!;

        var urheberantwort = await webApi.Klient.PostAsJsonAsync("/api/kontributoren", new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        urheberantwort.EnsureSuccessStatusCode();
        var kontributor = (await urheberantwort.Content.ReadFromJsonAsync<Kontributor>())!;

        return new Probeaufbau(board, kartenklasse.KartenklasseId, kontributor.KontributorId);
    }

    private sealed record Probeaufbau(Board Board, long KartenklasseId, long KontributorId);
}
