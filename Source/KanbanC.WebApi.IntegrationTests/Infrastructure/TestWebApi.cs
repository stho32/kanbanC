using Microsoft.AspNetCore.Mvc.Testing;

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

    public void Dispose()
    {
        Klient.Dispose();
        _fabrik.Dispose();
    }
}
