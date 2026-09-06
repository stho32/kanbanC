using System.Data;
using Dapper;
using KanbanC.WebApi.IntegrationTests.Infrastructure;

namespace KanbanC.WebApi.IntegrationTests.Persistenz;

// Die Fabrik öffnet Verbindungen — und liefert die Schreibtransaktion, auf der die
// Nummernvergabe steht: sie schließt die zweite aus, solange sie läuft. Ohne diesen Beleg wäre
// der nebenläufige Fall in der Kartenklassenzuordnung eine Annahme über SQLite und kein
// geprüftes Verhalten — paralleler Schreibzugriff war im Repository vorher nirgends belegt.
public class SqliteVerbindungsfabrikTests
{
    // So lange hält der erste Schreiber das Schloss, während geprüft wird, dass der zweite es
    // nicht bekommt. Eine korrekte Schreibtransaktion lässt ihn beliebig lange nicht durch; die
    // Spanne entscheidet nur, wie sicher ein fehlendes Schloss auffliegt.
    private static readonly TimeSpan Wartezeit = TimeSpan.FromMilliseconds(250);

    [Test]
    public void Wenn_die_Datei_fehlt_dann_liefert_Oeffne_eine_offene_Verbindung_und_legt_die_Datei_an()
    {
        using var datenbank = new TemporaereDatenbank();
        Assert.That(File.Exists(datenbank.Dateipfad), Is.False);

        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();

        Assert.That(verbindung.State, Is.EqualTo(ConnectionState.Open));
        Assert.That(File.Exists(datenbank.Dateipfad), Is.True);
    }

    [Test]
    public void Wenn_eine_Schreibtransaktion_laeuft_dann_bekommt_die_zweite_das_Schloss_erst_nach_ihrem_Abschluss()
    {
        using var datenbank = new TemporaereDatenbank().MitSchema();
        LegeBoardEin(datenbank);
        var fabrik = datenbank.Verbindungsfabrik;
        using var zweiterGreiftZu = new ManualResetEventSlim();
        var zweiterHatDasSchloss = false;
        var zweiter = Task.Run(() =>
        {
            using var verbindung = fabrik.Oeffne();
            zweiterGreiftZu.Set();
            using var transaktion = fabrik.BeginneSchreibtransaktion(verbindung);
            Volatile.Write(ref zweiterHatDasSchloss, true);
            BenenneBoardUm(verbindung, transaktion, "Zweiter");
            transaktion.Commit();
        });

        using (var verbindung = fabrik.Oeffne())
        {
            using var transaktion = fabrik.BeginneSchreibtransaktion(verbindung);
            BenenneBoardUm(verbindung, transaktion, "Erster");
            zweiterGreiftZu.Wait();
            Thread.Sleep(Wartezeit); // stil-check: C03 die Wartezeit ist hier der Prüfgegenstand: das Schloss soll halten, solange es gehalten wird

            Assert.That(Volatile.Read(ref zweiterHatDasSchloss), Is.False, "Der zweite Schreiber hat das Schloss bekommen, während der erste es hielt.");
            transaktion.Commit();
        }

        zweiter.Wait();
        Assert.Multiple(() =>
        {
            Assert.That(zweiterHatDasSchloss, Is.True);
            Assert.That(Boardname(datenbank), Is.EqualTo("Zweiter"));
        });
    }

    private static void LegeBoardEin(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        verbindung.Execute(@"
            INSERT INTO Board (Name, Art)
            VALUES ('Entwicklung', 'Linie')");
    }

    private static void BenenneBoardUm(IDbConnection verbindung, IDbTransaction transaktion, string name)
    {
        verbindung.Execute(@"
            UPDATE Board
               SET Name = @Name
             WHERE BoardId = 1", new { Name = name }, transaktion);
    }

    private static string Boardname(TemporaereDatenbank datenbank)
    {
        using var verbindung = datenbank.Verbindungsfabrik.Oeffne();
        return verbindung.ExecuteScalar<string>(@"
            SELECT Name
              FROM Board
             WHERE BoardId = 1")!;
    }
}
