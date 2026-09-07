using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;
using KanbanC.Contracts.Import;

namespace KanbanC.BL.Tests.Operations.Import;

// **Fünf Zahlen** und eine Zeile je Knoten in Dateireihenfolge; verwaiste Karten stehen dahinter,
// weil sie keine Zeilennummer in der Datei haben.
public class ImportberichtbildnerTests
{
    [Test]
    public void Wenn_der_Bericht_gebildet_wird_dann_traegt_er_fuenf_Zahlen_aus_den_Zeilen()
    {
        var bericht = Importberichtbildner.Bilde(Bildung(), [Uebersprungene(5)], Kartenzahlen(), Wirkungen(), [Verwaistenzeile("I0019", "WBS-47")]);

        Assert.Multiple(() =>
        {
            Assert.That(bericht.Angelegt, Is.EqualTo(1));
            Assert.That(bericht.Geaendert, Is.EqualTo(1));
            Assert.That(bericht.Unveraendert, Is.EqualTo(1));
            Assert.That(bericht.Uebersprungen, Is.EqualTo(1));
            Assert.That(bericht.Verwaist, Is.EqualTo(1));
        });
    }

    [Test]
    public void Wenn_der_Bericht_gebildet_wird_dann_stehen_die_Dateizeilen_in_Dateireihenfolge_und_die_verwaisten_dahinter()
    {
        var bericht = Importberichtbildner.Bilde(Bildung(), [Uebersprungene(5)], Kartenzahlen(), Wirkungen(), [Verwaistenzeile("I0019", "WBS-47")]);

        Assert.That(bericht.Zeilen.Select(zeile => zeile.Kennung), Is.EqualTo(new[] { "I0001", "I0002", "Zeile 5", "I0003", "I0019" }));
    }

    // Die Kartennummer steht an der Zeile, wo es eine gibt — an einer anzulegenden Karte nicht.
    [Test]
    public void Wenn_eine_Karte_wiedererkannt_wurde_dann_traegt_ihre_Zeile_die_Kartennummer()
    {
        var bericht = Importberichtbildner.Bilde(Bildung(), [], Kartenzahlen(), Wirkungen(), []);

        Assert.Multiple(() =>
        {
            Assert.That(bericht.Zeilen.Single(zeile => zeile.Kennung == "I0001").Kartennummer, Is.Null);
            Assert.That(bericht.Zeilen.Single(zeile => zeile.Kennung == "I0002").Kartennummer, Is.EqualTo("WBS-02"));
            Assert.That(bericht.Zeilen.Single(zeile => zeile.Kennung == "I0003").Kartennummer, Is.EqualTo("WBS-03"));
        });
    }

    [Test]
    public void Wenn_der_Vergleich_an_einer_Zeile_etwas_zu_sagen_hat_dann_steht_es_als_Grund_daran()
    {
        var bericht = Importberichtbildner.Bilde(Bildung(), [], Kartenzahlen(), Wirkungen(), []);

        Assert.That(bericht.Zeilen.Single(zeile => zeile.Kennung == "I0002").Grund, Is.EqualTo("1 Abhakung zurückgenommen"));
    }

    // Ein Knoten, der Etikett oder Teilaufgabe wurde, behält seine Zeile unangetastet — der
    // Vergleich hat zu ihm nichts zu sagen.
    [Test]
    public void Wenn_ein_Knoten_keine_Karte_wurde_dann_bleibt_seine_Zeile_unveraendert()
    {
        var bildung = new Kartenentwurfsbildung(new Kartenentwuerfe([]), [Zeile(1, "D0001", Importwirkung.Etikett)]);

        var bericht = Importberichtbildner.Bilde(bildung, [], Kartenzahlen(), new Kartenwirkungen([]), []);

        Assert.Multiple(() =>
        {
            Assert.That(bericht.Zeilen.Single().Wirkung, Is.EqualTo(Importwirkung.Etikett));
            Assert.That(bericht.Angelegt, Is.Zero);
        });
    }

    private static Kartenentwurfsbildung Bildung()
    {
        return new Kartenentwurfsbildung(
            new Kartenentwuerfe([]),
            [Zeile(3, "I0001", Importwirkung.Angelegt), Zeile(4, "I0002", Importwirkung.Angelegt), Zeile(6, "I0003", Importwirkung.Angelegt)]);
    }

    private static Importberichtzeile Zeile(int zeilennummer, string kennung, Importwirkung wirkung)
    {
        return new Importberichtzeile(zeilennummer, new Importzeile(kennung, "Interaction", wirkung, null, null));
    }

    private static Kartenwirkungen Wirkungen()
    {
        return new Kartenwirkungen(
        [
            new Kartenwirkung("I0001", Importwirkung.Angelegt, null, null),
            new Kartenwirkung("I0002", Importwirkung.Geaendert, "WBS-02", "1 Abhakung zurückgenommen"),
            new Kartenwirkung("I0003", Importwirkung.Unveraendert, "WBS-03", null),
        ]);
    }

    private static Uebersprungenezeile Uebersprungene(int zeilennummer)
    {
        return new Uebersprungenezeile($"Zeile {zeilennummer}", zeilennummer, "Die Zeile hat 9 Zellen statt 12.");
    }

    private static Importzeile Verwaistenzeile(string kennung, string kartennummer)
    {
        return new Importzeile(kennung, "Interaction", Importwirkung.Verwaist, "steht nicht mehr in der Datei", kartennummer);
    }

    private static Kartenzahlen Kartenzahlen()
    {
        return new Kartenzahlen(1, 3, 3, 3);
    }
}
