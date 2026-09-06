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

    // Der Ablageordner der Anhaenge liegt neben der Datenbankdatei und wird von der Anwendung
    // beim ersten Anhang angelegt. Der Test kennt seinen Namen, damit er am Dateisystem pruefen
    // kann — und damit er ihn wieder abraeumt.
    public string Ablageordner => Dateipfad + "-Files";

    public TemporaereDatenbank MitSchema()
    {
        new Migrationslaeufer(Verbindungsfabrik).FuehreAus();
        return this;
    }

    public void Dispose()
    {
        File.Delete(Dateipfad);
        var derAblageordnerIstEntstanden = Directory.Exists(Ablageordner);
        if (derAblageordnerIstEntstanden)
        {
            Directory.Delete(Ablageordner, recursive: true);
        }
    }
}
