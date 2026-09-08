using KanbanC.BL.Integrations.Auswertungen;
using KanbanC.BL.Integrations.Boardimport;
using KanbanC.BL.Integrations.Boards;
using KanbanC.BL.Integrations.Export;
using KanbanC.BL.Integrations.Import;
using KanbanC.BL.Integrations.Karten;
using KanbanC.BL.Integrations.Klassen;
using KanbanC.BL.Integrations.Kontributoren;
using KanbanC.BL.Integrations.Rohdaten;
using KanbanC.BL.Integrations.Zeiten;
using KanbanC.BL.Interfaces.Auswertungen;
using KanbanC.BL.Interfaces.Boardimport;
using KanbanC.BL.Interfaces.Boards;
using KanbanC.BL.Interfaces.Export;
using KanbanC.BL.Interfaces.Import;
using KanbanC.BL.Interfaces.Karten;
using KanbanC.BL.Interfaces.Klassen;
using KanbanC.BL.Interfaces.Kontributoren;
using KanbanC.BL.Interfaces.Persistenz;
using KanbanC.BL.Interfaces.Rohdaten;
using KanbanC.BL.Interfaces.Zeiten;
using KanbanC.BL.Persistenz;
using KanbanC.BL.Persistenz.Auswertungen;
using KanbanC.BL.Persistenz.Boardimport;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Export;
using KanbanC.BL.Persistenz.Import;
using KanbanC.BL.Persistenz.Karten;
using KanbanC.BL.Persistenz.Klassen;
using KanbanC.BL.Persistenz.Kontributoren;
using KanbanC.BL.Persistenz.Migrationen;
using KanbanC.BL.Persistenz.Rohdaten;
using KanbanC.BL.Persistenz.Zeiten;
using KanbanC.Contracts.Karten;
using KanbanC.WebApi;
using KanbanC.WebApi.Endpunkte;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Die Obergrenze der Anhaenge gilt auch dann, wenn ein Agent an der Oberflaeche vorbei direkt
// diese API ruft — eine Grenze, die nur in der Oberflaeche steht, ist keine. Dieselbe Konstante
// wie im Validator und im Blazor-Kreislauf; eine zweite Zahl gibt es nicht.
builder.Services.Configure<FormOptions>(optionen => optionen.MultipartBodyLengthLimit = Anhangsgrenze.HoechsteDateigroesse);

// Die Formularbindung meldet den Abbruch am Rumpf nur, wenn sie werfen darf — der Vorgabe nach
// tut sie das allein in Development. Im Betrieb bliebe sonst eine 400 ohne Befund stehen, und die
// Obergrenze wäre für den Agenten, der die API direkt ruft, eine wortlose Abweisung.
builder.Services.Configure<RouteHandlerOptions>(optionen => optionen.ThrowOnBadRequest = true);

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
builder.Services.AddSingleton<IKartenklassenRepository, KartenklassenRepository>();
builder.Services.AddSingleton<IZeitenRepository, ZeitenRepository>();
builder.Services.AddSingleton<IWbsImportRepository, WbsImportRepository>();
builder.Services.AddSingleton<IAuswertungsrepository, Auswertungsrepository>();
builder.Services.AddSingleton<IRohdatenRepository, RohdatenRepository>();
builder.Services.AddSingleton<IBoardexportRepository, BoardexportRepository>();
builder.Services.AddSingleton<IBoardimportRepository, BoardimportRepository>();
builder.Services.AddSingleton<BoardService>();
builder.Services.AddSingleton<SpaltenService>();
builder.Services.AddSingleton<KartenService>();
builder.Services.AddSingleton<KontributorenService>();
builder.Services.AddSingleton<KartenklassenService>();
builder.Services.AddSingleton<ZeitenService>();
builder.Services.AddSingleton<WbsImportService>();
builder.Services.AddSingleton<AuswertungsService>();
builder.Services.AddSingleton<RohdatenService>();
builder.Services.AddSingleton<BoardexportService>();
builder.Services.AddSingleton<BoardimportService>();

// Eine Drehscheibe je Prozess: sie nimmt die Meldungen der Endpunkte an und gibt jedem Abonnenten
// von GET /api/ereignisse seinen eigenen Strom.
builder.Services.AddSingleton<Ereignisdrehscheibe>();

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
KartenklassenEndpunkte.Registriere(app);
ZeitenEndpunkte.Registriere(app);
WbsImportEndpunkte.Registriere(app);
AuswertungsEndpunkte.Registriere(app);
RohdatenEndpunkte.Registriere(app);
ExportEndpunkte.Registriere(app);
BoardimportEndpunkte.Registriere(app);
EreignisEndpunkte.Registriere(app);

app.Run();

// Sichtbar für die Integrationstests (WebApplicationFactory braucht die Einstiegsklasse).
public partial class Program;
