using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

// Probe des Wegs, den eine Datei nimmt: multipart in die WebApi, Bytes wieder heraus. Der Weg ist
// im Repository sonst nirgends belegt — es gibt keinen Datei-Upload, keinen IFormFile, kein
// Results.File. Geprüft wird auf einem im Test selbst gebauten Host, ohne eine Zeile
// Produktionscode, wie in SqliteEigenschaftenTests. Bleibt als Regressionsschutz stehen.
public class DateiwegProbeTests
{
    private const int ZwoelfKilobyte = 12 * 1024;
    private const int ZwoelfMegabyte = 12 * 1024 * 1024;
    private const long KleineGrenze = 64 * 1024;

    // Annahme 1, **widerlegt in ihrer ersten Fassung**: ein zweites Formularfeld bindet nicht ohne
    // Zutun. Ein einfacher Typ ohne Attribut kommt aus der Query, nicht aus dem multipart-Rumpf,
    // und der Aufruf endet mit 400. Erst [FromForm] holt ihn aus dem Formular.
    [Test]
    public async Task Wenn_eine_Datei_und_ein_Formularfeld_als_multipart_kommen_dann_bindet_die_Route_beide_ohne_eigenen_Binder()
    {
        await using var host = await Probehost.Starte(KleineGrenze);
        var inhalt = Bytes(ZwoelfKilobyte);

        using var antwort = await host.Klient.PostAsync("/probe/anhaenge", Multipart(inhalt, "wbs-export.md", urheber: 42));

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var gemeldet = await antwort.Content.ReadAsStringAsync();
        Assert.That(gemeldet, Is.EqualTo($"wbs-export.md|{ZwoelfKilobyte}|42"));
    }

    // Annahme 2: Results.File setzt Content-Disposition so, dass der Klient den Originalnamen
    // zurückliest — und die Bytes kommen unverändert wieder heraus.
    [Test]
    public async Task Wenn_Results_File_einen_Dateinamen_setzt_dann_liest_der_Klient_ihn_aus_Content_Disposition_und_die_Bytes_sind_gleich()
    {
        await using var host = await Probehost.Starte(KleineGrenze);
        var inhalt = Bytes(ZwoelfKilobyte);
        using var abgelegt = await host.Klient.PostAsync("/probe/anhaenge", Multipart(inhalt, "wbs-export.md", urheber: 42));
        abgelegt.EnsureSuccessStatusCode();

        using var antwort = await host.Klient.GetAsync("/probe/anhaenge/7");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(antwort.Content.Headers.ContentDisposition?.FileNameStar ?? antwort.Content.Headers.ContentDisposition?.FileName, Is.EqualTo("wbs-export.md"));
        var zurueck = await antwort.Content.ReadAsByteArrayAsync();
        Assert.That(zurueck, Is.EqualTo(inhalt));
    }

    // Annahme 3, die Fault-Injection und die wichtigste der drei: eine Datei über der Grenze
    // scheitert **sichtbar** — sie wird nicht still abgeschnitten. Würde sie abgeschnitten, läge
    // eine halbe Datei mit voller Dateigroesse in der Ablage.
    [Test]
    public async Task Wenn_die_Datei_die_Laengengrenze_ueberschreitet_dann_scheitert_der_Aufruf_sichtbar_statt_still_abzuschneiden()
    {
        await using var host = await Probehost.Starte(KleineGrenze);
        var inhalt = Bytes(ZwoelfMegabyte);

        using var antwort = await host.Klient.PostAsync("/probe/anhaenge", Multipart(inhalt, "burndown-r2.png", urheber: 42));

        Assert.That((int)antwort.StatusCode, Is.InRange(400, 599));
        var gemeldet = await antwort.Content.ReadAsStringAsync();
        Assert.That(gemeldet, Does.Not.Contain($"|{ZwoelfMegabyte}|"), "Die Route hat die zu große Datei angenommen.");
        Assert.That(host.LetzteGeschriebeneLaenge, Is.Null, "Die Route hat aus der zu großen Datei stillschweigend eine kurze gemacht.");
    }

    private static MultipartFormDataContent Multipart(byte[] inhalt, string dateiname, long urheber)
    {
        var rumpf = new MultipartFormDataContent();
        var datei = new ByteArrayContent(inhalt);
        datei.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        rumpf.Add(datei, "datei", dateiname);
        rumpf.Add(new StringContent(urheber.ToString(System.Globalization.CultureInfo.InvariantCulture)), "kontributor");
        return rumpf;
    }

    private static byte[] Bytes(int laenge)
    {
        var inhalt = new byte[laenge];
        for (var stelle = 0; stelle < laenge; stelle++)
        {
            inhalt[stelle] = (byte)(stelle % 251);
        }

        return inhalt;
    }

    // Der Probe-Host: eigene Routen, eigene Grenze, kein Produktionscode. Er läuft auf einem vom
    // Betriebssystem vergebenen Port, damit die Probe keinen belegten Port trifft.
    private sealed class Probehost : IAsyncDisposable
    {
        private readonly WebApplication _anwendung;
        private byte[] _abgelegteBytes = [];
        private string _abgelegterName = string.Empty;

        private Probehost(WebApplication anwendung)
        {
            _anwendung = anwendung;
        }

        public HttpClient Klient { get; private set; } = new();

        public long? LetzteGeschriebeneLaenge { get; private set; }

        public static async Task<Probehost> Starte(long laengengrenze)
        {
            var bauer = WebApplication.CreateSlimBuilder();
            bauer.WebHost.UseUrls("http://127.0.0.1:0");
            bauer.Services.Configure<FormOptions>(optionen => optionen.MultipartBodyLengthLimit = laengengrenze);
            var anwendung = bauer.Build();
            var host = new Probehost(anwendung);
            host.Registriere(anwendung);
            await anwendung.StartAsync();
            host.Klient = new HttpClient { BaseAddress = new Uri(host.Adresse()) };
            return host;
        }

        // DisableAntiforgery ist Teil der Probe: eine Minimal-API-Route mit Formularbindung trägt
        // Antiforgery-Metadaten, und ohne die Middleware oder diese Abschaltung scheitert schon
        // der erste Aufruf.
        private void Registriere(WebApplication anwendung)
        {
            anwendung.MapPost("/probe/anhaenge", async (IFormFile datei, [FromForm] long kontributor) =>
            {
                LetzteGeschriebeneLaenge = null;
                _abgelegterName = datei.FileName;
                using var ziel = new MemoryStream();
                await using var quelle = datei.OpenReadStream();
                await quelle.CopyToAsync(ziel);
                _abgelegteBytes = ziel.ToArray();
                LetzteGeschriebeneLaenge = _abgelegteBytes.Length;
                return Results.Text($"{_abgelegterName}|{_abgelegteBytes.Length}|{kontributor}");
            }).DisableAntiforgery();

            anwendung.MapGet("/probe/anhaenge/{anhangId:long}", () =>
            {
                return Results.File(new MemoryStream(_abgelegteBytes), "application/octet-stream", _abgelegterName);
            });
        }

        private string Adresse()
        {
            var adressen = _anwendung.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
            var erste = adressen?.Addresses.FirstOrDefault();
            if (erste is null)
            {
                throw new InvalidOperationException("Der Probe-Host hat keine Adresse gemeldet.");
            }

            return erste;
        }

        public async ValueTask DisposeAsync()
        {
            Klient.Dispose();
            await _anwendung.StopAsync();
            await _anwendung.DisposeAsync();
        }
    }
}
