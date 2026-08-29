using System.Data;
using KanbanC.BL.Persistenz;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

public class SqliteVerbindungsfabrikTests
{
    [Test]
    public void Wenn_die_Datei_fehlt_dann_liefert_Oeffne_eine_offene_Verbindung_und_legt_die_Datei_an()
    {
        var dateipfad = Path.Combine(Path.GetTempPath(), $"kanbanc-test-{Guid.NewGuid():N}.db");
        var fabrik = new SqliteVerbindungsfabrik($"Data Source={dateipfad}");
        Assert.That(File.Exists(dateipfad), Is.False);

        try
        {
            using var verbindung = fabrik.Oeffne();

            Assert.That(verbindung.State, Is.EqualTo(ConnectionState.Open));
            Assert.That(File.Exists(dateipfad), Is.True);
        }
        finally
        {
            File.Delete(dateipfad);
        }
    }
}
