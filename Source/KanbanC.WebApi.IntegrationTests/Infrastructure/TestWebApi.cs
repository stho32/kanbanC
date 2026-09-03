using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace KanbanC.WebApi.IntegrationTests.Infrastructure;

public sealed class TestWebApi : IDisposable
{
    private const string VerbindungsSchluessel = "Datenhaltung:Verbindungszeichenfolge";
    private const string NachladenSchluessel = "hostBuilder:reloadConfigOnChange";
    private readonly WebApplicationFactory<Program> _fabrik;

    public TestWebApi(string datenbankDateipfad)
    {
        // Jeder Test baut einen eigenen Host. Ohne die Abschaltung legt jeder von ihnen
        // Dateiwächter für die Konfiguration an und der Lauf schöpft das Kontingent des
        // Systems aus — geändertes appsettings nachzuladen braucht kein Test.
        _fabrik = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseSetting(VerbindungsSchluessel, $"Data Source={datenbankDateipfad}");
            host.UseSetting(NachladenSchluessel, "false");
        });
        Klient = _fabrik.CreateClient();
    }

    public HttpClient Klient { get; }

    // Die registrierten Routen als „METHODE /pfad“. Der Vertragstest liest sie, damit ein neuer
    // Endpunkt nicht stillschweigend an der Prüfung vorbeikommt.
    public IReadOnlyList<string> Routen
    {
        get
        {
            var quelle = _fabrik.Services.GetRequiredService<EndpointDataSource>();
            var routen = new List<string>();
            foreach (var endpunkt in quelle.Endpoints.OfType<RouteEndpoint>())
            {
                routen.Add($"{Methode(endpunkt)} {endpunkt.RoutePattern.RawText}");
            }

            return routen;
        }
    }

    private static string Methode(RouteEndpoint endpunkt)
    {
        var methoden = endpunkt.Metadata.GetMetadata<IHttpMethodMetadata>();
        if (methoden is null)
        {
            return "?";
        }

        return string.Join("|", methoden.HttpMethods);
    }

    public void Dispose()
    {
        Klient.Dispose();
        _fabrik.Dispose();
    }
}
