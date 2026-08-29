using Microsoft.AspNetCore.Mvc.Testing;

namespace KanbanC.WebApi.IntegrationTests.Infrastructure;

public sealed class TestWebApi : IDisposable
{
    private const string VerbindungsSchluessel = "Datenhaltung:Verbindungszeichenfolge";
    private readonly WebApplicationFactory<Program> _fabrik;

    public TestWebApi(string datenbankDateipfad)
    {
        _fabrik = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
            host.UseSetting(VerbindungsSchluessel, $"Data Source={datenbankDateipfad}"));
        Klient = _fabrik.CreateClient();
    }

    public HttpClient Klient { get; }

    public void Dispose()
    {
        Klient.Dispose();
        _fabrik.Dispose();
    }
}
