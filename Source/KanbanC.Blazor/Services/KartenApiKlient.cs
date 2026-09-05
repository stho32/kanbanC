using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Services;

public sealed class KartenApiKlient
{
    private const string KlientName = "KanbanC";
    private const string BoardsRoute = "api/boards";
    private readonly IHttpClientFactory _klientFabrik;

    public KartenApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
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
