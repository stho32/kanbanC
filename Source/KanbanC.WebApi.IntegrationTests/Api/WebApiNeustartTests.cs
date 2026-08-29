using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
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
