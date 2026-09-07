using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

public class AufwandsbandTests
{
    [Test]
    public void Wenn_die_Zelle_einen_Einzelwert_mit_Dezimalkomma_traegt_dann_stehen_beide_Grenzen_gleich()
    {
        var band = Aufwandsband.Lies("0,4");

        Assert.That(band, Is.EqualTo(new Sollband(0.4m, 0.4m)));
    }

    [Test]
    public void Wenn_die_Zelle_eine_ganze_Zahl_traegt_dann_stehen_beide_Grenzen_auf_ihr()
    {
        var band = Aufwandsband.Lies("2");

        Assert.That(band, Is.EqualTo(new Sollband(2m, 2m)));
    }

    [Test]
    public void Wenn_die_Zelle_eine_Spanne_aus_ganzen_Zahlen_traegt_dann_stehen_Unter_und_Obergrenze_getrennt()
    {
        var band = Aufwandsband.Lies("2-4");

        Assert.That(band, Is.EqualTo(new Sollband(2m, 4m)));
    }

    [Test]
    public void Wenn_die_Zelle_eine_Spanne_mit_Dezimalkomma_traegt_dann_stehen_beide_Grenzen_wie_in_der_Datei()
    {
        var band = Aufwandsband.Lies("0,4-1,5");

        Assert.That(band, Is.EqualTo(new Sollband(0.4m, 1.5m)));
    }

    // Von Hand geschriebene Zeilen setzen Leerzeichen um den Bindestrich; sie sind kein Grund,
    // die Schätzung wegzuwerfen.
    [Test]
    public void Wenn_die_Spanne_Leerzeichen_um_den_Bindestrich_traegt_dann_wird_sie_trotzdem_gelesen()
    {
        var band = Aufwandsband.Lies(" 2 - 4 ");

        Assert.That(band, Is.EqualTo(new Sollband(2m, 4m)));
    }

    [Test]
    public void Wenn_die_Zelle_leer_ist_dann_gibt_es_kein_Band_und_keine_Ausnahme()
    {
        Assert.That(Aufwandsband.Lies(string.Empty), Is.Null);
        Assert.That(Aufwandsband.Lies("   "), Is.Null);
    }

    // Eine unlesbare Zelle liefert kein Band und wirft nicht: die Karte steht dann ohne Soll da.
    [TestCase("mittel")]
    [TestCase("2-4-6")]
    [TestCase("zwei bis vier")]
    [TestCase("-")]
    [TestCase("2-")]
    [TestCase("4-2")]
    public void Wenn_die_Zelle_unlesbar_ist_dann_gibt_es_kein_Band_und_keine_Ausnahme(string zelle)
    {
        Sollband? band = null;

        Assert.That(() => band = Aufwandsband.Lies(zelle), Throws.Nothing);
        Assert.That(band, Is.Null);
    }

    // Gelesen wird invariant: die Datei führt deutsches Dezimalkomma, ein Punkt darin wäre eine
    // fremde Schreibweise derselben Zahl und keine dritte Bedeutung.
    [Test]
    public void Wenn_die_Zelle_einen_Dezimalpunkt_traegt_dann_meint_sie_dieselbe_Zahl_wie_mit_Komma()
    {
        Assert.That(Aufwandsband.Lies("0.4"), Is.EqualTo(Aufwandsband.Lies("0,4")));
    }
}
