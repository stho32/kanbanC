using System.Net;
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
}
