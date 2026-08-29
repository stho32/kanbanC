# Testing Strategy

## Test-Pyramide

```
  ╔══════════════════════════════════════════════╗
  ║  SAST (CodeQL + Roslyn Analyzers)            ║  Uebergreifend: Statische Analyse
  ╚══════════════════════════════════════════════╝
         ┌──────────┐
         │Playwright │   E2E: Wenige, kritische User-Journeys
         │  E2E     │   Source/MyApp.PlaywrightTests/
         ├──────────┤
         │Integration│   Integration: Web+BL zusammen via WebApplicationFactory
         │  Tests   │   Source/MyApp.BL.IntegrationTests/
         ├──────────┤
         │   Unit   │   Unit: Operations und Integrations isoliert
         │  Tests   │   Source/MyApp.BL.Tests/
         └──────────┘
```

| Ebene | Projekt | Framework | Anzahl | Geschwindigkeit |
|---|---|---|---|---|
| Unit | MyApp.BL.Tests | NUnit + NSubstitute | Viele (80%+) | Schnell (<1s) |
| Integration | MyApp.BL.IntegrationTests | NUnit + WebApplicationFactory | Mittel (15%) | Mittel (1-5s) |
| E2E | MyApp.PlaywrightTests | NUnit + Playwright | Wenige (5%) | Langsam (5-30s) |

## Unit Tests (MyApp.BL.Tests)

### Zweck
Testen die Business-Logik isoliert — ohne Web-Server, ohne Datenbank, ohne Browser.

### Verzeichnisstruktur
```
Source/MyApp.BL.Tests/
├── MyApp.BL.Tests.csproj
├── Operations/                     # Spiegelt BL/Operations/
│   ├── PricingOperationsTests.cs
│   ├── ValidationOperationsTests.cs
│   └── TransformOperationsTests.cs
├── Integrations/                   # Spiegelt BL/Integrations/
│   ├── OrderIntegrationTests.cs
│   └── UserIntegrationTests.cs
└── TestHelpers/
    └── TestDataBuilder.cs
```

### Operations testen (kein Mock noetig)

```csharp
using NUnit.Framework;
using MyApp.BL.Operations;

namespace MyApp.BL.Tests.Operations;

[TestFixture]
public class PricingOperationsTests
{
    [Test]
    public void CalculateDiscount_QuantityUnder10_ReturnsZero()
    {
        var result = PricingOperations.CalculateDiscount(100m, 5);

        Assert.That(result, Is.EqualTo(0m));
    }

    [TestCase(10, 5.0)]
    [TestCase(50, 10.0)]
    [TestCase(100, 15.0)]
    public void CalculateDiscount_VariousQuantities_ReturnsExpectedPercentage(
        int quantity, decimal expectedDiscount)
    {
        var result = PricingOperations.CalculateDiscount(100m, quantity);

        Assert.That(result, Is.EqualTo(expectedDiscount));
    }

    [Test]
    public void CreateSummary_WithItems_CalculatesCorrectTotal()
    {
        var items = new List<OrderItem>
        {
            new("Widget", 10m, 5),
            new("Gadget", 20m, 3)
        };

        var summary = PricingOperations.CreateSummary(items, discount: 5m, taxRate: 0.19m);

        Assert.That(summary.Subtotal, Is.EqualTo(110m));
        Assert.That(summary.Discount, Is.EqualTo(5m));
        Assert.That(summary.Tax, Is.EqualTo(19.95m));
        Assert.That(summary.Total, Is.EqualTo(124.95m));
    }

    [Test]
    public void ValidateOrderItem_ValidInput_ReturnsSuccess()
    {
        var result = PricingOperations.ValidateOrderItem("Widget", 10m, 1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Name, Is.EqualTo("Widget"));
    }

    [Test]
    public void ValidateOrderItem_NegativePrice_ReturnsFailure()
    {
        var result = PricingOperations.ValidateOrderItem("Widget", -5m, 1);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Does.Contain("Preis"));
    }
}
```

### Integrations testen (mit NSubstitute Mocks)

```csharp
using NUnit.Framework;
using NSubstitute;
using MyApp.BL.Integrations;
using MyApp.BL.Interfaces;

namespace MyApp.BL.Tests.Integrations;

[TestFixture]
public class OrderIntegrationTests
{
    private OrderIntegration _sut = null!;
    private IOrderRepository _repository = null!;
    private ITaxService _taxService = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IOrderRepository>();
        _taxService = Substitute.For<ITaxService>();
        _sut = new OrderIntegration(_repository, _taxService);
    }

    [Test]
    public async Task ProcessOrderAsync_ValidOrder_ReturnsSummaryWithDiscount()
    {
        _repository.GetItemsAsync(1).Returns(new List<OrderItem>
        {
            new("Widget", 10m, 100)
        });
        _taxService.GetCurrentRateAsync().Returns(0.19m);

        var result = await _sut.ProcessOrderAsync(1);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value!.Discount, Is.GreaterThan(0));
    }

    [Test]
    public async Task ProcessOrderAsync_ValidOrder_SavesSummaryToRepository()
    {
        _repository.GetItemsAsync(1).Returns(new List<OrderItem>
        {
            new("Widget", 10m, 1)
        });
        _taxService.GetCurrentRateAsync().Returns(0.19m);

        await _sut.ProcessOrderAsync(1);

        await _repository.Received(1).SaveSummaryAsync(1, Arg.Any<OrderSummary>());
    }
}
```

### Test Data Builder

```csharp
// Source/MyApp.BL.Tests/TestHelpers/TestDataBuilder.cs
public static class TestDataBuilder
{
    public static List<OrderItem> CreateOrderItems(int count = 3, decimal price = 10m)
    {
        return Enumerable.Range(1, count)
            .Select(i => new OrderItem($"Item {i}", price, i))
            .ToList();
    }
}
```

## Integration Tests (MyApp.BL.IntegrationTests)

### Zweck
Testen die gesamte Anwendung inklusive Middleware, DI, Routing und Datenbank via `WebApplicationFactory`.

### Verzeichnisstruktur
```
Source/MyApp.BL.IntegrationTests/
├── MyApp.BL.IntegrationTests.csproj
├── Infrastructure/
│   ├── CustomWebApplicationFactory.cs
│   ├── IntegrationTestBase.cs
│   └── TestAuthHandler.cs
├── Pages/
│   ├── HomePageTests.cs
│   └── CounterPageTests.cs
└── Api/
    └── WeatherEndpointTests.cs
```

### CustomWebApplicationFactory

```csharp
// Source/MyApp.BL.IntegrationTests/Infrastructure/CustomWebApplicationFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp.BL.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Produktions-DB durch Test-DB ersetzen (falls EF Core verwendet)
            // var descriptor = services.SingleOrDefault(
            //     d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            // if (descriptor != null) services.Remove(descriptor);
            // services.AddDbContext<AppDbContext>(options =>
            //     options.UseSqlite("DataSource=:memory:"));
        });

        builder.UseEnvironment("Development");
    }
}
```

### IntegrationTestBase

```csharp
// Source/MyApp.BL.IntegrationTests/Infrastructure/IntegrationTestBase.cs
using NUnit.Framework;

namespace MyApp.BL.IntegrationTests.Infrastructure;

public class IntegrationTestBase
{
    protected CustomWebApplicationFactory Factory = null!;
    protected HttpClient Client = null!;

    [SetUp]
    public void BaseSetUp()
    {
        Factory = new CustomWebApplicationFactory();
        Client = Factory.CreateClient();
    }

    [TearDown]
    public void BaseTearDown()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}
```

### TestAuthHandler (fuer authentifizierte Endpunkte)

```csharp
// Source/MyApp.BL.IntegrationTests/Infrastructure/TestAuthHandler.cs
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyApp.BL.IntegrationTests.Infrastructure;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

### Beispiel: Seiten-Test

```csharp
// Source/MyApp.BL.IntegrationTests/Pages/HomePageTests.cs
using System.Net;
using NUnit.Framework;
using MyApp.BL.IntegrationTests.Infrastructure;

namespace MyApp.BL.IntegrationTests.Pages;

[TestFixture]
public class HomePageTests : IntegrationTestBase
{
    [Test]
    public async Task Get_HomePage_ReturnsSuccessAndHtml()
    {
        var response = await Client.GetAsync("/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(
            response.Content.Headers.ContentType?.MediaType,
            Is.EqualTo("text/html"));
    }

    [Test]
    public async Task Get_HomePage_ContainsExpectedTitle()
    {
        var response = await Client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.That(content, Does.Contain("<title>"));
    }
}
```

### Beispiel: API-Test

```csharp
// Source/MyApp.BL.IntegrationTests/Api/WeatherEndpointTests.cs
using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using MyApp.BL.IntegrationTests.Infrastructure;
using MyApp.BL.Models;

namespace MyApp.BL.IntegrationTests.Api;

[TestFixture]
public class WeatherEndpointTests : IntegrationTestBase
{
    [Test]
    public async Task Get_WeatherApi_ReturnsForecasts()
    {
        var response = await Client.GetAsync("/api/weather");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var forecasts = await response.Content
            .ReadFromJsonAsync<List<WeatherForecast>>();
        Assert.That(forecasts, Is.Not.Null);
        Assert.That(forecasts, Has.Count.GreaterThan(0));
    }
}
```

## Playwright E2E Tests (MyApp.PlaywrightTests)

### Zweck
Testen kritische User-Journeys im echten Browser. Nur fuer Flows die sich nicht durch Unit/Integration Tests abdecken lassen.

### Verzeichnisstruktur
```
Source/MyApp.PlaywrightTests/
├── MyApp.PlaywrightTests.csproj
├── .runsettings
├── Infrastructure/
│   ├── PlaywrightTestBase.cs
│   └── CustomWebApplicationFactory.cs
├── PageObjects/
│   ├── HomePage.cs
│   └── CounterPage.cs
└── Tests/
    ├── HomePageE2ETests.cs
    └── CounterE2ETests.cs
```

### .runsettings

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <NUnit>
    <NumberOfTestWorkers>4</NumberOfTestWorkers>
  </NUnit>
  <Playwright>
    <BrowserName>chromium</BrowserName>
    <ExpectTimeout>10000</ExpectTimeout>
    <LaunchOptions>
      <Headless>true</Headless>
    </LaunchOptions>
  </Playwright>
</RunSettings>
```

### CustomWebApplicationFactory (mit echtem Kestrel)

```csharp
// Source/MyApp.PlaywrightTests/Infrastructure/CustomWebApplicationFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyApp.PlaywrightTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private IHost? _host;

    public string ServerAddress
    {
        get
        {
            EnsureServer();
            return ClientOptions.BaseAddress.ToString();
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder =>
            webHostBuilder.UseKestrel());

        _host = builder.Build();
        _host.Start();

        var server = _host.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        ClientOptions.BaseAddress = addresses!.Addresses
            .Select(x => new Uri(x)).Last();

        testHost.Start();
        return testHost;
    }

    private void EnsureServer()
    {
        if (_host is null)
        {
            using var _ = CreateDefaultClient();
        }
    }

    protected override void Dispose(bool disposing)
    {
        _host?.Dispose();
        base.Dispose(disposing);
    }
}
```

### PlaywrightTestBase

```csharp
// Source/MyApp.PlaywrightTests/Infrastructure/PlaywrightTestBase.cs
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace MyApp.PlaywrightTests.Infrastructure;

public class PlaywrightTestBase : PageTest
{
    protected CustomWebApplicationFactory Factory = null!;
    protected string BaseUrl = null!;

    [SetUp]
    public void BaseSetUp()
    {
        Factory = new CustomWebApplicationFactory();
        BaseUrl = Factory.ServerAddress.TrimEnd('/');
    }

    [TearDown]
    public void BaseTearDown()
    {
        Factory.Dispose();
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            Locale = "de-DE",
            ViewportSize = new() { Width = 1280, Height = 720 }
        };
    }
}
```

### Page Object Model

```csharp
// Source/MyApp.PlaywrightTests/PageObjects/CounterPage.cs
using Microsoft.Playwright;

namespace MyApp.PlaywrightTests.PageObjects;

public class CounterPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;

    public CounterPage(IPage page, string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }

    public async Task NavigateAsync()
        => await _page.GotoAsync($"{_baseUrl}/counter");

    public async Task ClickIncrementAsync()
        => await _page.GetByRole(AriaRole.Button, new() { Name = "Click me" })
            .ClickAsync();

    public async Task ExpectCountAsync(int expectedCount)
        => await Expect(_page.GetByText($"Current count: {expectedCount}"))
            .ToBeVisibleAsync();

    private ILocatorAssertions Expect(ILocator locator)
        => Microsoft.Playwright.Assertions.Expect(locator);
}
```

### Beispiel: E2E Test

```csharp
// Source/MyApp.PlaywrightTests/Tests/CounterE2ETests.cs
using NUnit.Framework;
using MyApp.PlaywrightTests.Infrastructure;
using MyApp.PlaywrightTests.PageObjects;

namespace MyApp.PlaywrightTests.Tests;

[TestFixture]
public class CounterE2ETests : PlaywrightTestBase
{
    [Test]
    public async Task Counter_ClickIncrement_IncrementsCount()
    {
        var counterPage = new CounterPage(Page, BaseUrl);

        await counterPage.NavigateAsync();
        await counterPage.ExpectCountAsync(0);

        await counterPage.ClickIncrementAsync();
        await counterPage.ExpectCountAsync(1);

        await counterPage.ClickIncrementAsync();
        await counterPage.ExpectCountAsync(2);
    }
}
```

## Playwright Smoke-Tests (Seitenerreichbarkeit)

### Zweck
Ergaenzung der E2E-Ebene in der Breite: jede `@page`-Route wird einmal aufgerufen und auf drei Dinge geprueft — erreichbar, kein ASP.NET-/Blazor-Fehler (Developer Exception Page, Error Boundary, HTTP-Fehler), ein bis zwei Schluesselelemente vorhanden. Die Ausgabe traegt Marker (`[PAGE_OK]`, `[PAGE_ERROR]`, `[SUMMARY]`), damit ein KI-Assistent nach Codeaenderungen selbst pruefen und Fehler analysieren kann. Anders als `MyApp.PlaywrightTests` startet dieses Projekt keine eigene Instanz, sondern verbindet sich per Chrome DevTools Protocol mit einem laufenden, eingeloggten Chrome — so lassen sich auch Seiten hinter Login und 2FA pruefen.

### Vorlagen
Die Templates liegen unter [templates/playwright-smoke/](./templates/playwright-smoke/README.md) — Projektdatei, `PlaywrightTestBase`, `AspNetErrorParser`, Sammeltest, Start-Scripts (`.sh`/`.ps1`) und Projekt-README mit Platzhaltern. Die dortige README fuehrt die **Versionstabelle** (NUnit-Trias nach `commands/upgrade/nunit.md`, Playwright, Chrome 136+) und die Grundsaetze (keine festen Wartezeiten, `Inconclusive` statt Gruen ohne Nachweis, Tab-Management, Auth-Redirect-Erkennung). Instanziiert wird per `/erstelle-blazor-playwright-tests`.

### Verzeichnisstruktur (instanziiert)
```
Source/MyApp.Web.PlaywrightTests/
├── MyApp.Web.PlaywrightTests.csproj
├── PlaywrightTestBase.cs
├── README.md
├── start-chrome-debug.sh
├── start-chrome-debug.ps1
├── Helpers/
│   └── AspNetErrorParser.cs
└── PageTests/
    └── AlleSeiten_SmokeTests.cs        # AllPages-Tabelle, Sammeltest + parametrisierte Einzeltests
```

### Ausfuehren
```bash
./start-chrome-debug.sh                      # Chrome mit Remote Debugging + Test-Profil; dann einloggen
dotnet test Source/MyApp.Web.PlaywrightTests/ --logger "console;verbosity=detailed"
dotnet test Source/MyApp.Web.PlaywrightTests/ --filter "Category=SmokeTest"   # nur der Sammeltest
```

## Tests ausfuehren

```bash
# Alle Tests
dotnet test

# Nur Unit Tests
dotnet test Source/MyApp.BL.Tests/

# Nur Integration Tests
dotnet test Source/MyApp.BL.IntegrationTests/

# Nur E2E Tests
dotnet test Source/MyApp.PlaywrightTests/ -- NUnit.NumberOfTestWorkers=1

# Mit Coverage-Report
dotnet test Source/MyApp.BL.Tests/ --collect:"XPlat Code Coverage"
dotnet test Source/MyApp.BL.IntegrationTests/ --collect:"XPlat Code Coverage"

# Coverage-Report generieren (reportgenerator installieren)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport"
```

## Coverage-Ziele

| Projekt | Ziel | Fokus |
|---|---|---|
| MyApp.BL (Operations) | 90%+ | Alle Geschaeftslogik-Pfade |
| MyApp.BL (Integrations) | 80%+ | Orchestrierungs-Pfade |
| MyApp.Web (Pages) | — | Via E2E abgedeckt, nicht direkt gemessen |
| Gesamt | 80%+ | Gewichteter Durchschnitt |

## SAST (Static Application Security Testing)

SAST ist eine uebergreifende Schicht die unabhaengig von der Test-Pyramide laeuft.

### Stufenplan

1. **Built-in Analysatoren** — Roslyn Analyzers im Build-Prozess (sofort)
2. **CodeQL** — GitHub-native Analyse bei jedem PR (Woche 1)
3. **Vulnerability Scanning** — NuGet-Paket-Pruefung im CI (sofort)
4. **Custom Rules** — Projektspezifische Regeln bei Bedarf (spaeter)

### 1. Roslyn Analyzers (Build-Zeit)

In jedem `.csproj` oder in `Directory.Build.props`:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="10.*">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

### 2. CodeQL (GitHub Actions)

```yaml
# .github/workflows/codeql.yml
name: CodeQL

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 6 * * 1'  # Montags 06:00 UTC

jobs:
  analyze:
    runs-on: ubuntu-latest
    permissions:
      security-events: write

    steps:
      - uses: actions/checkout@v4

      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: csharp

      - name: Autobuild
        uses: github/codeql-action/autobuild@v3

      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@v3
```

### 3. Vulnerability Scanning

```bash
# Lokale Pruefung
dotnet list package --vulnerable --include-transitive

# Im CI (ci.yml) — siehe 06-build-deployment.md
```

## Dos and Don'ts

### Do
- Operations-Tests immer ohne Mocks schreiben
- Integration Tests mit `WebApplicationFactory` fuer HTTP-Endpunkte
- Page Object Model fuer Playwright Tests
- Locators statt CSS-Selektoren in Playwright (`GetByRole`, `GetByText`, `GetByLabel`)
- `Expect()` Assertions in Playwright (automatisches Warten)

### Don't
- Keine E2E Tests fuer Logik die per Unit Test abdeckbar ist
- Keine `Thread.Sleep()` oder `Task.Delay()` in Tests — Playwright wartet automatisch
- Keine Singletons in Test-Factories (jeder Test bekommt eigene Factory)
- Keine fragilen CSS-Selektoren in Playwright
