using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

public class ZeilenzerlegerTests
{
    [Test]
    public void Wenn_eine_Knotenzeile_zerlegt_wird_dann_entstehen_zwoelf_Zellen()
    {
        var zeile = "| I0001 | Interaction | D0001 | Board anlegen | gruen | Fertig | Ein → Aus | 2 | Stufe | I0021 | R00001 | Notiz |";

        var zellen = Zeilenzerleger.Zerlege(zeile);

        Assert.Multiple(() =>
        {
            Assert.That(zellen.Zellenanzahl, Is.EqualTo(12));
            Assert.That(zellen[0], Is.EqualTo("I0001"));
            Assert.That(zellen[4], Is.EqualTo("gruen"));
            Assert.That(zellen[11], Is.EqualTo("Notiz"));
        });
    }

    [Test]
    public void Wenn_eine_Zelle_ein_maskiertes_Trennzeichen_traegt_dann_bleibt_es_ein_Zeichen()
    {
        var zeile = @"| I0001 | Interaction | D0001 | Name | gruen | a \| b | | | | | | |";

        var zellen = Zeilenzerleger.Zerlege(zeile);

        Assert.Multiple(() =>
        {
            Assert.That(zellen.Zellenanzahl, Is.EqualTo(12));
            Assert.That(zellen[5], Is.EqualTo("a | b"));
        });
    }

    [Test]
    public void Wenn_eine_Zeile_zu_wenige_Zellen_hat_dann_meldet_der_Zerleger_die_gefundene_Zahl()
    {
        var zeile = "| I0001 | Interaction | D0001 |";

        var zellen = Zeilenzerleger.Zerlege(zeile);

        Assert.That(zellen.Zellenanzahl, Is.EqualTo(3));
    }

    [Test]
    public void Wenn_eine_Zeile_zu_viele_Zellen_hat_dann_meldet_der_Zerleger_die_gefundene_Zahl()
    {
        var zeile = "| a | b | c | d | e | f | g | h | i | j | k | l | m |";

        var zellen = Zeilenzerleger.Zerlege(zeile);

        Assert.That(zellen.Zellenanzahl, Is.EqualTo(13));
    }

    // Die Ränder sind keine Zellen: eine Tabellenzeile beginnt und endet mit dem Trennzeichen.
    [Test]
    public void Wenn_eine_Zeile_kein_Trennzeichen_traegt_dann_entstehen_keine_Zellen()
    {
        var zellen = Zeilenzerleger.Zerlege("nur Text ohne Tabelle");

        Assert.That(zellen.Zellenanzahl, Is.Zero);
    }

    [Test]
    public void Wenn_eine_Zeile_mit_dem_Trennzeichen_beginnt_dann_ist_sie_eine_Tabellenzeile()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Zeilenzerleger.IstTabellenzeile("| a |"), Is.True);
            Assert.That(Zeilenzerleger.IstTabellenzeile("   | a |"), Is.True);
            Assert.That(Zeilenzerleger.IstTabellenzeile("## Knoten"), Is.False);
        });
    }
}
