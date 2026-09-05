using System.Net;
using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Das prüfbare Gegenstück zur Zusage „keine Fehlerantwort mit leerem Rumpf“: der Test geht jede
// Fehlerantwort jedes Endpunkts durch, nicht nur die der zuletzt gebauten Route.
public class FehlervertragTests
{
    private const string BoardsRoute = "/api/boards";
    private const string KontributorenRoute = "/api/kontributoren";
    private static readonly string[] RoutenOhneFehlerantwort =
    [
        "GET /openapi/{documentName}.json",
        "GET /api/zustand",
        "GET /api/kontributoren",
    ];

    [Test]
    public async Task Jede_Fehlerantwort_jedes_Endpunkts_traegt_einen_Befund_mit_Code_Meldung_und_Kompensation()
    {
        using var datenbank = new TemporaereDatenbank();
        using var webApi = new TestWebApi(datenbank.Dateipfad);
        var aufbau = await LegeAufbauAn(webApi);

        var faelle = await AlleFehlerantworten(webApi, aufbau);

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
        var aufbau = await LegeAufbauAn(webApi);
        var faelle = await AlleFehlerantworten(webApi, aufbau);
        foreach (var fall in faelle)
        {
            fall.Antwort.Dispose();
        }

        var geprueft = faelle.Select(fall => fall.Route).Distinct();
        var ungeprueft = webApi.Routen.Except(RoutenOhneFehlerantwort).Except(geprueft);

        Assert.That(ungeprueft, Is.Empty, "Diese Routen liefern Fehlerantworten, die der Vertragstest nicht abruft.");
    }

    private static async Task<IReadOnlyList<Fehlerfall>> AlleFehlerantworten(TestWebApi webApi, Aufbau aufbau)
    {
        var board = aufbau.Board;
        var spalteId = board.Spalten[0].SpalteId;
        var faelle = new List<Fehlerfall>();

        faelle.Add(new Fehlerfall(
            "POST /api/boards",
            "Board anlegen ohne Name",
            await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage("", BoardArt.Linie, null, null))));

        faelle.Add(new Fehlerfall(
            "POST /api/kontributoren",
            "Kontributor anlegen ohne Name",
            await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage("", Kontributorart.Mensch))));

        faelle.Add(new Fehlerfall(
            "PUT /api/kontributoren/{kontributorId:long}",
            "Kontributor ändern ohne Name",
            await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/{aufbau.Kontributor.KontributorId}", new KontributorAendernAnfrage("", Kontributorart.Mensch))));

        faelle.Add(new Fehlerfall(
            "PUT /api/kontributoren/{kontributorId:long}",
            "Kontributor ändern mit unbekannter KontributorId",
            await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/999", new KontributorAendernAnfrage("Zora", Kontributorart.Mensch))));

        faelle.Add(new Fehlerfall(
            "PUT /api/kontributoren/{kontributorId:long}/stilllegung",
            "Stilllegung schalten mit unbekannter KontributorId",
            await webApi.Klient.PutAsJsonAsync($"{KontributorenRoute}/999/stilllegung", new Stilllegung(true))));

        faelle.Add(new Fehlerfall(
            "GET /api/boards",
            "Boards auflisten mit unlesbarem Archiv-Filter",
            await webApi.Klient.GetAsync($"{BoardsRoute}?archiviert=vielleicht")));

        faelle.Add(new Fehlerfall(
            "GET /api/boards/{boardId:long}",
            "Board lesen mit unbekannter BoardId",
            await webApi.Klient.GetAsync($"{BoardsRoute}/999")));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}",
            "Board umbenennen ohne Name",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}", new BoardUmbenennenAnfrage(""))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}",
            "Board umbenennen mit unbekannter BoardId",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/999", new BoardUmbenennenAnfrage("Betrieb"))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/kartenzahl",
            "Kartenzahl schalten an unbekanntem Board",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/999/kartenzahl", new Kartenzahlanzeige(true))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/archivierung",
            "Archivierung schalten an unbekanntem Board",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/999/archivierung", new Archivierung(true))));

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
            "GET /api/boards/{boardId:long}/spalten/{spalteId:long}/karten",
            "Karten einer Spalte lesen an unbekanntem Board",
            await webApi.Klient.GetAsync($"{BoardsRoute}/999/spalten/{spalteId}/karten")));

        faelle.Add(new Fehlerfall(
            "GET /api/boards/{boardId:long}/spalten/{spalteId:long}/karten",
            "Karten einer unbekannten Spalte lesen",
            await webApi.Klient.GetAsync($"{BoardsRoute}/{board.BoardId}/spalten/999/karten")));

        faelle.Add(new Fehlerfall(
            "GET /api/boards/{boardId:long}/spalten/{spalteId:long}/karten",
            "Karten einer Spalte lesen mit unlesbarem Archiv-Filter",
            await webApi.Klient.GetAsync($"{BoardsRoute}/{board.BoardId}/spalten/{spalteId}/karten?archiviert=vielleicht")));

        faelle.Add(new Fehlerfall(
            "POST /api/boards/{boardId:long}/spalten/{spalteId:long}/karten",
            "Karte anlegen ohne Titel",
            await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{board.BoardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(""))));

        faelle.Add(new Fehlerfall(
            "POST /api/boards/{boardId:long}/spalten/{spalteId:long}/karten",
            "Karte anlegen an unbekannter Spalte",
            await webApi.Klient.PostAsJsonAsync($"{BoardsRoute}/{board.BoardId}/spalten/999/karten", new KarteAnlegenAnfrage("Migration schreiben"))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/karten/{karteId:long}/lage",
            "Karte verschieben auf eine Position ausserhalb der Zielspalte",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}/karten/{aufbau.Karte.KarteId}/lage", new Kartenlage(spalteId, 99))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/karten/{karteId:long}/lage",
            "Karte verschieben an unbekanntem Board",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/999/karten/{aufbau.Karte.KarteId}/lage", new Kartenlage(spalteId, 1))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/karten/{karteId:long}/lage",
            "Karte verschieben mit unbekannter KarteId",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}/karten/999/lage", new Kartenlage(spalteId, 1))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/karten/{karteId:long}/lage",
            "Karte verschieben in eine unbekannte Zielspalte",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}/karten/{aufbau.Karte.KarteId}/lage", new Kartenlage(999, 1))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/karten/{karteId:long}/archivierung",
            "Karte archivieren mit unbekannter KarteId",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/{board.BoardId}/karten/999/archivierung", new Archivierung(true))));

        faelle.Add(new Fehlerfall(
            "PUT /api/boards/{boardId:long}/karten/{karteId:long}/archivierung",
            "Karte archivieren an unbekanntem Board",
            await webApi.Klient.PutAsJsonAsync($"{BoardsRoute}/999/karten/{aufbau.Karte.KarteId}/archivierung", new Archivierung(true))));

        faelle.Add(new Fehlerfall(
            "GET /api/karten/{karteId:long}",
            "Kartendetail lesen mit unbekannter KarteId",
            await webApi.Klient.GetAsync("/api/karten/999")));

        return faelle;
    }

    private static async Task<Aufbau> LegeAufbauAn(TestWebApi webApi)
    {
        var antwort = await webApi.Klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null));
        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        Assert.That(board, Is.Not.Null);

        var angelegt = await webApi.Klient.PostAsJsonAsync(
            $"{BoardsRoute}/{board!.BoardId}/spalten/{board.Spalten[0].SpalteId}/karten",
            new KarteAnlegenAnfrage("Migration schreiben"));
        Assert.That(angelegt.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var karte = await angelegt.Content.ReadFromJsonAsync<Karte>();
        Assert.That(karte, Is.Not.Null);

        var eingetragen = await webApi.Klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage("Bert", Kontributorart.Agent));
        Assert.That(eingetragen.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var kontributor = await eingetragen.Content.ReadFromJsonAsync<Kontributor>();
        Assert.That(kontributor, Is.Not.Null);
        return new Aufbau(board, karte!, kontributor!);
    }

    private sealed record Aufbau(Board Board, Karte Karte, Kontributor Kontributor);

    private sealed record Fehlerfall(string Route, string Lage, HttpResponseMessage Antwort);
}
