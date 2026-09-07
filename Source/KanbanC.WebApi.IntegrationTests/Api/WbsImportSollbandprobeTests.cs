using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using KanbanC.BL.Models.Import;
using KanbanC.BL.Persistenz.Import;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Import;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// **Die Probe des Sollbands: die echte Planungsdatei mit ihren Aufwänden einfahren.** Die
// Summierung über den Teilbaum wird hier an einem Fall gemessen, den niemand für den Test
// zurechtgelegt hat — 540 Knotenzeilen, Aufwandszellen als Einzelwert und als Spanne, und Karten,
// unter denen gar keine Schätzung steht.
// Gesummt wird, **was in den Zellen steht**: Untergrenzen zu Untergrenze, Obergrenzen zu
// Obergrenze. Die Zahlen der Berichtsform von `/planung zaehlen` sind eine Darstellungsregel und
// keine Summe — sie lässt die Untergrenzen unklarer Bubbles bewusst aus, und der Import liest
// Zellen, keine Berichte.
public class WbsImportSollbandprobeTests
{
    private const string BoardsRoute = "/api/boards";
    private const string Herkunftspfad = "Dokumentation/Planung/kanbanc.md";

    // An der eingefrorenen Kopie gemessen, nicht vermutet: [I0022] hat vier Aufwandszellen
    // (0,4 · 0,4 · 2 · 0,4-1,5), [I0030] deren 31.
    private static readonly Sollband BandDerKarteI0022 = new(3.2m, 4.3m);
    private static readonly Sollband BandDerKarteI0030 = new(38.0m, 44.0m);

    [Test]
    public async Task PROBE_Wenn_die_echte_Planungsdatei_eingefahren_wird_dann_tragen_ihre_Karten_das_Aufwandsband_ihres_Teilbaums()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var lauf = await Importiere(webApi, aufbau, EingefroreneWbsdatei.Text());

        Assert.That(lauf.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Der Lauf ist nicht durchgekommen.");
        var iststand = Iststand(datenbank, aufbau);
        Assert.Multiple(() =>
        {
            Assert.That(Karte(iststand, "I0022").Sollband, Is.EqualTo(BandDerKarteI0022));
            Assert.That(Karte(iststand, "I0030").Sollband, Is.EqualTo(BandDerKarteI0030));
        });
    }

    // Eine Interaction, unter der keine Zeile einen Aufwand schätzt, steht **ohne** Soll da und
    // nicht bei null Stunden — in der eingefrorenen Kopie ist I0033 selbst so ein Fall.
    [Test]
    public async Task PROBE_Wenn_unter_einer_Karte_der_echten_Datei_niemand_geschaetzt_hat_dann_traegt_sie_kein_Sollband()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);

        using var lauf = await Importiere(webApi, aufbau, EingefroreneWbsdatei.Text());

        lauf.EnsureSuccessStatusCode();
        var iststand = Iststand(datenbank, aufbau);
        Assert.That(Karte(iststand, "I0033").Sollband, Is.Null);
    }

    // Schritt 3 des Artboards, an der echten Datei: ein zweiter Lauf über **geänderten** Aufwand
    // meldet die Karte `geaendert` und schreibt das neue Band.
    [Test]
    public async Task PROBE_Wenn_ein_Aufwand_der_Datei_geaendert_wird_dann_meldet_der_zweite_Lauf_die_Karte_als_geaendert_mit_neuem_Band()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        using (var ersterLauf = await Importiere(webApi, aufbau, EingefroreneWbsdatei.Text()))
        {
            ersterLauf.EnsureSuccessStatusCode();
        }

        using var zweiterLauf = await Importiere(webApi, aufbau, MitVerdoppeltemAufwand(EingefroreneWbsdatei.Text(), "B0315"));

        var bericht = await AlsBericht(zweiterLauf);
        var iststand = Iststand(datenbank, aufbau);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Geaendert, Is.EqualTo(1), "Der zweite Lauf hat die Karte mit dem geänderten Aufwand nicht als geändert gemeldet.");
            Assert.That(bericht.Angelegt, Is.Zero);
            Assert.That(Karte(iststand, "I0022").Sollband, Is.EqualTo(new Sollband(3.6m, 4.7m)));
            Assert.That(Karte(iststand, "I0030").Sollband, Is.EqualTo(BandDerKarteI0030), "Eine Karte ohne geänderten Aufwand hat ihr Band verloren.");
        });
    }

    // `B0452` gilt unverändert weiter: zwei Läufe über **denselben** Stand melden „0 angelegt,
    // 0 geändert" — auch jetzt, wo das Sollband im Abbild steht.
    [Test]
    public async Task PROBE_Wenn_dieselbe_Datei_zweimal_eingefahren_wird_dann_aendert_das_mitgefuehrte_Sollband_nichts_an_der_Bilanz()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await Aufbau(webApi);
        using (var ersterLauf = await Importiere(webApi, aufbau, EingefroreneWbsdatei.Text()))
        {
            ersterLauf.EnsureSuccessStatusCode();
        }

        using var zweiterLauf = await Importiere(webApi, aufbau, EingefroreneWbsdatei.Text());

        var bericht = await AlsBericht(zweiterLauf);
        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.Zero);
            Assert.That(bericht.Geaendert, Is.Zero);
            Assert.That(bericht.Unveraendert, Is.EqualTo(EingefroreneWbsdatei.KartenBeiInteractionschnitt));
        });
    }

    // Verdoppelt die Aufwandsspanne einer einzelnen Bubble, ohne sonst ein Zeichen der Datei
    // anzufassen: die Zelle `0,4` derselben Zeile wird `0,8`.
    private static string MitVerdoppeltemAufwand(string dateitext, string knotenId)
    {
        var zeilen = dateitext.Split('\n');
        for (var stelle = 0; stelle < zeilen.Length; stelle++)
        {
            var dasIstDieZeileDesKnotens = zeilen[stelle].StartsWith($"| {knotenId} |", StringComparison.Ordinal);
            if (dasIstDieZeileDesKnotens)
            {
                zeilen[stelle] = ErsetzeAufwandszelle(zeilen[stelle], "0,8");
                return string.Join('\n', zeilen);
            }
        }

        throw new InvalidOperationException($"Die eingefrorene Kopie führt den Knoten {knotenId} nicht.");
    }

    private static string ErsetzeAufwandszelle(string zeile, string aufwand)
    {
        const int StelleAufwand = 8;
        var teile = zeile.Split('|');
        Assert.That(teile[StelleAufwand].Trim(), Is.EqualTo("0,4"), "Die Probe rechnet mit der Aufwandszelle 0,4 in dieser Zeile.");
        teile[StelleAufwand] = $" {aufwand} ";
        return string.Join('|', teile);
    }

    private static Karteniststaende Iststand(TemporaereDatenbank datenbank, Probeaufbau aufbau)
    {
        return new WbsImportRepository(datenbank.Verbindungsfabrik).LiesIststand(aufbau.Board.BoardId, aufbau.KartenklasseId);
    }

    private static Karteniststand Karte(Karteniststaende iststand, string knotenId)
    {
        foreach (var stand in iststand)
        {
            if (stand.Dateiverweise.Contains($"{Herkunftspfad}#{knotenId}"))
            {
                return stand;
            }
        }

        throw new InvalidOperationException($"Zum Knoten {knotenId} steht keine Karte auf dem Board.");
    }

    private static async Task<HttpResponseMessage> Importiere(TestWebApi webApi, Probeaufbau aufbau, string dateitext)
    {
        var rumpf = new MultipartFormDataContent();
        var datei = new ByteArrayContent(Encoding.UTF8.GetBytes(dateitext));
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
