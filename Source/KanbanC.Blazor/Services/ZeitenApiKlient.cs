using System.Net;
using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Zeiten;

namespace KanbanC.Blazor.Services;

public sealed class ZeitenApiKlient
{
    private const string KlientName = "KanbanC";

    // Ohne Board in der Adresse: wer /karten/14 offen hat, kennt das Board nur aus der Antwort.
    private const string KartenRoute = "api/karten";
    private readonly IHttpClientFactory _klientFabrik;

    public ZeitenApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
    }

    // JSON in beide Richtungen, Muster KartenApiKlient. Der Kontributor reist im **Rumpf** und
    // nicht als Query, wie jeder Kontributor in diesem Projekt; einen Beginn schickt der Klient
    // nicht mit — den setzt die WebApi.
    // **201 und 200 laufen denselben Weg:** beide tragen den Zeiteintrag, und für die Oberfläche
    // ist der Unterschied ohne Belang — sie zeigt danach so oder so „läuft seit". Wer ihn braucht,
    // ist der Agent an der API.
    public async Task<ApiErgebnis<Zeiteintrag>> StarteZeitmessung(long karteId, ZeitmessungStartenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync($"{KartenRoute}/{karteId}/zeiten/laufend", anfrage);
        return await AlsZeiteintrag(antwort);
    }

    // **PutAsync ohne Inhalt** statt PutAsJsonAsync: der Aufruf hat keinen Rumpf, und einen
    // Kontributor nennt er nicht — jeder darf stoppen. Zurück kommt der beendete Eintrag, und die
    // Antwort läuft durch dieselbe Lesehilfe wie beim Start.
    public async Task<ApiErgebnis<Zeiteintrag>> BeendeZeitmessung(long karteId, long zeiteintragId)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsync($"{KartenRoute}/{karteId}/zeiten/{zeiteintragId}/ende", null);
        return await AlsZeiteintrag(antwort);
    }

    // Der Nachtrag geht an „…/zeiten" ohne „laufend": eine andere Adresse für eine andere Frage.
    // Beginn **und** Ende reisen mit — die WebApi setzt hier nichts selbst.
    public async Task<ApiErgebnis<Zeiteintrag>> TrageNach(long karteId, ZeiteintragNachtragenAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PostAsJsonAsync($"{KartenRoute}/{karteId}/zeiten", anfrage);
        return await AlsZeiteintrag(antwort);
    }

    // Dieselbe Adresse wie das Löschen, anderes Verb. Ein Ende von null reist als JSON-null mit
    // und macht den Eintrag wieder laufend.
    public async Task<ApiErgebnis<Zeiteintrag>> Aendere(long karteId, long zeiteintragId, ZeiteintragAendernAnfrage anfrage)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.PutAsJsonAsync($"{KartenRoute}/{karteId}/zeiten/{zeiteintragId}", anfrage);
        return await AlsZeiteintrag(antwort);
    }

    // **Ohne Rumpf**, und zurück kommt nicht der gelöschte Eintrag, sondern das ganze
    // Kartendetail ohne ihn — die zweite Antwortgestalt dieses Klienten braucht deshalb eine
    // eigene Lesehilfe.
    public async Task<ApiErgebnis<Kartendetail>> Loesche(long karteId, long zeiteintragId)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var antwort = await klient.DeleteAsync($"{KartenRoute}/{karteId}/zeiten/{zeiteintragId}");
        return await AlsKartendetail(antwort);
    }

    // 400 und 404 laufen denselben Weg, weil beide einen Befund der WebApi tragen.
    // ApiAntwortleser wäre die falsche Stelle: sein 404-Zweig ersetzt jeden Befund durch eine
    // Board-Meldung, und diese Route kennt kein Board.
    private static async Task<ApiErgebnis<Zeiteintrag>> AlsZeiteintrag(HttpResponseMessage antwort)
    {
        var dieWebApiHatDenAufrufZurueckgewiesen = antwort.StatusCode == HttpStatusCode.BadRequest || antwort.StatusCode == HttpStatusCode.NotFound;
        if (dieWebApiHatDenAufrufZurueckgewiesen)
        {
            var zurueckweisung = await Zurueckweisungsleser.Lies(antwort);
            return ApiErgebnis<Zeiteintrag>.Zurueckgewiesen(zurueckweisung);
        }

        antwort.EnsureSuccessStatusCode();
        var zeiteintrag = await antwort.Content.ReadFromJsonAsync<Zeiteintrag>();
        if (zeiteintrag is null)
        {
            throw new InvalidOperationException("Die WebApi hat keinen Zeiteintrag zurückgegeben.");
        }

        return ApiErgebnis<Zeiteintrag>.Erfolg(zeiteintrag);
    }

    // Die Schwester von AlsZeiteintrag für die zweite Antwortgestalt; dieselbe Behandlung der
    // Zurückweisung, weil dieselben Befunde kommen.
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
}
