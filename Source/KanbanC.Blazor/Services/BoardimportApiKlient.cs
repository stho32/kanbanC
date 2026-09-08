using System.Net.Http.Headers;
using KanbanC.Contracts.Boardimport;

namespace KanbanC.Blazor.Services;

// Der Weg der Oberfläche zum Boardimport — **derselbe**, den ein Agent ohne Browser geht. Die
// Oberfläche hat hier keinen Vorsprung: sie setzt nur die zwei Felder, die die Route ohnehin
// führt.
// multipart wie beim WBS-Import und beim Anhang: die Datei reist neben dem Feld, und die
// Feldnamen sind Vertrag mit der WebApi — dort heißen die Parameter der Route genauso.
public sealed class BoardimportApiKlient
{
    private const string KlientName = "KanbanC";
    private const string Importroute = "api/boards/import";
    private const string Dateifeld = "datei";
    private const string Trockenfeld = "trocken";
    private const string Dateiinhaltstyp = "application/octet-stream";
    private readonly IHttpClientFactory _klientFabrik;

    public BoardimportApiKlient(IHttpClientFactory klientFabrik)
    {
        _klientFabrik = klientFabrik;
    }

    // **Trocken reist immer ausdrücklich mit**, auch wenn die WebApi es ohnehin voraussetzt: der
    // Schirm setzt das Feld im ersten Schritt auf true und im zweiten auf false, und eine
    // Auslassung ließe die Absicht raten.
    public async Task<ApiErgebnis<Boardimportbericht>> Importiere(Boardimportauftrag auftrag, Stream datei)
    {
        using var klient = _klientFabrik.CreateClient(KlientName);
        using var rumpf = new MultipartFormDataContent();
        using var inhalt = new StreamContent(datei);
        inhalt.Headers.ContentType = new MediaTypeHeaderValue(Dateiinhaltstyp);
        rumpf.Add(inhalt, Dateifeld, auftrag.Dateiname);
        rumpf.Add(new StringContent(Wahrheitswert(auftrag.Trocken)), Trockenfeld);
        using var antwort = await klient.PostAsync(Importroute, rumpf);
        return await ApiAntwortleser.AlsErgebnis<Boardimportbericht>(antwort);
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
