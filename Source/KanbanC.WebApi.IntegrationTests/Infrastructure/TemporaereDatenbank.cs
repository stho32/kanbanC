using KanbanC.BL.Persistenz;
using KanbanC.BL.Persistenz.Migrationen;

namespace KanbanC.WebApi.IntegrationTests.Infrastructure;

public sealed class TemporaereDatenbank : IDisposable
{
    public TemporaereDatenbank()
    {
        Dateipfad = Path.Combine(Path.GetTempPath(), $"kanbanc-test-{Guid.NewGuid():N}.db");
        Verbindungsfabrik = new SqliteVerbindungsfabrik($"Data Source={Dateipfad}");
    }

    public string Dateipfad { get; }

    public SqliteVerbindungsfabrik Verbindungsfabrik { get; }

    public TemporaereDatenbank MitSchema()
    {
        new Migrationslaeufer(Verbindungsfabrik).FuehreAus();
        return this;
    }

    public void Dispose()
    {
        File.Delete(Dateipfad);
    }
}
