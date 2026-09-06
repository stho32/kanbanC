using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Services;

public sealed class KartenApiKlient
{
    private const string KlientName = "KanbanC";
    private const string BoardsRoute = "api/boards";

    // Ohne Board in der Adresse: wer /karten/14 oeffnet, kennt das Board noch nicht.
    private const string KartenRoute = "api/karten";

    // Die Feldnamen des multipart-Rumpfs sind Vertrag mit der WebApi: dort heissen die Parameter
    // der Route genauso.
    private const string Dateifeld = "datei";
    private const string Urheberfeld = "kontributor";
    private const string Anhanginhaltstyp = "application/octet-stream";
    private readonly IHttpClientFactory _klientFabrik;

    public KartenApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
    }

    public async Task<ApiErgebnis<Kartendetail>> LadeKartendetail(long karteId)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.GetAsync($"{KartenRoute}/{karteId}");
        return await AlsKartendetail(antwort);
    }

    public async Task<ApiErgebnis<Kartendetail>> AendereKarte(long karteId, KarteAendernAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{KartenRoute}/{karteId}", anfrage);
        return await AlsKartendetail(antwort);
    }

    // Die ganze Liste, nicht ein Zugang: was hier reist, ist danach die Liste der Karte.
    public async Task<ApiErgebnis<Kartendetail>> SetzeEtiketten(long karteId, Kartenetiketten etiketten)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{KartenRoute}/{karteId}/etiketten", etiketten);
        return await AlsKartendetail(antwort);
    }

    // Eine Zeile, nicht die ganze Liste — anders als bei den Etiketten. Zurueck kommt trotzdem das
    // ganze Kartendetail, weil die Seite eine Quelle behaelt und nach dem Schreiben nicht nachlaedt.
    public async Task<ApiErgebnis<Kartendetail>> LegeTeilaufgabeAn(long karteId, TeilaufgabeAnlegenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync($"{KartenRoute}/{karteId}/teilaufgaben", anfrage);
        return await AlsKartendetail(antwort);
    }

    // Setzt den Stand, statt ihn zu kippen: derselbe Aufruf zweimal laesst denselben Stand stehen.
    public async Task<ApiErgebnis<Kartendetail>> SetzeAbhakung(long karteId, long teilaufgabeId, Teilaufgabenstand stand)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{KartenRoute}/{karteId}/teilaufgaben/{teilaufgabeId}", stand);
        return await AlsKartendetail(antwort);
    }

    // Eine Zeile, und zurueck kommt das ganze Kartendetail — wie beim Anlegen einer Teilaufgabe.
    // Der Urheber reist im **Rumpf** der Anfrage und nicht als Query: der Rumpf ist der Ort, an
    // dem dieses Projekt Kontributoren uebergibt, und zwei Wege fuer denselben Wert waeren
    // Synonym-Wildwuchs an der Schnittstelle. Einen Zeitpunkt schickt der Klient nicht mit — den
    // setzt die WebApi.
    public async Task<ApiErgebnis<Kartendetail>> SchreibeKommentar(long karteId, KommentarSchreibenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync($"{KartenRoute}/{karteId}/kommentare", anfrage);
        return await AlsKartendetail(antwort);
    }

    // Datei und Urheber reisen im **selben** multipart-Rumpf, wie die WebApi ihn erwartet: das
    // Feld „datei" traegt die Bytes samt Originalnamen, das Feld „kontributor" die Nummer. Kein
    // Query-Parameter — der Rumpf ist der Ort, an dem dieses Projekt Kontributoren uebergibt.
    // Der Strom fliesst durch: er wird nicht vorher in ein Byte-Array gelesen, damit bei 10 MB
    // kein vermeidbarer Druck auf den Arbeitsspeicher entsteht.
    // Einen Zeitpunkt schickt der Klient nicht mit — den setzt die WebApi.
    public async Task<ApiErgebnis<Kartendetail>> HaengeAnhangAn(long karteId, long kontributorId, string dateiname, Stream inhalt)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var rumpf = new MultipartFormDataContent();
        using var datei = new StreamContent(inhalt);
        datei.Headers.ContentType = new MediaTypeHeaderValue(Anhanginhaltstyp);
        rumpf.Add(datei, Dateifeld, dateiname);
        rumpf.Add(new StringContent(kontributorId.ToString(CultureInfo.InvariantCulture)), Urheberfeld);
        using var antwort = await klient.PostAsync($"{KartenRoute}/{karteId}/anhaenge", rumpf);
        return await AlsKartendetail(antwort);
    }

    // Zurueck kommt das ganze Kartendetail, wie beim Anhaengen — die Seite behaelt eine Quelle.
    // **Keine Methode, die Bytes liest:** die holt der Browser direkt von der WebApi. Jede Methode
    // hier endet in ReadFromJsonAsync, und ein zweiter Rueckweg braechte eine zweite Leseform und
    // einen Fehlerpfad ohne Befund-Rumpf.
    public async Task<ApiErgebnis<Kartendetail>> EntferneAnhang(long karteId, long anhangId)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.DeleteAsync($"{KartenRoute}/{karteId}/anhaenge/{anhangId}");
        return await AlsKartendetail(antwort);
    }

    // Eine Zeile, und zurueck kommt das ganze Kartendetail — wie beim Kommentar. **JSON in beide
    // Richtungen**, anders als beim Anhang: ein Dateiverweis traegt einen Pfad und keine Bytes,
    // also keine multipart-Form und keine Direktadresse an der WebApi. Der Urheber reist im
    // **Rumpf** und nicht als Query, wie jeder Kontributor in diesem Projekt. Einen Zeitpunkt
    // schickt der Klient nicht mit — den setzt die WebApi.
    public async Task<ApiErgebnis<Kartendetail>> TrageDateiverweisEin(long karteId, DateiverweisEintragenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync($"{KartenRoute}/{karteId}/dateiverweise", anfrage);
        return await AlsKartendetail(antwort);
    }

    // Zurueck kommt das ganze Kartendetail, wie beim Eintragen — die Seite behaelt eine Quelle.
    // **Keine Methode zum Aendern:** es gibt keine solche Route, und eine Methode ohne Route
    // waere tote Flexibilitaet.
    public async Task<ApiErgebnis<Kartendetail>> EntferneDateiverweis(long karteId, long dateiverweisId)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.DeleteAsync($"{KartenRoute}/{karteId}/dateiverweise/{dateiverweisId}");
        return await AlsKartendetail(antwort);
    }

    // 400 und 404 laufen denselben Weg, weil beide einen Befund der WebApi tragen.
    // ApiAntwortleser waere die falsche Stelle: sein 404-Zweig ersetzt jeden Befund durch eine
    // Board-Meldung, und diese Route kennt kein Board.
    private static async Task<ApiErgebnis<Kartendetail>> AlsKartendetail(HttpResponseMessage antwort)
    {
        var dieWebApiHatDenAufrufZurueckgewiesen = antwort.StatusCode == HttpStatusCode.BadRequest || antwort.StatusCode == HttpStatusCode.NotFound;
        if (dieWebApiHatDenAufrufZurueckgewiesen)
        {
            var zurueckweisung = await Zurueckweisungsleser.Lies(antwort);
            return ApiErgebnis<Kartendetail>.Zurueckgewiesen(zurueckweisung);
        }

        antwort.EnsureSuccessStatusCode();
        var detail = await antwort.Content.ReadFromJsonAsync<Kartendetail>();
        if (detail is null)
        {
            throw new InvalidOperationException("Die WebApi hat kein Kartendetail zurückgegeben.");
        }

        return ApiErgebnis<Kartendetail>.Erfolg(detail);
    }

    public async Task<ApiErgebnis<Karte>> LegeKarteAn(long boardId, long spalteId, KarteAnlegenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten", anfrage);
        return await ApiAntwortleser.AlsErgebnis<Karte>(antwort);
    }

    // Ungekürzt: dieselbe Adresse, auf der eine Karte entsteht, liefert alle Karten der Spalte.
    public async Task<ApiErgebnis<IReadOnlyList<Karte>>> LadeKartenDerSpalte(long boardId, long spalteId)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.GetAsync($"{BoardsRoute}/{boardId}/spalten/{spalteId}/karten");
        return await ApiAntwortleser.AlsErgebnis<IReadOnlyList<Karte>>(antwort);
    }

    public async Task<ApiErgebnis<IReadOnlyList<Spalte>>> VerschiebeKarte(long boardId, long karteId, Kartenlage lage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/karten/{karteId}/lage", lage);
        return await ApiAntwortleser.AlsErgebnis<IReadOnlyList<Spalte>>(antwort);
    }

    // Dieselbe Antwortgestalt wie der Zug: die Spalten kommen zurück, weil die Bahn eine Karte
    // verliert und neu durchnummeriert wird. Derselbe Aufruf mit false holt die Karte zurück.
    public async Task<ApiErgebnis<IReadOnlyList<Spalte>>> SchalteArchivierung(long boardId, long karteId, Archivierung archivierung)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{BoardsRoute}/{boardId}/karten/{karteId}/archivierung", archivierung);
        return await ApiAntwortleser.AlsErgebnis<IReadOnlyList<Spalte>>(antwort);
    }
}
