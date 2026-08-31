using KanbanC.PlaywrightTests.Infrastructure;

[assembly: Parallelizable(ParallelScope.None)]

namespace KanbanC.PlaywrightTests;

[SetUpFixture]
public sealed class Testumgebung
{
    private const string ZustandsPfad = "/api/zustand";
    private const string BrowserVariable = "BROWSER";
    private const string Browser = "chromium";
    private const string StartseitenPfad = "/";
    private static Testumgebung? _aktuelle;
    private readonly string _webApiProjekt;
    private readonly string _blazorProjekt;
    private readonly string _konfiguration;
    private Dienstprozess? _blazor;
    private Dienstprozess? _webApi;
    private string? _datenbankDateipfad;

    public Testumgebung()
    {
        var wurzel = RepositoryWurzel();
        _konfiguration = Buildkonfiguration();
        _webApiProjekt = Path.Combine(wurzel, "Source", "KanbanC.WebApi");
        _blazorProjekt = Path.Combine(wurzel, "Source", "KanbanC.Blazor");
        WebApiAdresse = $"http://127.0.0.1:{FreierPort.Ermittle()}";
        BlazorAdresse = $"http://127.0.0.1:{FreierPort.Ermittle()}";
    }

    public static Testumgebung Aktuelle
    {
        get
        {
            if (_aktuelle is null)
            {
                throw new InvalidOperationException("Die Testumgebung ist nicht gestartet.");
            }

            return _aktuelle;
        }
    }

    public string BlazorAdresse { get; }

    public string WebApiAdresse { get; }

    [OneTimeSetUp] // stil-check: C18 Prozess-Infrastruktur (Blazor einmal je Lauf), kein Arrange eines Tests
    public async Task StarteBlazor()
    {
        // Die Shell-Variable BROWSER (Standardbrowser des Nutzers) würde die .runsettings von Playwright überstimmen.
        Environment.SetEnvironmentVariable(BrowserVariable, Browser);
        var umgebung = new Dictionary<string, string> { ["WebApi__BasisAdresse"] = WebApiAdresse + "/" };
        _blazor = await Dienstprozess.Starte(_blazorProjekt, Assembly(_blazorProjekt), BlazorAdresse, umgebung, StartseitenPfad);
        _aktuelle = this;
    }

    public async Task StarteWebApiMitLeererDatenbank()
    {
        StoppeWebApi();
        LoescheDatenbank();
        _datenbankDateipfad = Path.Combine(Path.GetTempPath(), $"kanbanc-e2e-{Guid.NewGuid():N}.db");
        await StarteWebApi();
    }

    public async Task StarteWebApiNeu()
    {
        StoppeWebApi();
        await StarteWebApi();
    }

    public void HalteWebApiAn()
    {
        StoppeWebApi();
    }

    private async Task StarteWebApi()
    {
        var umgebung = new Dictionary<string, string> { ["Datenhaltung__Verbindungszeichenfolge"] = $"Data Source={_datenbankDateipfad}" };
        _webApi = await Dienstprozess.Starte(_webApiProjekt, Assembly(_webApiProjekt), WebApiAdresse, umgebung, ZustandsPfad);
    }

    private void StoppeWebApi()
    {
        _webApi?.Dispose();
        _webApi = null;
    }

    [OneTimeTearDown] // stil-check: C18 Prozess-Infrastruktur, stoppt die Dienste nach dem letzten Test
    public void StoppeAlles()
    {
        StoppeWebApi();
        LoescheDatenbank();
        _blazor?.Dispose();
        _aktuelle = null;
    }

    private void LoescheDatenbank()
    {
        if (_datenbankDateipfad is null)
        {
            return;
        }

        File.Delete(_datenbankDateipfad);
        _datenbankDateipfad = null;
    }

    private string Assembly(string projektverzeichnis)
    {
        var projektname = Path.GetFileName(projektverzeichnis);
        return Path.Combine(projektverzeichnis, "bin", _konfiguration, "net10.0", projektname + ".dll");
    }

    private static string RepositoryWurzel()
    {
        var verzeichnis = new DirectoryInfo(AppContext.BaseDirectory);
        while (verzeichnis is not null)
        {
            var istWurzel = File.Exists(Path.Combine(verzeichnis.FullName, "KanbanC.sln"));
            if (istWurzel)
            {
                return verzeichnis.FullName;
            }

            verzeichnis = verzeichnis.Parent;
        }

        throw new DirectoryNotFoundException("KanbanC.sln wurde oberhalb des Testverzeichnisses nicht gefunden.");
    }

    private static string Buildkonfiguration()
    {
        var zielverzeichnis = new DirectoryInfo(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory));
        var konfigurationsverzeichnis = zielverzeichnis.Parent;
        if (konfigurationsverzeichnis is null)
        {
            throw new DirectoryNotFoundException("Die Buildkonfiguration ließ sich aus dem Testpfad nicht ableiten.");
        }

        return konfigurationsverzeichnis.Name;
    }
}
