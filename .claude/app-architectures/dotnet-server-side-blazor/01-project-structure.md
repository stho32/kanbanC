# Project Structure

## Solution Layout

```
MyApp/
├── MyApp.sln
├── .gitignore
├── .editorconfig                               # Code-Formatierung und Style-Regeln
├── CLAUDE.md
├── README.md
├── Anforderungen/                              # Anforderungsdokumente (RXXXXX)
│   └── README.md
├── Dokumentation/                              # Projektdokumentation
│   └── README.md
│
└── Source/                                     # Gesamter Quellcode
    ├── Directory.Build.props                   # Zentrale Build-Properties (Analyzers, TFM)
    ├── MyApp.Web/                              # Blazor Server Anwendung
    │   ├── MyApp.Web.csproj
    │   ├── Program.cs                          # Host-Konfiguration, DI, Middleware
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   ├── Components/
    │   │   ├── App.razor                       # Root-Komponente
    │   │   ├── Routes.razor                    # Routing-Konfiguration
    │   │   ├── _Imports.razor                  # Globale Using-Direktiven
    │   │   ├── Layout/
    │   │   │   ├── MainLayout.razor            # Haupt-Layout
    │   │   │   └── NavMenu.razor               # Navigation
    │   │   ├── Pages/
    │   │   │   ├── Home.razor                  # Startseite
    │   │   │   ├── Counter.razor               # Beispielseite
    │   │   │   └── Error.razor                 # Fehlerseite
    │   │   └── Shared/                         # Wiederverwendbare UI-Komponenten
    │   │       ├── ConfirmDialog.razor
    │   │       └── LoadingSpinner.razor
    │   ├── Services/                           # Web-spezifische Services (Integration-Schicht)
    │   │   └── WeatherService.cs               # Beispiel: ruft BL-Operationen auf
    │   └── wwwroot/
    │       ├── css/
    │       ├── js/
    │       └── favicon.ico
    │
    ├── MyApp.BL/                               # Business Logic Bibliothek
    │   ├── MyApp.BL.csproj
    │   ├── Models/                             # Domain-Modelle und DTOs
    │   │   ├── WeatherForecast.cs
    │   │   └── UserProfile.cs
    │   ├── Operations/                         # IOSP: Reine Logik-Methoden (keine Aufrufe)
    │   │   ├── WeatherOperations.cs
    │   │   └── ValidationOperations.cs
    │   ├── Integrations/                       # IOSP: Orchestrierung (nur Aufrufe, keine Logik)
    │   │   ├── WeatherIntegration.cs
    │   │   └── UserIntegration.cs
    │   ├── Interfaces/                         # Abstrakte Schnittstellen
    │   │   ├── IWeatherRepository.cs
    │   │   └── IUserRepository.cs
    │   └── Extensions/                         # Erweiterungsmethoden
    │       └── ServiceCollectionExtensions.cs
    │
    ├── MyApp.BL.Tests/                         # Unit Tests (nur BL, keine Web-Abhaengigkeit)
    │   ├── MyApp.BL.Tests.csproj
    │   ├── Operations/                         # Tests fuer Operations (reine Logik)
    │   │   ├── WeatherOperationsTests.cs
    │   │   └── ValidationOperationsTests.cs
    │   ├── Integrations/                       # Tests fuer Integrations (mit Mocks)
    │   │   └── WeatherIntegrationTests.cs
    │   └── TestHelpers/
    │       └── TestDataBuilder.cs
    │
    ├── MyApp.BL.IntegrationTests/              # Integration Tests (mit WebApplicationFactory)
    │   ├── MyApp.BL.IntegrationTests.csproj
    │   ├── Infrastructure/
    │   │   ├── CustomWebApplicationFactory.cs
    │   │   ├── IntegrationTestBase.cs
    │   │   └── TestAuthHandler.cs
    │   ├── Pages/
    │   │   ├── HomePageTests.cs
    │   │   └── CounterPageTests.cs
    │   └── Api/
    │       └── WeatherEndpointTests.cs
    │
    └── MyApp.PlaywrightTests/                  # E2E Tests (Browser-Automation)
        ├── MyApp.PlaywrightTests.csproj
        ├── .runsettings
        ├── Infrastructure/
        │   ├── PlaywrightTestBase.cs
        │   └── CustomWebApplicationFactory.cs
        ├── PageObjects/                        # Page Object Model
        │   ├── HomePage.cs
        │   └── CounterPage.cs
        └── Tests/
            ├── HomePageE2ETests.cs
            └── CounterE2ETests.cs

.github/
└── workflows/
    ├── ci.yml                                  # Build + Test bei jedem Push/PR
    ├── codeql.yml                              # SAST — CodeQL Sicherheitsanalyse
    └── release.yml                             # Release-Workflow

docker/
├── Dockerfile
└── docker-compose.yml
```

## Projekt-Abhaengigkeiten

```
MyApp.Web ──────────► MyApp.BL
     │
     │  (Referenziert BL fuer Models, Operations, Interfaces)
     │
MyApp.BL.Tests ─────► MyApp.BL
     │
     │  (Testet BL isoliert, keine Web-Abhaengigkeit)
     │
MyApp.BL.IntegrationTests ──► MyApp.Web ──► MyApp.BL
     │
     │  (Testet die gesamte Anwendung via WebApplicationFactory)
     │
MyApp.PlaywrightTests ──► MyApp.Web ──► MyApp.BL
     │
     │  (Testet via echtem Browser gegen laufende Anwendung)
```

## Projekt-Dateien (.csproj)

### MyApp.Web.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\MyApp.BL\MyApp.BL.csproj" />
  </ItemGroup>
</Project>
```

### MyApp.BL.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

Paketversionen der Testprojekte sind fest (kein Wildcard) und folgen `~/.claude/commands/upgrade/nunit.md` bzw. `~/.claude/app-architectures/Common/snippets/nunit4-testprojekt.csproj.md` (NUnit 4.4.0, NUnit3TestAdapter 6.0.0, Microsoft.NET.Test.Sdk 18.0.1; Playwright 1.62.0 wie `templates/playwright-smoke/`). Stand Dezember 2025 — bei Ausfuehrung auf NuGet.org pruefen; `/upgrade nunit` hebt sie.

### MyApp.BL.Tests.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="NUnit" Version="4.4.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.0.0" />
    <PackageReference Include="NSubstitute" Version="5.3.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\MyApp.BL\MyApp.BL.csproj" />
  </ItemGroup>
</Project>
```

### MyApp.BL.IntegrationTests.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="NUnit" Version="4.4.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.0.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\MyApp.Web\MyApp.Web.csproj" />
  </ItemGroup>
</Project>
```

### MyApp.PlaywrightTests.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="Microsoft.Playwright.NUnit" Version="1.62.0" />
    <PackageReference Include="NUnit" Version="4.4.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\MyApp.Web\MyApp.Web.csproj" />
  </ItemGroup>
</Project>
```

## Namenskonventionen

| Element | Konvention | Beispiel |
|---|---|---|
| Solution | PascalCase | `MyApp.sln` |
| Projekte | PascalCase mit Punkt-Trennung | `MyApp.BL`, `MyApp.Web` |
| Namespaces | Projekt-basiert | `MyApp.BL.Operations` |
| Klassen | PascalCase | `WeatherOperations` |
| Interfaces | I-Prefix + PascalCase | `IWeatherRepository` |
| Methoden | PascalCase | `CalculateAverage` |
| Private Felder | _camelCase | `_weatherService` |
| Lokale Variablen | camelCase | `forecasts` |
| Razor-Komponenten | PascalCase | `ConfirmDialog.razor` |
| Razor-Seiten | PascalCase | `Home.razor` |
| Test-Klassen | `[Klasse]Tests` | `WeatherOperationsTests` |
| Test-Methoden | `[Method]_[Szenario]_[Erwartung]` | `Calculate_NegativeInput_ThrowsException` |

## Verzeichnis-Zwecke

| Verzeichnis | Zweck |
|---|---|
| `Source/MyApp.Web/Components/Pages/` | Routable Blazor-Seiten (`@page "/..."`) |
| `Source/MyApp.Web/Components/Shared/` | Wiederverwendbare UI-Komponenten ohne Route |
| `Source/MyApp.Web/Components/Layout/` | Layout-Komponenten (MainLayout, NavMenu) |
| `Source/MyApp.Web/Services/` | Web-spezifische Integration-Services (rufen BL auf) |
| `Source/MyApp.BL/Models/` | Domain-Modelle, DTOs, Value Objects |
| `Source/MyApp.BL/Operations/` | IOSP-Operations: reine Logik, keine Abhaengigkeiten |
| `Source/MyApp.BL/Integrations/` | IOSP-Integrations: Orchestrierung, keine eigene Logik |
| `Source/MyApp.BL/Interfaces/` | Abstrakte Schnittstellen fuer Repositories und Services |
| `Source/MyApp.BL.Tests/` | Unit Tests — testen BL isoliert |
| `Source/MyApp.BL.IntegrationTests/` | Integration Tests — testen Web+BL zusammen |
| `Source/MyApp.PlaywrightTests/` | E2E Tests — testen im echten Browser |
| `Anforderungen/` | Anforderungsdokumente (RXXXXX-Format) |
| `Dokumentation/` | Projektdokumentation, Architektur-Entscheidungen, Guides |
