using KanbanC.Blazor.Components;
using KanbanC.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Die Oberflaeche spricht ausschliesslich ueber die WebApi mit den Daten -
// es gibt bewusst keine Projektreferenz auf KanbanC.BL.
builder.Services.AddHttpClient("KanbanC", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WebApi:BasisAdresse"]
        ?? throw new InvalidOperationException("WebApi:BasisAdresse fehlt in der Konfiguration."));
});
builder.Services.AddScoped<BoardApiKlient>();

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
