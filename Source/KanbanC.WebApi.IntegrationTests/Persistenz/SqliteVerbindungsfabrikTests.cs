using System.Data;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

public class SqliteVerbindungsfabrikTests
{
    [Test]
    public void Wenn_die_Datei_fehlt_dann_liefert_Oeffne_eine_offene_Verbindung_und_legt_die_Datei_an()
    {
        using var datenbank = new TemporaereDatenbank();
        Assert.That(File.Exists(datenbank.Dateipfad), Is.False);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();

        Assert.That(verbindung.State, Is.EqualTo(ConnectionState.Open));
        Assert.That(File.Exists(datenbank.Dateipfad), Is.True);
    }
}
