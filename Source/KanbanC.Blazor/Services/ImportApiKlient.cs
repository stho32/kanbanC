using System.Globalization;
using System.Net.Http.Headers;
using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Services;

// Der Weg der Oberfläche zum WBS-Import — **derselbe**, den ein Agent ohne Browser geht. Die
// Oberfläche hat hier keinen Vorsprung: sie setzt nur die Felder, die im Schirm stehen.
// multipart wie beim Anhang: die Datei reist neben den Feldern, und die Feldnamen sind Vertrag mit
// der WebApi — dort heißen die Parameter der Route genauso.
public sealed class ImportApiKlient
{
    private const string KlientName = "KanbanC";
    private const string BoardsRoute = "api/boards";
    private const string Dateifeld = "datei";
    private const string Klassenfeld = "klasse";
    private const string Schnittebenenfeld = "schnittebene";
    private const string Pfadfeld = "pfad";
    private const string Trockenfeld = "trocken";
    private const string Urheberfeld = "kontributor";
    private const string Dateiinhaltstyp = "application/octet-stream";
    private readonly IHttpClientFactory _klientFabrik;

    public ImportApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
    }

    // **Trocken reist immer mit**, auch wenn die WebApi es ohnehin voraussetzt: der Schirm setzt
    // das Feld in Schritt 2 auf true und in Schritt 3 auf false, und eine Auslassung ließe die
    // Absicht raten.
    public async Task<ApiErgebnis<Importbericht>> Importiere(long boardId, Importauftrag auftrag, Stream datei)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var rumpf = new MultipartFormDataContent();
        using var inhalt = new StreamContent(datei);
        inhalt.Headers.ContentType = new MediaTypeHeaderValue(Dateiinhaltstyp);
        rumpf.Add(inhalt, Dateifeld, auftrag.Dateiname);
        rumpf.Add(new StringContent(auftrag.Kartenklasse.ToString(CultureInfo.InvariantCulture)), Klassenfeld);
        rumpf.Add(new StringContent(auftrag.Schnittebene.ToString()), Schnittebenenfeld);
        rumpf.Add(new StringContent(auftrag.Pfad), Pfadfeld);
        rumpf.Add(new StringContent(Wahrheitswert(auftrag.Trocken)), Trockenfeld);
        rumpf.Add(new StringContent(auftrag.Kontributor.ToString(CultureInfo.InvariantCulture)), Urheberfeld);
        using var antwort = await klient.PostAsync($"{BoardsRoute}/{boardId}/wbs-import", rumpf);
        return await ApiAntwortleser.AlsErgebnis<Importbericht>(antwort);
    }

    // Die Formularbindung der WebApi liest „true“ und „false“ — nicht „True“, wie ToString es
    // schriebe.
    private static string Wahrheitswert(bool wert)
    {
        if (wert)
        {
            return "true";
        }

        return "false";
    }
}
