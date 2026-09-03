using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

public class AblagestellenTests
{
    [Test]
    public void Wenn_die_Karte_aus_einer_anderen_Bahn_kommt_dann_ist_die_Zielposition_die_Stelle_plus_eins()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Ablagestellen.Zielposition(0, null), Is.EqualTo(1));
            Assert.That(Ablagestellen.Zielposition(1, null), Is.EqualTo(2));
            Assert.That(Ablagestellen.Zielposition(3, null), Is.EqualTo(4));
        });
    }

    [Test]
    public void Wenn_die_Stelle_vor_der_gezogenen_Karte_derselben_Bahn_liegt_dann_zaehlt_sie_wie_aus_einer_anderen_Bahn()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Ablagestellen.Zielposition(0, 3), Is.EqualTo(1));
            Assert.That(Ablagestellen.Zielposition(1, 3), Is.EqualTo(2));
            Assert.That(Ablagestellen.Zielposition(3, 3), Is.EqualTo(4));
        });
    }

    [Test]
    public void Wenn_die_Stelle_hinter_der_gezogenen_Karte_derselben_Bahn_liegt_dann_ruecken_die_Positionen_um_eins_vor()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Ablagestellen.Zielposition(4, 3), Is.EqualTo(4));
            Assert.That(Ablagestellen.Zielposition(2, 0), Is.EqualTo(2));
            Assert.That(Ablagestellen.Zielposition(1, 0), Is.EqualTo(1));
        });
    }

    // A, B, C, D in einer Bahn; D wird gezogen. Die fünf Stellen zeigen auf die Positionen,
    // die D nach dem Herausnehmen tatsächlich einnehmen kann — die letzte ist 4, nicht 5.
    [Test]
    public void Wenn_die_letzte_Karte_einer_Bahn_gezogen_wird_dann_bleibt_die_hoechste_Zielposition_die_Kartenzahl()
    {
        var positionen = new[] { 0, 1, 2, 3, 4 }.Select(stelle => Ablagestellen.Zielposition(stelle, 3));

        Assert.That(positionen, Is.EqualTo(new[] { 1, 2, 3, 4, 4 }));
    }

    // A, B, C, D; A wird gezogen. Vor A und hinter A bedeuten dasselbe: A bleibt an Position 1.
    [Test]
    public void Wenn_die_erste_Karte_einer_Bahn_gezogen_wird_dann_zeigen_die_beiden_Stellen_an_ihr_auf_Position_eins()
    {
        var positionen = new[] { 0, 1, 2, 3, 4 }.Select(stelle => Ablagestellen.Zielposition(stelle, 0));

        Assert.That(positionen, Is.EqualTo(new[] { 1, 1, 2, 3, 4 }));
    }
}
