using Dapper;
using KanbanC.BL.Persistenz;
using KanbanC.BL.Persistenz.Migrationen;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

public class MigrationslaeuferTests
{
    [Test]
    public void Wenn_die_Datei_leer_ist_dann_legt_FuehreAus_die_Tabellen_Board_und_Spalte_an()
    {
        var dateipfad = Path.Combine(Path.GetTempPath(), $"kanbanc-test-{Guid.NewGuid():N}.db");
        var fabrik = new SqliteVerbindungsfabrik($"Data Source={dateipfad}");

        try
        {
            Assert.That(Tabellennamen(fabrik), Is.Empty);

            new Migrationslaeufer(fabrik).FuehreAus();

            Assert.That(Tabellennamen(fabrik), Is.SupersetOf(new[] { "Board", "Spalte" }));
        }
        finally
        {
            File.Delete(dateipfad);
        }
    }

    private static List<string> Tabellennamen(SqliteVerbindungsfabrik fabrik)
    {
        using var verbindung = fabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT name
              FROM sqlite_master
             WHERE type = 'table'
               AND name NOT LIKE 'sqlite_%'").ToList();
    }
}
