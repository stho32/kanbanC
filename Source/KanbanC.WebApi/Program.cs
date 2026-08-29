var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/zustand", () => Results.Ok(new { dienst = "KanbanC.WebApi", bereit = true }))
   .WithName("ZustandLesen");

app.Run();

// Sichtbar fuer die Integrationstests (WebApplicationFactory braucht die Einstiegsklasse).
public partial class Program;
