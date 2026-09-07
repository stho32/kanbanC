using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Import;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Klassen;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.PlaywrightTests.Infrastructure;

// Der Weg des Agenten: dieselben Routen, die die Oberfläche ruft, nur ohne Browser.
public sealed class WebApiKlient : IDisposable
{
    private const string BoardsRoute = "api/boards";
    private const string KontributorenRoute = "api/kontributoren";
    private const string KartenRoute = "api/karten";
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

    // Der Weg des Agenten ins Gespraech an der Karte: eine Zeile je Aufruf, mit Urheber im Rumpf.
    public async Task<Kartendetail> SchreibeKommentar(long karteId, string text, long kontributorId)
    {
        var antwort = await _klient.PostAsJsonAsync($"api/karten/{karteId}/kommentare", new KommentarSchreibenAnfrage(text, kontributorId));
        antwort.EnsureSuccessStatusCode();
        return await AlsKartendetail(antwort);
    }

    // Der Weg des Agenten an die Bezuege der Karte: eine Zeile je Aufruf, mit Urheber im Rumpf.
    // **JSON in beide Richtungen** — anders als beim Anhang reisen hier keine Bytes.
    public async Task<Kartendetail> TrageDateiverweisEin(long karteId, string pfad, long kontributorId)
    {
        var antwort = await _klient.PostAsJsonAsync($"api/karten/{karteId}/dateiverweise", new DateiverweisEintragenAnfrage(pfad, kontributorId));
        antwort.EnsureSuccessStatusCode();
        return await AlsKartendetail(antwort);
    }

    // Der Weg des Agenten in die Zeiterfassung: eine Zeile je Aufruf, mit dem Kontributor im
    // Rumpf. Einen Beginn schickt er nicht mit — den setzt die WebApi.
    public async Task<Zeiteintrag> StarteZeitmessung(long karteId, long kontributorId)
    {
        var antwort = await _klient.PostAsJsonAsync($"api/karten/{karteId}/zeiten/laufend", new ZeitmessungStartenAnfrage(kontributorId));
        antwort.EnsureSuccessStatusCode();
        var zeiteintrag = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        if (zeiteintrag is null)
        {
            throw new InvalidOperationException("Die WebApi hat keinen Zeiteintrag zurückgegeben.");
        }

        return zeiteintrag;
    }

    // Ohne Rumpf und ohne Kontributor: jeder darf stoppen, und das Ende setzt die WebApi.
    public async Task<Zeiteintrag> BeendeZeitmessung(long karteId, long zeiteintragId)
    {
        var antwort = await _klient.PutAsync($"api/karten/{karteId}/zeiten/{zeiteintragId}/ende", null);
        antwort.EnsureSuccessStatusCode();
        var zeiteintrag = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        if (zeiteintrag is null)
        {
            throw new InvalidOperationException("Die WebApi hat keinen Zeiteintrag zurückgegeben.");
        }

        return zeiteintrag;
    }

    // Derselbe Aufruf ohne EnsureSuccessStatusCode: der Test will die Zurueckweisung sehen, statt
    // an ihr zu scheitern.
    public async Task<HttpResponseMessage> VersucheZeitmessungZuStarten(long karteId, long kontributorId)
    {
        return await _klient.PostAsJsonAsync($"api/karten/{karteId}/zeiten/laufend", new ZeitmessungStartenAnfrage(kontributorId));
    }

    public async Task<Kartendetail> LadeKartendetail(long karteId)
    {
        var detail = await _klient.GetFromJsonAsync<Kartendetail>($"api/karten/{karteId}");
        if (detail is null)
        {
            throw new InvalidOperationException("Die WebApi hat kein Kartendetail zurückgegeben.");
        }

        return detail;
    }

    // Der Weg des Agenten in die Ablage: Datei und Urheber im selben multipart-Rumpf.
    public async Task<Kartendetail> HaengeAnhangAn(long karteId, string dateiname, byte[] inhalt, long kontributorId)
    {
        using var rumpf = new MultipartFormDataContent();
        var datei = new ByteArrayContent(inhalt);
        datei.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        rumpf.Add(datei, "datei", dateiname);
        rumpf.Add(new StringContent(kontributorId.ToString(CultureInfo.InvariantCulture)), "kontributor");
        var antwort = await _klient.PostAsync($"api/karten/{karteId}/anhaenge", rumpf);
        antwort.EnsureSuccessStatusCode();
        return await AlsKartendetail(antwort);
    }

    // Der Weg des Agenten in den WBS-Import: dieselbe Route, die der Schirm ruft, nur ohne
    // Browser — und mit trocken=false, weil dieser Aufrufer schreiben will.
    public async Task<Importbericht> ImportiereWbs(long boardId, string dateiname, string inhalt, long kartenklasseId, long kontributorId, string pfad)
    {
        using var rumpf = new MultipartFormDataContent();
        var datei = new ByteArrayContent(Encoding.UTF8.GetBytes(inhalt));
        datei.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        rumpf.Add(datei, "datei", dateiname);
        rumpf.Add(new StringContent(kartenklasseId.ToString(CultureInfo.InvariantCulture)), "klasse");
        rumpf.Add(new StringContent("Interaction"), "schnittebene");
        rumpf.Add(new StringContent(pfad), "pfad");
        rumpf.Add(new StringContent("false"), "trocken");
        rumpf.Add(new StringContent(kontributorId.ToString(CultureInfo.InvariantCulture)), "kontributor");
        var antwort = await _klient.PostAsync($"{BoardsRoute}/{boardId}/wbs-import", rumpf);
        antwort.EnsureSuccessStatusCode();
        var bericht = await antwort.Content.ReadFromJsonAsync<Importbericht>();
        if (bericht is null)
        {
            throw new InvalidOperationException("Die WebApi hat keinen Importbericht zurückgegeben.");
        }

        return bericht;
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

    // Der Weg des Agenten an den Nummernkreis: Name und Praefix im Rumpf, die angelegte
    // Kartenklasse zurück.
    public async Task<Kartenklasse> LegeKartenklasseAn(long boardId, string name, string praefix)
    {
        var antwort = await _klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/kartenklassen", new KartenklasseAnlegenAnfrage(name, praefix));
        antwort.EnsureSuccessStatusCode();
        var kartenklasse = await antwort.Content.ReadFromJsonAsync<Kartenklasse>();
        if (kartenklasse is null)
        {
            throw new InvalidOperationException("Die WebApi hat keine Kartenklasse zurückgegeben.");
        }

        return kartenklasse;
    }

    public async Task<Kartendetail> OrdneKartenklasseZu(long karteId, long kartenklasseId)
    {
        var antwort = await _klient.PutAsJsonAsync($"{KartenRoute}/{karteId}/kartenklasse", new KartenklasseZuordnenAnfrage(kartenklasseId));
        antwort.EnsureSuccessStatusCode();
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        if (detail is null)
        {
            throw new InvalidOperationException("Die WebApi hat kein Kartendetail zurückgegeben.");
        }

        return detail;
    }

    // Zurück kommen die Spalten des Boards ohne die archivierte Karte — Hausform der
    // Kartenarchivierung.
    public async Task<IReadOnlyList<Spalte>> SchalteKartenarchivierung(long boardId, long karteId, bool istArchiviert)
    {
        var antwort = await _klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/karten/{karteId}/archivierung", new Archivierung(istArchiviert));
        antwort.EnsureSuccessStatusCode();
        var spalten = await antwort.Content.ReadFromJsonAsync<List<Spalte>>();
        if (spalten is null)
        {
            throw new InvalidOperationException("Die WebApi hat keine Spalten zurückgegeben.");
        }

        return spalten;
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
