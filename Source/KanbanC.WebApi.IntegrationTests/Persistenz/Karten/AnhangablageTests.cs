using System.Text;
using KanbanC.BL.Persistenz.Karten;

namespace KanbanC.WebApi.IntegrationTests.Persistenz.Karten;

// Die Ablage fasst das Dateisystem an und ist deshalb ein Integrationstest, kein Unit-Test.
// Gearbeitet wird gegen einen temporären Ordner, der nach jedem Test wieder verschwindet.
public class AnhangablageTests
{
    [Test]
    public void Wenn_ein_Strom_abgelegt_wird_dann_liegt_die_Datei_am_gerechneten_Pfad_und_traegt_dieselben_Bytes()
    {
        using var ordner = new TemporaererOrdner();
        var pfad = Path.Combine(ordner.Pfad, "14", "7");
        var inhalt = Bytes(41000);

        var geschrieben = Anhangablage.Lege(pfad, new MemoryStream(inhalt));

        Assert.That(geschrieben, Is.EqualTo(41000));
        Assert.That(File.Exists(pfad), Is.True);
        Assert.That(File.ReadAllBytes(pfad), Is.EqualTo(inhalt));
    }

    // Der Unterordner der Karte entsteht mit dem ersten Anhang; niemand legt ihn vorher an.
    [Test]
    public void Wenn_der_Unterordner_der_Karte_noch_fehlt_dann_legt_das_Ablegen_ihn_an()
    {
        using var ordner = new TemporaererOrdner();
        var pfad = Path.Combine(ordner.Pfad, "14", "7");
        Assert.That(Directory.Exists(Path.Combine(ordner.Pfad, "14")), Is.False);

        Anhangablage.Lege(pfad, new MemoryStream(Bytes(10)));

        Assert.That(Directory.Exists(Path.Combine(ordner.Pfad, "14")), Is.True);
    }

    // Die geschriebene Laenge ist die Wahrheit ueber die Datei: was der Aufrufer meldet, geht
    // hier gar nicht erst ein — die Ablage kennt nur den Strom.
    [Test]
    public void Wenn_der_Strom_kuerzer_ist_als_erwartet_dann_meldet_das_Ablegen_die_tatsaechlich_geschriebene_Laenge()
    {
        using var ordner = new TemporaererOrdner();
        var pfad = Path.Combine(ordner.Pfad, "14", "7");

        var geschrieben = Anhangablage.Lege(pfad, new MemoryStream(Bytes(3)));

        Assert.That(geschrieben, Is.EqualTo(3));
        Assert.That(new FileInfo(pfad).Length, Is.EqualTo(3));
    }

    [Test]
    public void Wenn_eine_abgelegte_Datei_geoeffnet_wird_dann_liefert_der_Lesestrom_dieselben_Bytes()
    {
        using var ordner = new TemporaererOrdner();
        var pfad = Path.Combine(ordner.Pfad, "14", "7");
        var inhalt = Encoding.UTF8.GetBytes("Playwright-Lizenz klären");
        Anhangablage.Lege(pfad, new MemoryStream(inhalt));

        using var lesestrom = Anhangablage.Oeffne(pfad);
        using var gelesen = new MemoryStream();
        lesestrom.CopyTo(gelesen);

        Assert.That(gelesen.ToArray(), Is.EqualTo(inhalt));
    }

    // Der Fall aus US-2: die Zeile steht, die Datei fehlt. Das scheitert sichtbar, statt eine
    // leere Datei auszuliefern.
    [Test]
    public void Wenn_die_Datei_zu_einer_Zeile_fehlt_dann_scheitert_das_Oeffnen_sichtbar_statt_leer_zu_liefern()
    {
        using var ordner = new TemporaererOrdner();
        var pfad = Path.Combine(ordner.Pfad, "14", "7");

        var fehler = Assert.Throws<FileNotFoundException>(() => Anhangablage.Oeffne(pfad));

        Assert.That(fehler!.Message, Does.Contain(pfad));
    }

    [Test]
    public void Wenn_ein_Anhang_entfernt_wird_dann_ist_die_Datei_wirklich_weg_und_die_Nachbardatei_bleibt()
    {
        using var ordner = new TemporaererOrdner();
        var erster = Path.Combine(ordner.Pfad, "14", "7");
        var zweiter = Path.Combine(ordner.Pfad, "14", "8");
        Anhangablage.Lege(erster, new MemoryStream(Bytes(100)));
        Anhangablage.Lege(zweiter, new MemoryStream(Bytes(200)));

        Anhangablage.Entferne(erster);

        Assert.That(File.Exists(erster), Is.False);
        Assert.That(new FileInfo(zweiter).Length, Is.EqualTo(200));
    }

    [Test]
    public void Wenn_zu_einer_Zeile_keine_Datei_liegt_dann_bleibt_das_Entfernen_ohne_Fehler()
    {
        using var ordner = new TemporaererOrdner();
        var pfad = Path.Combine(ordner.Pfad, "14", "7");

        Assert.That(() => Anhangablage.Entferne(pfad), Throws.Nothing);
    }

    private static byte[] Bytes(int laenge)
    {
        var inhalt = new byte[laenge];
        for (var stelle = 0; stelle < laenge; stelle++)
        {
            inhalt[stelle] = (byte)(stelle % 251);
        }

        return inhalt;
    }

    private sealed class TemporaererOrdner : IDisposable
    {
        public TemporaererOrdner()
        {
            Pfad = Path.Combine(Path.GetTempPath(), $"kanbanc-test-{Guid.NewGuid():N}.db-Files");
        }

        public string Pfad { get; }

        public void Dispose()
        {
            var derOrdnerIstEntstanden = Directory.Exists(Pfad);
            if (derOrdnerIstEntstanden)
            {
                Directory.Delete(Pfad, recursive: true);
            }
        }
    }
}
