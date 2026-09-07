using KanbanC.Blazor.Components;
using KanbanC.Blazor.Services;
using KanbanC.Contracts.Ereignisse;
using KanbanC.Contracts.Karten;

var builder = WebApplication.CreateBuilder(args);

// Die Voreinstellung von MaximumReceiveMessageSize sind 32 KB je SignalR-Nachricht — schon eine
// 41-kB-Datei kaeme damit nicht durch den Kreislauf. Angehoben wird auf dieselbe Konstante, auf
// die sich Validator, WebApi-Route und Dateiwaehler beziehen.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(optionen => optionen.MaximumReceiveMessageSize = Anhangsgrenze.HoechsteDateigroesse);

// Die Oberfläche spricht ausschließlich über die WebApi mit den Daten -
// es gibt bewusst keine Projektreferenz auf KanbanC.BL.
var webApiBasisAdresse = builder.Configuration["WebApi:BasisAdresse"];
if (webApiBasisAdresse is null)
{
    throw new InvalidOperationException("WebApi:BasisAdresse fehlt in der Konfiguration.");
}

// Die Adresse, die in den href des Download-Symbols geht. Voreinstellung ist die interne
// Basisadresse: auf einem Rechner ist sie richtig, und der Einzelrechnerbetrieb funktioniert damit
// ohne Zutun. Im LAN wird sie gesetzt, weil der Browser eines zweiten Rechners „localhost" nicht
// erreicht.
// Leer heisst „nicht gesetzt": der Schluessel steht in appsettings.json, damit man ihn findet, und
// bleibt dort leer, damit der Einzelrechnerbetrieb ohne Zutun funktioniert. Stuende dort eine
// Adresse, griffe die Voreinstellung nie.
var gesetzteOeffentlicheAdresse = builder.Configuration["WebApi:OeffentlicheBasisAdresse"];
var oeffentlicheBasisAdresse = webApiBasisAdresse;
if (!string.IsNullOrWhiteSpace(gesetzteOeffentlicheAdresse))
{
    oeffentlicheBasisAdresse = gesetzteOeffentlicheAdresse;
}

builder.Services.AddSingleton(new WebApibasisadresse(oeffentlicheBasisAdresse));

// Der Weg reist im Kopf und wird an **genau einer Stelle** gesetzt: hier. Damit trägt ihn jeder
// Aufruf der Oberfläche, ohne dass ein einziger Aufrufer davon weiß — eine vergessene Aufrufstelle
// ließe die Einflugmarke lügen.
builder.Services.AddHttpClient("KanbanC", client =>
{
    client.BaseAddress = new Uri(webApiBasisAdresse);
    client.DefaultRequestHeaders.Add(Wegkopf.Name, Wegkopf.Oberflaechenwert);
});
builder.Services.AddScoped<BoardApiKlient>();
builder.Services.AddScoped<SpaltenApiKlient>();
builder.Services.AddScoped<KartenApiKlient>();
builder.Services.AddScoped<KontributorenApiKlient>();
builder.Services.AddScoped<KartenklassenApiKlient>();
builder.Services.AddScoped<ZeitenApiKlient>();
builder.Services.AddScoped<ImportApiKlient>();
builder.Services.AddScoped<AuswertungenApiKlient>();
builder.Services.AddScoped<Identitaetsspeicher>();
builder.Services.AddScoped<Laufzeitmelder>();
builder.Services.AddScoped<Pfadkopie>();

// Der Rückweg von der WebApi: **eine** Leitung je Prozess, ein Verteiler je Prozess. Die Leitung
// ist ein HostedService und kein Kreislaufdienst — zehn offene Browser erzeugen eine Leitung,
// nicht zehn —, und der Verteiler trägt ihre Meldungen an jede offene Sicht.
builder.Services.AddSingleton<Ereignisverteiler>();
// Die Pause zwischen zwei Versuchen steht wie die Standzeit der Marke in der Konfiguration: ein
// Testlauf startet die WebApi zwischen zwei Tests neu und darf nicht auf sie warten muessen.
var wiederaufnahmepause = Sekundenwert.Aus(builder.Configuration["Oberflaeche:WiederaufnahmepauseInSekunden"], Ereignisleitung.Vorgabepause);
builder.Services.AddHostedService(dienste => new Ereignisleitung(
    dienste.GetRequiredService<IHttpClientFactory>(),
    dienste.GetRequiredService<Ereignisverteiler>(),
    wiederaufnahmepause,
    () => DateTimeOffset.UtcNow,
    dienste.GetRequiredService<ILogger<Ereignisleitung>>()));
builder.Services.AddSingleton(Markenstandzeit.Aus(builder.Configuration["Oberflaeche:MarkenstandzeitInSekunden"]));
// Ab wie vielen nachgeholten Änderungen das Band nur noch zählt — dieselbe Sorte Stelle wie die
// Markenstandzeit, damit ein Testlauf die Schwelle senken kann.
builder.Services.AddSingleton(Aufschliessschwelle.Aus(builder.Configuration["Oberflaeche:AufschliessschwelleInAenderungen"]));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
