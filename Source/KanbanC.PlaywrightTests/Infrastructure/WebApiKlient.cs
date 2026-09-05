using System.Net.Http.Json;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.PlaywrightTests.Infrastructure;

// Der Weg des Agenten: dieselben Routen, die die Oberfläche ruft, nur ohne Browser.
public sealed class WebApiKlient : IDisposable
{
    private const string BoardsRoute = "api/boards";
    private const string KontributorenRoute = "api/kontributoren";
    private readonly HttpClient _klient;

    public WebApiKlient(string webApiAdresse)
    {
        _klient = new HttpClient { BaseAddress = new Uri(webApiAdresse + "/") };
    }

    public async Task<Board> LegeBoardAn(string name)
    {
        var antwort = await _klient.PostAsJsonAsync(BoardsRoute, new BoardAnlegenAnfrage(name, BoardArt.Linie, null, null));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        if (board is null)
        {
            throw new InvalidOperationException("Die WebApi hat kein Board zurückgegeben.");
        }

        return board;
    }

    public async Task<Board> LadeBoard(long boardId)
    {
        var board = await _klient.GetFromJsonAsync<Board>($"{BoardsRoute}/{boardId}");
        if (board is null)
        {
            throw new InvalidOperationException("Die WebApi hat kein Board zurückgegeben.");
        }

        return board;
    }

    public async Task<IReadOnlyList<BoardUebersicht>> LadeAlleBoards(bool archiviert)
    {
        var adresse = BoardsRoute;
        if (archiviert)
        {
            adresse = $"{BoardsRoute}?archiviert=true";
        }

        var boards = await _klient.GetFromJsonAsync<List<BoardUebersicht>>(adresse);
        if (boards is null)
        {
            throw new InvalidOperationException("Die WebApi hat keine Boardliste zurückgegeben.");
        }

        return boards;
    }

    public async Task<Board> SchalteArchivierung(long boardId, bool istArchiviert)
    {
        var antwort = await _klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/archivierung", new Archivierung(istArchiviert));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        if (board is null)
        {
            throw new InvalidOperationException("Die WebApi hat kein Board zurückgegeben.");
        }

        return board;
    }

    public async Task<Board> SchalteKartenzahl(long boardId, bool zeigtKartenzahl)
    {
        var antwort = await _klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/kartenzahl", new Kartenzahlanzeige(zeigtKartenzahl));
        antwort.EnsureSuccessStatusCode();
        var board = await antwort.Content.ReadFromJsonAsync<Board>();
        if (board is null)
        {
            throw new InvalidOperationException("Die WebApi hat kein Board zurückgegeben.");
        }

        return board;
    }

    public async Task<Karte> LegeKarteAn(long boardId, long spalteId, string titel)
    {
        var antwort = await _klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten", new KarteAnlegenAnfrage(titel));
        antwort.EnsureSuccessStatusCode();
        var karte = await antwort.Content.ReadFromJsonAsync<Karte>();
        if (karte is null)
        {
            throw new InvalidOperationException("Die WebApi hat keine Karte zurückgegeben.");
        }

        return karte;
    }

    // Der Weg, auf dem ein Agent die ganze Bahn liest, waehrend die Oberflaeche kuerzt.
    public async Task<IReadOnlyList<Karte>> LadeKartenDerSpalte(long boardId, long spalteId)
    {
        var karten = await _klient.GetFromJsonAsync<List<Karte>>($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten");
        if (karten is null)
        {
            throw new InvalidOperationException("Die WebApi hat keine Kartenliste zurückgegeben.");
        }

        return karten;
    }

    public async Task<IReadOnlyList<Spalte>> VerschiebeKarte(long boardId, long karteId, Kartenlage lage)
    {
        var antwort = await _klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/karten/{karteId}/lage", lage);
        antwort.EnsureSuccessStatusCode();
        var spalten = await antwort.Content.ReadFromJsonAsync<IReadOnlyList<Spalte>>();
        if (spalten is null)
        {
            throw new InvalidOperationException("Die WebApi hat keine Spalten zurückgegeben.");
        }

        return spalten;
    }

    public async Task<Kartendetail> SetzeEtiketten(long karteId, IReadOnlyList<string> etiketten)
    {
        var antwort = await _klient.PutAsJsonAsync($"api/karten/{karteId}/etiketten", new Kartenetiketten(etiketten));
        antwort.EnsureSuccessStatusCode();
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        if (detail is null)
        {
            throw new InvalidOperationException("Die WebApi hat kein Kartendetail zurückgegeben.");
        }

        return detail;
    }

    // Der Weg des Agenten in die Gliederung: eine Zeile je Aufruf, angehaengt.
    public async Task<Kartendetail> LegeTeilaufgabeAn(long karteId, string text)
    {
        var antwort = await _klient.PostAsJsonAsync($"api/karten/{karteId}/teilaufgaben", new TeilaufgabeAnlegenAnfrage(text));
        antwort.EnsureSuccessStatusCode();
        return await AlsKartendetail(antwort);
    }

    public async Task<Kartendetail> SetzeAbhakung(long karteId, long teilaufgabeId, bool abgehakt)
    {
        var antwort = await _klient.PutAsJsonAsync($"api/karten/{karteId}/teilaufgaben/{teilaufgabeId}", new Teilaufgabenstand(abgehakt));
        antwort.EnsureSuccessStatusCode();
        return await AlsKartendetail(antwort);
    }

    private static async Task<Kartendetail> AlsKartendetail(HttpResponseMessage antwort)
    {
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        if (detail is null)
        {
            throw new InvalidOperationException("Die WebApi hat kein Kartendetail zurückgegeben.");
        }

        return detail;
    }

    public async Task<Kontributor> LegeKontributorAn(string name, Kontributorart art)
    {
        var antwort = await _klient.PostAsJsonAsync(KontributorenRoute, new KontributorAnlegenAnfrage(name, art));
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        if (kontributor is null)
        {
            throw new InvalidOperationException("Die WebApi hat keinen Kontributor zurückgegeben.");
        }

        return kontributor;
    }

    public async Task<Kontributor> AendereKontributor(long kontributorId, string name, Kontributorart art)
    {
        var antwort = await _klient.PutAsJsonAsync($"{KontributorenRoute}/{kontributorId}", new KontributorAendernAnfrage(name, art));
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        if (kontributor is null)
        {
            throw new InvalidOperationException("Die WebApi hat keinen Kontributor zurückgegeben.");
        }

        return kontributor;
    }

    public async Task<Kontributor> SetzeStilllegung(long kontributorId, bool istStillgelegt)
    {
        var antwort = await _klient.PutAsJsonAsync($"{KontributorenRoute}/{kontributorId}/stilllegung", new Stilllegung(istStillgelegt));
        antwort.EnsureSuccessStatusCode();
        var kontributor = await antwort.Content.ReadFromJsonAsync<Kontributor>();
        if (kontributor is null)
        {
            throw new InvalidOperationException("Die WebApi hat keinen Kontributor zurückgegeben.");
        }

        return kontributor;
    }

    public async Task<IReadOnlyList<Kontributor>> LadeAlleKontributoren()
    {
        var kontributoren = await _klient.GetFromJsonAsync<List<Kontributor>>(KontributorenRoute);
        if (kontributoren is null)
        {
            throw new InvalidOperationException("Die WebApi hat keine Kontributorenliste zurückgegeben.");
        }

        return kontributoren;
    }

    public void Dispose()
    {
        _klient.Dispose();
    }
}
