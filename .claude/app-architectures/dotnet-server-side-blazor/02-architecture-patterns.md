# Architecture Patterns

## Architektur-Ueberblick

Die Architektur kombiniert zwei Kernprinzipien:

1. **Schichten-Trennung**: Web-Schicht (Blazor) und Business-Logic-Schicht (.BL) als separate Projekte
2. **IOSP**: Innerhalb jeder Schicht strikte Trennung von Integration- und Operation-Methoden

```
┌─────────────────────────────────────────────────┐
│                  MyApp.Web                       │
│  ┌───────────┐  ┌──────────┐  ┌──────────────┐ │
│  │   Pages   │  │  Shared  │  │   Services   │ │
│  │  (Razor)  │  │Components│  │ (Integration)│ │
│  └─────┬─────┘  └────┬─────┘  └──────┬───────┘ │
│        │              │               │         │
│        └──────────────┴───────────────┘         │
│                       │                         │
│              Dependency Injection               │
└───────────────────────┬─────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────┐
│                  MyApp.BL                        │
│  ┌──────────────┐  ┌──────────┐  ┌───────────┐ │
│  │ Integrations │  │Operations│  │   Models   │ │
│  │(Orchestriert)│  │(Reine    │  │   DTOs     │ │
│  │              │─►│ Logik)   │  │            │ │
│  └──────┬───────┘  └──────────┘  └───────────┘ │
│         │                                       │
│  ┌──────┴───────┐                               │
│  │  Interfaces  │  (Abstrakte Abhaengigkeiten)  │
│  └──────────────┘                               │
└─────────────────────────────────────────────────┘
```

## Datenfluss

```
Browser (SignalR/WebSocket)
    │
    ▼
Blazor Page (.razor)          ← UI-Events empfangen
    │
    ▼
Web Service (Integration)     ← Orchestriert BL-Aufrufe
    │
    ▼
BL Integration                ← Orchestriert Operations
    │
    ▼
BL Operation                  ← Reine Logik, berechnet Ergebnis
    │
    ▼
Ergebnis zurueck an Page      ← StateHasChanged() triggert Re-Render
    │
    ▼
Browser (DOM-Update via SignalR)
```

## Schichten und Verantwortlichkeiten

### MyApp.Web — Praesentationsschicht

**Verantwortung:** UI-Darstellung, Benutzer-Interaktion, DI-Konfiguration, Routing

**Enthaelt:**
- Razor-Komponenten (Pages, Shared, Layout)
- Web-spezifische Services (Integration-Schicht zwischen UI und BL)
- `Program.cs` mit Host-Konfiguration und Middleware-Pipeline
- Statische Assets (CSS, JS, Bilder)

**Regeln:**
- Pages enthalten **keine Geschaeftslogik** — sie delegieren an Services
- Pages sind duenn: Event-Handler rufen Service-Methoden auf, binden Ergebnisse an UI
- Web Services sind **Integrations** (nur Aufrufe, keine Logik)

```csharp
// Source/MyApp.Web/Components/Pages/Weather.razor
@page "/weather"
@inject IWeatherService WeatherService

<h1>Wetter</h1>

@if (_forecasts is null)
{
    <LoadingSpinner />
}
else
{
    <table>
        @foreach (var forecast in _forecasts)
        {
            <tr>
                <td>@forecast.Date.ToShortDateString()</td>
                <td>@forecast.TemperatureC &deg;C</td>
                <td>@forecast.Summary</td>
            </tr>
        }
    </table>
}

@code {
    private List<WeatherForecast>? _forecasts;

    protected override async Task OnInitializedAsync()
    {
        _forecasts = await WeatherService.GetForecastsAsync();
    }
}
```

### MyApp.BL — Business-Logic-Schicht

**Verantwortung:** Gesamte Geschaeftslogik, Validierung, Berechnungen, Datentransformationen

**Enthaelt:**
- `Models/` — Domain-Modelle, DTOs, Value Objects
- `Operations/` — IOSP-Operations (reine Logik)
- `Integrations/` — IOSP-Integrations (Orchestrierung)
- `Interfaces/` — Abstrakte Schnittstellen fuer externe Abhaengigkeiten

**Regeln:**
- **Keine Abhaengigkeit** auf MyApp.Web oder ASP.NET Core
- Operations sind **statische Methoden** oder Methoden ohne injizierte Abhaengigkeiten
- Integrations erhalten Abhaengigkeiten via Constructor Injection
- Models sind **reine Datenklassen** ohne Logik (Ausnahme: Value Object Validierung)

```csharp
// Source/MyApp.BL/Operations/WeatherOperations.cs
public static class WeatherOperations
{
    public static string ClassifyTemperature(int temperatureC)
    {
        return temperatureC switch
        {
            < 0 => "Freezing",
            < 10 => "Cold",
            < 20 => "Mild",
            < 30 => "Warm",
            _ => "Hot"
        };
    }

    public static List<WeatherForecast> FilterByMinTemperature(
        List<WeatherForecast> forecasts, int minTemp)
    {
        return forecasts.Where(f => f.TemperatureC >= minTemp).ToList();
    }
}
```

```csharp
// Source/MyApp.BL/Integrations/WeatherIntegration.cs
public class WeatherIntegration
{
    private readonly IWeatherRepository _repository;

    public WeatherIntegration(IWeatherRepository repository)
    {
        _repository = repository;
    }

    // INTEGRATION: nur Aufrufe, keine Logik
    public async Task<List<WeatherForecast>> GetClassifiedForecastsAsync(int minTemp)
    {
        var forecasts = await _repository.GetAllAsync();
        var filtered = WeatherOperations.FilterByMinTemperature(forecasts, minTemp);

        foreach (var forecast in filtered)
        {
            forecast.Summary = WeatherOperations.ClassifyTemperature(forecast.TemperatureC);
        }

        return filtered;
    }
}
```

## Dependency Injection

### Registrierung in Program.cs

```csharp
// Source/MyApp.Web/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Blazor Server Services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// BL Services registrieren
builder.Services.AddScoped<WeatherIntegration>();
builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();

// Web Services registrieren
builder.Services.AddScoped<IWeatherService, WeatherService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### DI-Richtlinien

| Lifetime | Verwendung |
|---|---|
| `Scoped` | Standard fuer alle Services und Integrations — ein Instance pro Circuit/Request |
| `Singleton` | Nur fuer thread-sichere, zustandslose Services (z.B. Konfiguration, Caching) |
| `Transient` | Nur fuer leichtgewichtige, zustandslose Hilfsklassen |

**Wichtig bei Blazor Server:** Scoped Services leben so lange wie der Circuit (die SignalR-Verbindung). Das ist laenger als bei einem normalen HTTP-Request. Singletons werden ueber alle Circuits geteilt — niemals nutzerspezifischen Zustand in Singletons speichern.

## Design Patterns

### Repository Pattern

Abstrahiert den Datenzugriff hinter Interfaces in der BL-Schicht:

```csharp
// Source/MyApp.BL/Interfaces/IWeatherRepository.cs
public interface IWeatherRepository
{
    Task<List<WeatherForecast>> GetAllAsync();
    Task<WeatherForecast?> GetByIdAsync(int id);
    Task AddAsync(WeatherForecast forecast);
    Task UpdateAsync(WeatherForecast forecast);
    Task DeleteAsync(int id);
}
```

Die Implementierung lebt im Web-Projekt oder einem separaten Infrastructure-Projekt:

```csharp
// Source/MyApp.Web/Data/WeatherRepository.cs
public class WeatherRepository : IWeatherRepository
{
    private readonly AppDbContext _context;

    public WeatherRepository(AppDbContext context) => _context = context;

    public async Task<List<WeatherForecast>> GetAllAsync()
        => await _context.Forecasts.ToListAsync();

    // ... weitere Methoden
}
```

### Service-Fassade (Web Service)

Web Services fungieren als Fassade zwischen Blazor-Komponenten und BL:

```csharp
// Source/MyApp.Web/Services/WeatherService.cs
public class WeatherService : IWeatherService
{
    private readonly WeatherIntegration _integration;

    public WeatherService(WeatherIntegration integration)
        => _integration = integration;

    // INTEGRATION: delegiert an BL
    public async Task<List<WeatherForecast>> GetForecastsAsync()
        => await _integration.GetClassifiedForecastsAsync(minTemp: 0);
}
```

### Result Pattern (statt Exceptions fuer erwartete Fehler)

```csharp
// Source/MyApp.BL/Models/Result.cs
public record Result<T>
{
    public T? Value { get; init; }
    public string? Error { get; init; }
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new() { Value = value };
    public static Result<T> Failure(string error) => new() { Error = error };
}
```

```csharp
// OPERATION: Validierung mit Result
public static Result<UserProfile> ValidateProfile(string name, string email)
{
    if (string.IsNullOrWhiteSpace(name))
        return Result<UserProfile>.Failure("Name darf nicht leer sein");

    if (!email.Contains('@'))
        return Result<UserProfile>.Failure("Ungueltige E-Mail-Adresse");

    return Result<UserProfile>.Success(new UserProfile(name, email));
}
```

## Dos and Don'ts

### Do
- BL-Projekt hat **keine Referenz** auf ASP.NET Core oder Blazor
- Operations als **statische Methoden** oder reine Instanz-Methoden ohne injizierte Abhaengigkeiten
- Interfaces in der BL-Schicht definieren, Implementierungen im Web-Projekt
- Jede Seite maximal 1-2 Service-Aufrufe im Event-Handler

### Don't
- Keine `HttpContext`-Zugriffe in der BL-Schicht
- Keine Geschaeftslogik in Razor-Komponenten
- Keine Hybrid-Methoden (Logik + Aufrufe gemischt) — siehe [IOSP Guide](./04-iosp-guide.md)
- Keine zirkulaeren Abhaengigkeiten zwischen Projekten
- Keine Singletons fuer nutzerspezifischen Zustand
