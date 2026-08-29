using Dapper;
using KanbanC.BL.Persistenz.Migrationen;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

public class MigrationslaeuferTests
{
    [Test]
    public void Wenn_die_Datei_leer_ist_dann_legt_FuehreAus_die_Tabellen_Board_und_Spalte_an()
    {
        using var datenbank = new TemporaereDatenbank();
        Assert.That(Tabellennamen(datenbank), Is.Empty);

        new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus();

        Assert.That(Tabellennamen(datenbank), Is.SupersetOf(new[] { "Board", "Spalte" }));
    }

    private static List<string> Tabellennamen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT name
              FROM sqlite_master
             WHERE type = 'table'
               AND name NOT LIKE 'sqlite_%'").ToList();
    }
}
