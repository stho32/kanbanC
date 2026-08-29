using Dapper;
using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Persistenz.Boards;
using KanbanC.BL.Persistenz.Migrationen;
using KanbanC.Contracts.Boards;
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

    [Test]
    public void Wenn_FuehreAus_auf_einer_gefuellten_Datei_ein_zweites_Mal_laeuft_dann_bleiben_Schema_und_Daten_unveraendert()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        var repository = new BoardRepository(datenbank.Verbindungsfabrik);
        var angelegt = repository.LegeAn(new BoardAnlegenAnfrage("Entwicklung", BoardArt.Linie, null, null), StandardspaltenVorlage.FuerNeuesBoard());
        var schemaVorher = SchemaDefinitionen(datenbank);
        var zeilenVorher = Zeilenanzahlen(datenbank);

        Assert.That(() => new Migrationslaeufer(datenbank.Verbindungsfabrik).FuehreAus(), Throws.Nothing);

        var geladen = new BoardRepository(datenbank.Verbindungsfabrik).Lade(angelegt.BoardId);
        Assert.That(geladen, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(SchemaDefinitionen(datenbank), Is.EqualTo(schemaVorher));
            Assert.That(Zeilenanzahlen(datenbank), Is.EqualTo(zeilenVorher));
            Assert.That(geladen.Name, Is.EqualTo("Entwicklung"));
            Assert.That(geladen.Spalten, Is.EqualTo(angelegt.Spalten));
        });
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

    private static List<string> SchemaDefinitionen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.Query<string>(@"
            SELECT sql
              FROM sqlite_master
             WHERE sql IS NOT NULL
             ORDER BY name").ToList();
    }

    private static (long Boards, long Spalten) Zeilenanzahlen(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        var boards = verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Board");
        var spalten = verbindung.ExecuteScalar<long>(@"
            SELECT COUNT(*)
              FROM Spalte");
        return (boards, spalten);
    }
}
