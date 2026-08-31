using KanbanC.BL.Integrations.Boards;
using KanbanC.BL.Integrations.Karten;
using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Persistenz;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.BL.Persistenz.Migrationen;
using KanbanC.WebApi.Endpunkte;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var verbindungszeichenfolge = builder.Configuration["Datenhaltung:Verbindungszeichenfolge"];
if (verbindungszeichenfolge is null)
{
    throw new InvalidOperationException("Datenhaltung:Verbindungszeichenfolge fehlt in der Konfiguration.");
}

builder.Services.AddSingleton<IDatenbankVerbindungsfabrik>(new SqliteVerbindungsfabrik(verbindungszeichenfolge));
builder.Services.AddSingleton<Migrationslaeufer>();
builder.Services.AddSingleton<IBoardRepository, BoardRepository>();
builder.Services.AddSingleton<ISpaltenRepository, SpaltenRepository>();
builder.Services.AddSingleton<IKartenRepository, KartenRepository>();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddSingleton<SpaltenService>();
builder.Services.AddSingleton<KartenService>();

var app = builder.Build();

app.Services.GetRequiredService<Migrationslaeufer>().FuehreAus();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/zustand", () => Results.Ok(new { dienst = "KanbanC.WebApi", bereit = true }))
   .WithName("ZustandLesen");

BoardEndpunkte.Registriere(app);
SpaltenEndpunkte.Registriere(app);
KartenEndpunkte.Registriere(app);

app.Run();

// Sichtbar für die Integrationstests (WebApplicationFactory braucht die Einstiegsklasse).
public partial class Program;
