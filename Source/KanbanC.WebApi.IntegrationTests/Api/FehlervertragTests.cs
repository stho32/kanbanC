using System.Net;
using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Das prüfbare Gegenstück zur Zusage „keine Fehlerantwort mit leerem Rumpf“: der Test geht jede
// Fehlerantwort jedes Endpunkts durch, nicht nur die der zuletzt gebauten Route.
public class FehlervertragTests
{
    private const string BoardsRoute = "/api/boards";
    private static readonly string[] RoutenOhneFehlerantwort =
    [
        "GET /openapi/{documentName}.json",
        "GET /api/zustand",
        "GET /api/boards",
    ];

    [Test]
    public async Task Jede_Fehlerantwort_jedes_Endpunkts_traegt_einen_Befund_mit_Code_Meldung_und_Kompensation()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardMitKarteAn(webApi);

        var faelle = await AlleFehlerantworten(webApi, board);

        Assert.That(faelle, Is.Not.Empty);
        foreach (var fall in faelle)
        {
            Assert.That((int)fall.Antwort.StatusCode, Is.InRange(400, 499), fall.Lage);
            await Fehlerrumpf.Lies(fall.Antwort, fall.Lage);
            fall.Antwort.Dispose();
        }
    }

    [Test]
    public async Task Wenn_ein_Endpunkt_hinzukommt_dann_faellt_auf_dass_seine_Fehlerantworten_ungeprueft_sind()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var board = await LegeBoardMitKarteAn(webApi);
        var faelle = await AlleFehlerantworten(webApi, board);
        foreach (var fall in faelle)
        {
            fall.Antwort.Dispose();
        }

        var geprueft = faelle.Select(fall => fall.Route).Distinct();
        var ungeprueft = webApi.Routen.Except(RoutenOhneFehlerantwort).Except(geprueft);

        Assert.That(ungeprueft, Is.Empty, "Diese Routen liefern Fehlerantworten, die der Vertragstest nicht abruft.");
    }

    private static async Task<IReadOnlyList<Fehlerfall>> AlleFehlerantworten(TestWebApi webApi, Board board)
    {
        var spalteId = board.Spalten[0].SpalteId;
        var faelle = new List<Fehlerfall>();

        faelle.Add(new Fehlerfall(
            "POST /api/boards",
            "Board anlegen ohne Name",
            await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage("", BoardArt.Linie, null, null))));

        faelle.Add(new Fehlerfall(
            "GET /api/boards/{boardId:long}",
            "Board lesen mit unbekannter BoardId",
            await webApi.Klient.GetAsync($"{BoardsRoute}/999")));

        faelle.Add(new Fehlerfall(
            "POST /api/boards/{boardId:long}/spalten",
            "Spalte anlegen ohne Bezeichnung",
            await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{board.BoardId}/spalten", new SpalteAnlegenAnfrage("", false, null))));

        faelle.Add(new Fehlerfall(
            "POST /api/boards/{boardId:long}/spalten",
            "Spalte anlegen an unbekanntem Board",
            await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/999/spalten", new SpalteAnlegenAnfrage("Eingang", false, null))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/spalten/{spalteId:long}",
            "Spalte ändern ohne Bezeichnung",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}/spalten/{spalteId}", new SpalteAendernAnfrage("", false, null))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/spalten/{spalteId:long}",
            "Spalte ändern mit unbekannter SpalteId",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}/spalten/999", new SpalteAendernAnfrage("Erfunden", false, null))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/spalten/reihenfolge",
            "Reihenfolge ohne alle Spalten",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}/spalten/reihenfolge", new Spaltenreihenfolge([]))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/spalten/reihenfolge",
            "Reihenfolge an unbekanntem Board",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/999/spalten/reihenfolge", new Spaltenreihenfolge([1, 2, 3]))));

        faelle.Add(new Fehlerfall(
            "DELETE /api/boards/{boardId:long}/spalten/{spalteId:long}",
            "Spalte entfernen, die noch eine Karte trägt",
            await webApi.Klient.DeleteAsync($"{BoardsRoute}/{board.BoardId}/spalten/{spalteId}")));

        faelle.Add(new Fehlerfall(
            "DELETE /api/boards/{boardId:long}/spalten/{spalteId:long}",
            "Spalte entfernen mit unbekannter SpalteId",
            await webApi.Klient.DeleteAsync($"{BoardsRoute}/{board.BoardId}/spalten/999")));

        faelle.Add(new Fehlerfall(
            "POST /api/boards/{boardId:long}/spalten/{spalteId:long}/karten",
            "Karte anlegen ohne Titel",
            await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{board.BoardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(""))));

        faelle.Add(new Fehlerfall(
            "POST /api/boards/{boardId:long}/spalten/{spalteId:long}/karten",
            "Karte anlegen an unbekannter Spalte",
            await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{board.BoardId}/spalten/999/karten", new KarteAnlegenAnfrage("Migration schreiben"))));

        return faelle;
    }

    private static async Task<Board> LegeBoardMitKarteAn(TestWebApi webApi)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        Assert.That(board, Is.Not.Null);

        var karte = await webApi.Klient.PostAsJsonAsync(
            $"{BoardsRoute}/{board!.BoardId}/spalten/{board.Spalten[0].SpalteId}/karten",
            new KarteAnlegenAnfrage("Migration schreiben"));
        Assert.That(karte.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        return board;
    }

    private sealed record Fehlerfall(string Route, string Lage, HttpResponseMessage Antwort);
}
