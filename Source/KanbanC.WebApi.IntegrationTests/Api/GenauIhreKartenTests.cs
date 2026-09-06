using System.Net;
using System.Net.Http.Json;
using Dapper;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// „Genau ihre Karten, ohne die übrigen“ ist eine Zusage über den Bestand und nicht über eine
// Funktion: sie fällt erst auf, wenn Karten einer anderen Klasse, ohne Klasse und eines anderen
// Boards danebenliegen. Ein Abruf, der Karten wegließe, sähe für einen Agenten wie ein Erfolg aus.
public class GenauIhreKartenTests
{
    private const string BoardsRoute = "/api/boards";

    [Test]
    public async Task Wenn_daneben_eine_zweite_Klasse_klassenlose_Karten_und_ein_zweites_Board_mit_demselben_Praefix_liegen_dann_kommen_genau_drei()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var karten = await LadeKartenDerKartenklasse(webApi, aufbau.Board.BoardId, aufbau.Wbs.KartenklasseId, string.Empty);

        Assert.That(karten, Has.Count.EqualTo(3));
        Assert.That(karten.Select(klassenkarte => klassenkarte.Karte.Kartennummer), Is.EqualTo(new[] { "WBS-01", "WBS-02", "WBS-03" }));
    }

    [Test]
    public async Task Wenn_die_drei_Karten_in_drei_Spalten_liegen_dann_nennt_jede_ihre_Spalte_mit_Nummer_und_Bezeichnung()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var karten = await LadeKartenDerKartenklasse(webApi, aufbau.Board.BoardId, aufbau.Wbs.KartenklasseId, string.Empty);

        var spalten = aufbau.Board.Spalten;
        Assert.Multiple(() =>
        {
            Assert.That(karten.Select(klassenkarte => klassenkarte.Spalte), Is.EqualTo(new[] { spalten[0].SpalteId, spalten[1].SpalteId, spalten[2].SpalteId }));
            Assert.That(karten.Select(klassenkarte => klassenkarte.Spaltenbezeichnung), Is.EqualTo(new[] { spalten[0].Bezeichnung, spalten[1].Bezeichnung, spalten[2].Bezeichnung }));
        });
    }

    // Die Abschlussspalte des Aufbaus traegt die Anzeigegrenze 1 und zwei WBS-Karten: dieser
    // Abruf kuerzt nicht, die Boardantwort weiter schon.
    [Test]
    public async Task Wenn_zwei_Karten_der_Klasse_in_der_Abschlussspalte_mit_Anzeigegrenze_1_liegen_dann_kommen_beide()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        await OrdneNeueKarteZu(webApi, aufbau.Board.BoardId, aufbau.Board.Spalten[2].SpalteId, "WBS vier", aufbau.Wbs.KartenklasseId);

        var karten = await LadeKartenDerKartenklasse(webApi, aufbau.Board.BoardId, aufbau.Wbs.KartenklasseId, string.Empty);

        Assert.That(karten.Select(klassenkarte => klassenkarte.Karte.Kartennummer), Is.EqualTo(new[] { "WBS-01", "WBS-02", "WBS-03", "WBS-04" }));
    }

    [Test]
    public async Task Wenn_dasselbe_Board_gelesen_wird_dann_kuerzt_es_seine_Abschlussspalte_weiterhin()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        await OrdneNeueKarteZu(webApi, aufbau.Board.BoardId, aufbau.Board.Spalten[2].SpalteId, "WBS vier", aufbau.Wbs.KartenklasseId);

        var geladen = await webApi.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{aufbau.Board.BoardId}");

        Assert.That(geladen, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(geladen!.Spalten[2].Karten, Has.Count.EqualTo(1));
            Assert.That(geladen.Spalten[2].Kartenzahl, Is.EqualTo(2));
        });
    }

    // Die Nummer fuellt nur auf zwei Stellen auf: als Text staende WBS-100 vor WBS-99.
    [Test]
    public async Task Wenn_die_Staende_99_und_100_vergeben_sind_dann_steht_WBS_99_vor_WBS_100()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardAn(webApi, "Entwicklung");
        var wbs = await LegeKartenklasseAn(webApi, board.BoardId, "WBS", "WBS-");
        SetzeZaehlerstand(datenbank, wbs.KartenklasseId, 98);
        await OrdneNeueKarteZu(webApi, board.BoardId, board.Spalten[2].SpalteId, "Neunundneunzig", wbs.KartenklasseId);
        await OrdneNeueKarteZu(webApi, board.BoardId, board.Spalten[0].SpalteId, "Hundert", wbs.KartenklasseId);

        var karten = await LadeKartenDerKartenklasse(webApi, board.BoardId, wbs.KartenklasseId, string.Empty);

        Assert.That(karten.Select(klassenkarte => klassenkarte.Karte.Kartennummer), Is.EqualTo(new[] { "WBS-99", "WBS-100" }));
    }

    [Test]
    public async Task Wenn_alle_Karten_der_Klasse_archiviert_sind_dann_ist_der_aktive_Bestand_leer_und_das_Archiv_vollstaendig()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);
        foreach (var klassenkarte in await LadeKartenDerKartenklasse(webApi, aufbau.Board.BoardId, aufbau.Wbs.KartenklasseId, string.Empty))
        {
            await Archiviere(webApi, aufbau.Board.BoardId, klassenkarte.Karte.KarteId);
        }

        var aktive = await LadeKartenDerKartenklasse(webApi, aufbau.Board.BoardId, aufbau.Wbs.KartenklasseId, string.Empty);
        var archiv = await LadeKartenDerKartenklasse(webApi, aufbau.Board.BoardId, aufbau.Wbs.KartenklasseId, "?archiviert=true");

        Assert.Multiple(() =>
        {
            Assert.That(aktive, Is.Empty);
            Assert.That(archiv.Select(klassenkarte => klassenkarte.Karte.Kartennummer), Is.EqualTo(new[] { "WBS-01", "WBS-02", "WBS-03" }));
        });
    }

    // Der Nachbar mit demselben Praefix bleibt bei seinen eigenen Karten: adressiert wird die
    // KartenklasseId, nicht das Praefix.
    [Test]
    public async Task Wenn_das_zweite_Board_dasselbe_Praefix_fuehrt_dann_liefert_es_seine_eigenen_Karten()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var karten = await LadeKartenDerKartenklasse(webApi, aufbau.Nachbar.BoardId, aufbau.NachbarWbs.KartenklasseId, string.Empty);

        Assert.That(karten.Select(klassenkarte => klassenkarte.Karte.Titel), Is.EqualTo(new[] { "Nachbar eins", "Nachbar zwei" }));
    }

    // Board 1 fuehrt WBS- (Stand 3) und BUG- (Stand 2): drei WBS-Karten in drei Spalten, zwei
    // BUG-Karten, zwei Karten ohne Klasse. Die dritte Spalte ist Abschlussspalte mit
    // Anzeigegrenze 1. Board 2 fuehrt ebenfalls WBS- und hat zwei Karten darin.
    private static async Task<Aufbau> LegeAufbauAn(TestWebApi webApi)
    {
        var board = await LegeBoardAn(webApi, "Entwicklung");
        await SetzeAnzeigegrenze(webApi, board, board.Spalten[2], 1);
        var wbs = await LegeKartenklasseAn(webApi, board.BoardId, "WBS", "WBS-");
        var bug = await LegeKartenklasseAn(webApi, board.BoardId, "Bugmeldungen", "BUG-");
        await OrdneNeueKarteZu(webApi, board.BoardId, board.Spalten[0].SpalteId, "WBS eins", wbs.KartenklasseId);
        await OrdneNeueKarteZu(webApi, board.BoardId, board.Spalten[1].SpalteId, "WBS zwei", wbs.KartenklasseId);
        await OrdneNeueKarteZu(webApi, board.BoardId, board.Spalten[2].SpalteId, "WBS drei", wbs.KartenklasseId);
        await OrdneNeueKarteZu(webApi, board.BoardId, board.Spalten[0].SpalteId, "BUG eins", bug.KartenklasseId);
        await OrdneNeueKarteZu(webApi, board.BoardId, board.Spalten[1].SpalteId, "BUG zwei", bug.KartenklasseId);
        await LegeKarteAn(webApi, board.BoardId, board.Spalten[0].SpalteId, "Ohne Klasse eins");
        await LegeKarteAn(webApi, board.BoardId, board.Spalten[1].SpalteId, "Ohne Klasse zwei");

        var nachbar = await LegeBoardAn(webApi, "Beschaffung");
        var nachbarWbs = await LegeKartenklasseAn(webApi, nachbar.BoardId, "WBS", "WBS-");
        await OrdneNeueKarteZu(webApi, nachbar.BoardId, nachbar.Spalten[0].SpalteId, "Nachbar eins", nachbarWbs.KartenklasseId);
        await OrdneNeueKarteZu(webApi, nachbar.BoardId, nachbar.Spalten[1].SpalteId, "Nachbar zwei", nachbarWbs.KartenklasseId);

        return new Aufbau(board, wbs, nachbar, nachbarWbs);
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, string name)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage(name, BoardArt.Linie, null, null));
        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        Assert.That(board, Is.Not.Null);
        return board!;
    }

    private static async Task SetzeAnzeigegrenze(TestWebApi webApi, Board board, Spalte spalte, int anzeigegrenze)
    {
        var antwort = await webApi.Klient.PutAsJsonAsync(
            $"{BoardsRoute}/{board.BoardId}/spalten/{spalte.SpalteId}",
            new SpalteAendernAnfrage(spalte.Bezeichnung, true, anzeigegrenze));
        antwort.EnsureSuccessStatusCode();
    }

    private static async Task<Kartenklasse> LegeKartenklasseAn(TestWebApi webApi, long boardId, string name, string praefix)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/kartenklassen", new KartenklasseAnlegenAnfrage(name, praefix));
        antwort.EnsureSuccessStatusCode();
        var kartenklasse = await antwort.Content.ReadFromJsonAsync<Kartenklasse>();
        Assert.That(kartenklasse, Is.Not.Null);
        return kartenklasse!;
    }

    private static async Task<Karte> LegeKarteAn(TestWebApi webApi, long boardId, long spalteId, string titel)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        antwort.EnsureSuccessStatusCode();
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        Assert.That(karte, Is.Not.Null);
        return karte!;
    }

    private static async Task<Karte> OrdneNeueKarteZu(TestWebApi webApi, long boardId, long spalteId, string titel, long kartenklasseId)
    {
        var karte = await LegeKarteAn(webApi, boardId, spalteId, titel);
        var zugeordnet = await webApi.Klient.PutAsJsonAsync($"/api/karten/{karte.KarteId}/kartenklasse", new KartenklasseZuordnenAnfrage(kartenklasseId));
        zugeordnet.EnsureSuccessStatusCode();
        return karte;
    }

    private static async Task Archiviere(TestWebApi webApi, long boardId, long karteId)
    {
        var antwort = await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/karten/{karteId}/archivierung", new Archivierung(true));
        antwort.EnsureSuccessStatusCode();
    }

    // Der Stand läßt sich über die API nicht setzen: er wächst nur beim Zuordnen. Für die Probe
    // gegen die Textsortierung braucht es aber die Stände 99 und 100.
    private static void SetzeZaehlerstand(TemporaereDatenbank datenbank, long kartenklasseId, long stand)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            UPDATE Kartenklasse
               SET Zaehlerstand = @Zaehlerstand
             WHERE KartenklasseId = @KartenklasseId", new { Zaehlerstand = stand, KartenklasseId = kartenklasseId });
    }

    private static async Task<IReadOnlyList<Klassenkarte>> LadeKartenDerKartenklasse(TestWebApi webApi, long boardId, long kartenklasseId, string abfrage)
    {
        var karten = await webApi.Klient.GetFromJsonAsync<List<Klassenkarte>>($"{BoardsRoute}/{boardId}/kartenklassen/{kartenklasseId}/karten{abfrage}");
        Assert.That(karten, Is.Not.Null);
        return karten!;
    }

    private sealed record Aufbau(Board Board, Kartenklasse Wbs, Board Nachbar, Kartenklasse NachbarWbs);
}
