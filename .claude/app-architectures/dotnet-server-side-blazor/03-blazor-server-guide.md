# Blazor Server Guide

## Funktionsweise

Blazor Server rendert UI-Komponenten auf dem Server. Der Browser empfaengt nur DOM-Diffs ueber eine persistente **SignalR-WebSocket-Verbindung** (den "Circuit"). Jeder Nutzer haelt eine eigene Verbindung mit eigenem DI-Scope.

```
Browser                              Server
┌─────────┐    SignalR/WebSocket    ┌──────────────┐
│  DOM    │◄──────────────────────►│   Circuit     │
│  Events │    (DOM-Diffs, Events)  │   (DI Scope)  │
│  JS     │                        │   Components  │
└─────────┘                        └──────────────┘
```

**Vorteile:**
- Kein WASM-Download, sofortige Ladezeit
- Voller Zugriff auf Server-Ressourcen (DB, Dateisystem, interne APIs)
- Duenne Clients — Logik bleibt auf dem Server

**Einschraenkungen:**
- Jeder Nutzer benoetigt eine offene WebSocket-Verbindung
- Latenz bei jeder Interaktion (Roundtrip zum Server)
- Sticky Sessions bei mehreren Servern erforderlich

## Komponenten-Lifecycle

Lifecycle-Methoden werden in dieser Reihenfolge aufgerufen:

```
1. SetParametersAsync          ← Parameter von Parent oder Route setzen
2. OnInitialized[Async]        ← Einmalig beim ersten Render
3. OnParametersSet[Async]      ← Nach OnInitialized + bei jedem Parameter-Update
4. ShouldRender                ← Soll die Komponente neu rendern? (nicht beim ersten Render)
5. OnAfterRender[Async]        ← Nach dem Render — hier JS Interop ausfuehren
6. Dispose/DisposeAsync        ← Aufraeumen bei Entfernung aus dem UI
```

**Wichtig bei Prerendering:** `OnInitialized[Async]` wird **zweimal** aufgerufen — einmal beim statischen Prerender, einmal beim Verbindungsaufbau. Teure Operationen mit `[PersistentState]` (.NET 10) oder einem Flag schuetzen:

```csharp
@code {
    [PersistentState]
    public List<WeatherForecast>? Forecasts { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Wird nur geladen wenn nicht bereits aus Prerender vorhanden
        Forecasts ??= await WeatherService.GetForecastsAsync();
    }
}
```

## Komponenten-Typen

### Seiten (Pages)

Routable Komponenten mit `@page`-Direktive. Leben in `Source/MyApp.Web/Components/Pages/`.

```csharp
// Source/MyApp.Web/Components/Pages/Weather.razor
@page "/weather"
@page "/weather/{City}"
@inject IWeatherService WeatherService
@rendermode InteractiveServer

<PageTitle>Wetter</PageTitle>

<h1>Wetter fuer @City</h1>

@if (_loading)
{
    <LoadingSpinner />
}
else if (_forecasts is not null)
{
    <WeatherTable Forecasts="_forecasts" />
}

@code {
    [Parameter] public string City { get; set; } = "Berlin";

    private List<WeatherForecast>? _forecasts;
    private bool _loading = true;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _forecasts = await WeatherService.GetForecastsAsync(City);
        _loading = false;
    }
}
```

### Shared-Komponenten

Wiederverwendbare UI-Bausteine ohne Route. Leben in `Source/MyApp.Web/Components/Shared/`.

```csharp
// Source/MyApp.Web/Components/Shared/ConfirmDialog.razor
<div class="modal @(_visible ? "show" : "")" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5>@Title</h5>
            </div>
            <div class="modal-body">
                @ChildContent
            </div>
            <div class="modal-footer">
                <button class="btn btn-secondary" @onclick="Cancel">Abbrechen</button>
                <button class="btn btn-danger" @onclick="Confirm">@ConfirmText</button>
            </div>
        </div>
    </div>
</div>

@code {
    [Parameter] public string Title { get; set; } = "Bestaetigung";
    [Parameter] public string ConfirmText { get; set; } = "OK";
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private bool _visible;

    public void Show() { _visible = true; StateHasChanged(); }
    public void Hide() { _visible = false; StateHasChanged(); }

    private async Task Confirm() { Hide(); await OnConfirm.InvokeAsync(); }
    private async Task Cancel() { Hide(); await OnCancel.InvokeAsync(); }
}
```

### Layout-Komponenten

Definieren das Seitenlayout. Leben in `Source/MyApp.Web/Components/Layout/`.

```csharp
// Source/MyApp.Web/Components/Layout/MainLayout.razor
@inherits LayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>
    <main>
        <article class="content px-4">
            @Body
        </article>
    </main>
</div>
```

## State Management

### Empfohlene Ansaetze

| Ansatz | Scope | Verwendung |
|---|---|---|
| Komponent-Parameter | Parent → Child | Daten an Kind-Komponenten weitergeben |
| `EventCallback` | Child → Parent | Events an Eltern-Komponenten melden |
| Cascading Values | Ancestor → Descendants | Theme, Auth-State, App-weite Konfiguration |
| Scoped State Container | Circuit (Session) | Cross-Komponenten-State innerhalb einer Session |
| `[PersistentState]` (.NET 10) | Prerender → Interactive | State ueber Prerendering-Transition bewahren |
| URL-Parameter | Navigation | Filterung, Paginierung, Entity-IDs |

### Scoped State Container Pattern

```csharp
// Source/MyApp.Web/Services/AppState.cs
public class AppState
{
    private string? _currentTheme = "light";

    public string CurrentTheme
    {
        get => _currentTheme ?? "light";
        set { _currentTheme = value; NotifyStateChanged(); }
    }

    public event Action? OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

Registrierung als Scoped Service:
```csharp
builder.Services.AddScoped<AppState>();
```

Nutzung in Komponenten:
```csharp
@inject AppState AppState
@implements IDisposable

<div class="theme-@AppState.CurrentTheme">
    @ChildContent
</div>

@code {
    protected override void OnInitialized()
        => AppState.OnChange += StateHasChanged;

    public void Dispose()
        => AppState.OnChange -= StateHasChanged;
}
```

## SignalR-Konfiguration

```csharp
// Source/MyApp.Web/Program.cs
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Optionale Circuit-Konfiguration
builder.Services.AddServerSideBlazor(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
});

// Hub-Optionen (z.B. maximale Nachrichtengroesse)
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 64 * 1024; // 64 KB statt 32 KB Default
    });
```

**Timeouts:**
- Server Timeout: 30s (Default) — wie lange der Server auf Pings wartet
- Keep-Alive Interval: 15s (Default) — wie oft der Server Pings sendet
- Regel: Server Timeout >= 2× Keep-Alive Interval

## Reconnection (.NET 10)

.NET 10 liefert eine anpassbare `ReconnectModal`-Komponente mit:

```javascript
// Programmatische Circuit-Kontrolle
Blazor.pauseCircuit();   // Circuit pausieren (z.B. Tab-Wechsel)
Blazor.resumeCircuit();  // Circuit fortsetzen
```

## Formulare und Validierung

```csharp
@page "/profile"
@inject UserIntegration UserIntegration

<EditForm Model="_model" OnValidSubmit="HandleSubmit" FormName="profile">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="mb-3">
        <label for="name">Name</label>
        <InputText id="name" @bind-Value="_model.Name" class="form-control" />
        <ValidationMessage For="() => _model.Name" />
    </div>

    <div class="mb-3">
        <label for="email">E-Mail</label>
        <InputText id="email" @bind-Value="_model.Email" class="form-control" />
        <ValidationMessage For="() => _model.Email" />
    </div>

    <button type="submit" class="btn btn-primary">Speichern</button>
</EditForm>

@code {
    private ProfileModel _model = new();

    private async Task HandleSubmit()
    {
        await UserIntegration.SaveProfileAsync(_model);
    }
}
```

## JS Interop

JavaScript-Aufrufe nur in `OnAfterRender[Async]` — nicht frueher, da das DOM noch nicht existiert:

```csharp
@inject IJSRuntime JS

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initializeChart", _chartElement);
        }
    }
}
```

## Security

- **`[Authorize]`** nur auf `@page`-Komponenten anwenden (nicht auf Child-Komponenten)
- **`<AuthorizeView>`** steuert UI-Sichtbarkeit — ersetzt **nicht** Server-seitige Autorisierung
- **Antiforgery** ist ab .NET 8 automatisch aktiviert (`AddRazorComponents()`)
- **Niemals** Secrets im Client speichern — alles ueber Server-seitige Services
- Scoped DI = ein Container pro Circuit — **keine** nutzerspezifischen Daten in Singletons

```csharp
// Source/MyApp.Web/Program.cs
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery(); // Nach UseAuthentication und UseAuthorization
```

## Performance Best Practices

- **`<Virtualize>`** fuer grosse Listen — rendert nur sichtbare Elemente
- **`ShouldRender`** ueberschreiben um unnoetige Re-Renders zu vermeiden
- **`@key`** Direktive fuer effizientes Diffing in Listen
- **`[StreamRendering]`** fuer Komponenten mit langsamem async Init
- **`StateHasChanged()`** sparsam einsetzen — Blazor ruft es nach Event-Handlern automatisch auf
- **`IDisposable`** implementieren: Timer, Event-Subscriptions, externe Ressourcen aufraeumen

## Offizielle Dokumentation

- [Blazor Server Fundamentals](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/?view=aspnetcore-10.0)
- [Blazor Components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/?view=aspnetcore-10.0)
- [Blazor Forms](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/?view=aspnetcore-10.0)
- [Blazor JS Interop](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0)
- [SignalR Configuration](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr?view=aspnetcore-10.0)
- [ASP.NET Core 10.0 Release Notes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0)
