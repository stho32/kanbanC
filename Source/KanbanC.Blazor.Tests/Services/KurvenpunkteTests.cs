using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Die Arithmetik des Bildes steht im Test, nicht im Markup: ein falscher Punkt fällt auf, bevor
// ihn jemand sieht.
public class KurvenpunkteTests
{
    private const int Breite = 600;
    private const int Hoehe = 160;

    [Test]
    public void Wenn_die_Reihe_fuenf_Tage_traegt_dann_traegt_das_Punkteattribut_fuenf_Paare()
    {
        var bild = Kurvenpunkte.Aus([4, 4, 2, 2, 2], Breite, Hoehe);

        Assert.Multiple(() =>
        {
            Assert.That(bild.Punkte, Has.Count.EqualTo(5));
            Assert.That(bild.Punkteattribut.Split(' '), Has.Length.EqualTo(5));
            Assert.That(bild.Hoechstwert, Is.EqualTo(4));
        });
    }

    // Der Höchstwert liegt oben, die Null unten — und die Tage verteilen sich über die ganze
    // Breite.
    [Test]
    public void Wenn_die_Reihe_vom_Hoechstwert_auf_null_faellt_dann_laeuft_die_Kurve_von_oben_nach_unten()
    {
        var bild = Kurvenpunkte.Aus([4, 2, 0], Breite, Hoehe);

        Assert.That(bild.Punkteattribut, Is.EqualTo("0,0 300,80 600,160"));
    }

    [Test]
    public void Wenn_die_Reihe_nur_einen_Tag_traegt_dann_steht_der_Punkt_in_der_Mitte_der_Flaeche()
    {
        var bild = Kurvenpunkte.Aus([3], Breite, Hoehe);

        Assert.Multiple(() =>
        {
            Assert.That(bild.Punkte, Has.Count.EqualTo(1));
            Assert.That(bild.Punkteattribut, Is.EqualTo("300,0"));
        });
    }

    [Test]
    public void Wenn_die_Reihe_zwei_Tage_traegt_dann_stehen_die_Punkte_an_den_beiden_Raendern()
    {
        var bild = Kurvenpunkte.Aus([2, 1], Breite, Hoehe);

        Assert.That(bild.Punkteattribut, Is.EqualTo("0,0 600,80"));
    }

    // Alle Werte gleich: die Kurve liegt flach am Höchstwert, es wird nicht durch Null geteilt.
    [Test]
    public void Wenn_alle_Werte_gleich_sind_dann_liegt_die_Kurve_flach_und_nichts_wird_durch_null_geteilt()
    {
        var bild = Kurvenpunkte.Aus([2, 2, 2], Breite, Hoehe);

        Assert.That(bild.Punkteattribut, Is.EqualTo("0,0 300,0 600,0"));
    }

    // Alle Karten erledigt: der Höchstwert ist 0, und die Kurve liegt auf der Grundlinie.
    [Test]
    public void Wenn_der_Hoechstwert_null_ist_dann_liegt_die_Kurve_auf_der_Grundlinie()
    {
        var bild = Kurvenpunkte.Aus([0, 0], Breite, Hoehe);

        Assert.Multiple(() =>
        {
            Assert.That(bild.Hoechstwert, Is.Zero);
            Assert.That(bild.Punkteattribut, Is.EqualTo("0,160 600,160"));
        });
    }

    [Test]
    public void Wenn_die_Reihe_leer_ist_dann_wird_sie_zurueckgewiesen()
    {
        Assert.That(() => Kurvenpunkte.Aus([], Breite, Hoehe), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Wenn_die_Wertmarken_gebildet_werden_dann_stehen_null_die_Mitte_und_der_Hoechstwert_daran()
    {
        var marken = Kurvenpunkte.Wertmarken(4, Hoehe);

        Assert.That(marken.Select(marke => marke.Wert), Is.EqualTo(new[] { 0, 2, 4 }));
        Assert.Multiple(() =>
        {
            Assert.That(marken[0].Y, Is.EqualTo(160));
            Assert.That(marken[1].Y, Is.EqualTo(80));
            Assert.That(marken[2].Y, Is.Zero);
        });
    }

    [Test]
    public void Wenn_der_Hoechstwert_null_ist_dann_traegt_die_Achse_nur_die_Grundlinie()
    {
        var marken = Kurvenpunkte.Wertmarken(0, Hoehe);

        Assert.That(marken.Select(marke => marke.Wert), Is.EqualTo(new[] { 0 }));
    }

    [Test]
    public void Wenn_der_Hoechstwert_eins_ist_dann_gibt_es_keine_eigene_Mitte()
    {
        var marken = Kurvenpunkte.Wertmarken(1, Hoehe);

        Assert.That(marken.Select(marke => marke.Wert), Is.EqualTo(new[] { 0, 1 }));
    }

    // Bis sieben Tage wird jeder beschriftet; darüber wird ausgedünnt, damit die Achse lesbar
    // bleibt — ein Jahr ergäbe sonst 365 Beschriftungen.
    [TestCase(1, 1)]
    [TestCase(5, 1)]
    [TestCase(7, 1)]
    [TestCase(8, 2)]
    [TestCase(14, 2)]
    [TestCase(365, 53)]
    public void Wenn_die_Achse_viele_Tage_traegt_dann_wird_die_Beschriftung_ausgeduennt(int tageanzahl, int erwarteterSchritt)
    {
        Assert.That(Kurvenpunkte.Beschriftungsschritt(tageanzahl), Is.EqualTo(erwarteterSchritt));
    }
}
