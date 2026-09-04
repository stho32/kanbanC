using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Earned Trust für den ersten Abfrageparameter des Bestands: Bindet ASP.NET einen unlesbaren
// bool-Wert vor dem Handler ab — und liefert dabei eine Fehlerantwort ohne unseren Befund —,
// oder behält der Handler die Kontrolle? Davon hängt ab, ob der Guard für „?archiviert=vielleicht"
// mit `bool?` möglich ist oder eine `string?`-Bindung braucht.
public class AbfrageparameterProbeTests
{
    private const string Route = "/probe";

    [Test]
    public async Task Wenn_ein_bool_Parameter_einen_unlesbaren_Wert_bekommt_dann_antwortet_ASP_NET_selbst_mit_400_ohne_unseren_Befund()
    {
        using var probe = new Probeanwendung(routen => routen.MapGet(Route, (bool? archiviert) => Results.Ok(new { gelesen = archiviert })));

        using var antwort = await probe.Klient.GetAsync($"{Route}?archiviert=vielleicht");

        var rumpf = await antwort.Content.ReadAsStringAsync();
        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(rumpf, Does.Not.Contain("befunde"));
    }

    [Test]
    public async Task Wenn_ein_string_Parameter_denselben_Wert_bekommt_dann_erreicht_er_den_Handler_unveraendert()
    {
        using var probe = new Probeanwendung(routen => routen.MapGet(Route, (string? archiviert) => Results.Text(archiviert ?? "fehlt")));

        using var antwort = await probe.Klient.GetAsync($"{Route}?archiviert=vielleicht");

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await antwort.Content.ReadAsStringAsync(), Is.EqualTo("vielleicht"));
    }

    [Test]
    public async Task Wenn_der_string_Parameter_fehlt_dann_kommt_er_als_null_beim_Handler_an()
    {
        using var probe = new Probeanwendung(routen => routen.MapGet(Route, (string? archiviert) => Results.Text(archiviert ?? "fehlt")));

        using var antwort = await probe.Klient.GetAsync(Route);

        Assert.That(antwort.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await antwort.Content.ReadAsStringAsync(), Is.EqualTo("fehlt"));
    }

    [Test]
    public async Task Wenn_der_string_Parameter_true_oder_false_traegt_dann_kommt_der_Text_unveraendert_beim_Handler_an()
    {
        using var probe = new Probeanwendung(routen => routen.MapGet(Route, (string? archiviert) => Results.Text(archiviert ?? "fehlt")));

        using var wahr = await probe.Klient.GetAsync($"{Route}?archiviert=true");
        using var falsch = await probe.Klient.GetAsync($"{Route}?archiviert=FALSE");

        Assert.That(await wahr.Content.ReadAsStringAsync(), Is.EqualTo("true"));
        Assert.That(await falsch.Content.ReadAsStringAsync(), Is.EqualTo("FALSE"));
    }

    private sealed class Probeanwendung : IDisposable
    {
        private readonly WebApplication _anwendung;

        public Probeanwendung(Action<WebApplication> registriereRouten)
        {
            var erbauer = WebApplication.CreateBuilder();
            erbauer.WebHost.UseTestServer();
            _anwendung = erbauer.Build();
            registriereRouten(_anwendung);
            _anwendung.StartAsync().GetAwaiter().GetResult();
            Klient = _anwendung.GetTestClient();
        }

        public HttpClient Klient { get; }

        public void Dispose()
        {
            Klient.Dispose();
            _anwendung.StopAsync().GetAwaiter().GetResult();
            ((IDisposable)_anwendung).Dispose();
        }
    }
}
