using KanbanC.Blazor.Components;
using KanbanC.Blazor.Services;
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

builder.Services.AddSingleton(new Anhangbasisadresse(oeffentlicheBasisAdresse));

builder.Services.AddHttpClient("KanbanC", client =>
{
    client.BaseAddress = new Uri(webApiBasisAdresse);
});
builder.Services.AddScoped<BoardApiKlient>();
builder.Services.AddScoped<SpaltenApiKlient>();
builder.Services.AddScoped<KartenApiKlient>();
builder.Services.AddScoped<KontributorenApiKlient>();
builder.Services.AddScoped<KartenklassenApiKlient>();
builder.Services.AddScoped<Identitaetsspeicher>();
builder.Services.AddScoped<Pfadkopie>();

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
