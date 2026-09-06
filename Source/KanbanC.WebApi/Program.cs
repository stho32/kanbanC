using KanbanC.BL.Integrations.Boards;
using KanbanC.BL.Integrations.Karten;
using KanbanC.BL.Integrations.Kontributoren;
using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Interfaces.Kontributoren;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Persistenz;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.BL.Persistenz.Kontributoren;
using KanbanC.BL.Persistenz.Migrationen;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi.Endpunkte;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Die Obergrenze der Anhaenge gilt auch dann, wenn ein Agent an der Oberflaeche vorbei direkt
// diese API ruft — eine Grenze, die nur in der Oberflaeche steht, ist keine. Dieselbe Konstante
// wie im Validator und im Blazor-Kreislauf; eine zweite Zahl gibt es nicht.
builder.Services.Configure<FormOptions>(optionen => optionen.MultipartBodyLengthLimit = Anhangsgrenze.HoechsteDateigroesse);

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
builder.Services.AddSingleton<IKontributorenRepository, KontributorenRepository>();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddSingleton<SpaltenService>();
builder.Services.AddSingleton<KartenService>();
builder.Services.AddSingleton<KontributorenService>();

var app = builder.Build();

app.Services.GetRequiredService<Migrationslaeufer>().FuehreAus();

Anhangsgrenzenwaechter.Registriere(app);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/zustand", () => Results.Ok(new { dienst = "KanbanC.WebApi", bereit = true }))
   .WithName("ZustandLesen");

BoardEndpunkte.Registriere(app);
SpaltenEndpunkte.Registriere(app);
KartenEndpunkte.Registriere(app);
KontributorenEndpunkte.Registriere(app);

app.Run();

// Sichtbar für die Integrationstests (WebApplicationFactory braucht die Einstiegsklasse).
public partial class Program;
