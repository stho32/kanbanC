using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

public class WebApiNeustartTests
{
    private const string BoardsRoute = "/api/boards";

    [Test]
    public async Task Wenn_die_WebApi_auf_derselben_Datei_neu_startet_dann_bleiben_beide_Boards_und_das_dritte_bekommt_BoardId_3()
    {
        using var datenbank = new TemporaereDatenbank();
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31)));
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var boards = await zweiteInstanz.Klient.GetFromJsonAsync<List<BoardUebersicht>>(BoardsRoute);
        Assert.That(boards, Is.EqualTo(new[]
        {
            new BoardUebersicht(1, "Entwicklung", BoardArt.Linie, null, null),
            new BoardUebersicht(2, "KanbanC 1.0", BoardArt.Projekt, new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31)),
        }));
        var drittes = await LegeBoardAn(zweiteInstanz, new BoardAnlegenAnfrage("Betrieb", BoardArt.Linie, null, null));
        Assert.That(drittes.BoardId, Is.EqualTo(3));
    }


    [Test]
    public async Task Wenn_die_WebApi_nach_einem_Zug_neu_startet_dann_liegt_die_Karte_unveraendert_an_ihrer_neuen_Stelle()
    {
        using var datenbank = new TemporaereDatenbank();
        long zielspalteId;
        using (var ersteInstanz = new TestWebApi(datenbank.Dateipfad))
        {
            var board = await LegeBoardAn(ersteInstanz, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
            var quelle = board.Spalten[0].SpalteId;
            zielspalteId = board.Spalten[1].SpalteId;
            await LegeKarteAn(ersteInstanz, board.BoardId, quelle, "Migration schreiben");
            var endpunkt = await LegeKarteAn(ersteInstanz, board.BoardId, quelle, "Endpunkt bauen");
            var zug = await ersteInstanz.Klient.PutAsJsonAsync(
                $"{BoardsRoute}/{board.BoardId}/karten/{endpunkt.KarteId}/lage", new Kartenlage(zielspalteId, 1));
            zug.EnsureSuccessStatusCode();
        }

        using var zweiteInstanz = new TestWebApi(datenbank.Dateipfad);

        var geladen = await zweiteInstanz.Klient.GetFromJsonAsync<Board>($"{BoardsRoute}/1");
        Assert.That(geladen, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(geladen!.Spalten[0].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Migration schreiben" }));
            Assert.That(geladen.Spalten[0].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1 }));
            Assert.That(geladen.Spalten[1].SpalteId, Is.EqualTo(zielspalteId));
            Assert.That(geladen.Spalten[1].Karten.Select(karte => karte.Titel), Is.EqualTo(new[] { "Endpunkt bauen" }));
            Assert.That(geladen.Spalten[1].Karten.Select(karte => karte.Position), Is.EqualTo(new[] { 1 }));
        });
    }

    private static async Task<Karte> LegeKarteAn(TestWebApi webApi, long boardId, long spalteId, string titel)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        antwort.EnsureSuccessStatusCode();
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        if (karte is null)
        {
            throw new InvalidOperationException("Die API hat keine Karte zurückgegeben.");
        }

        return karte;
    }

    private static async Task<Board> LegeBoardAn(TestWebApi webApi, BoardAnlegenAnfrage anfrage)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, anfrage);
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        if (board is null)
        {
            throw new InvalidOperationException("Die API hat kein Board zurückgegeben.");
        }

        return board;
    }
}
