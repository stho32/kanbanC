using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

public class AblagestellenTests
{
    [Test]
    public void Wenn_die_obere_Haelfte_einer_Karte_ueberfahren_wird_dann_zielt_sie_auf_die_Fuge_vor_ihr()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Ablagestellen.Fuge(0, Kartenhaelfte.Oben), Is.EqualTo(0));
            Assert.That(Ablagestellen.Fuge(2, Kartenhaelfte.Oben), Is.EqualTo(2));
        });
    }

    [Test]
    public void Wenn_die_untere_Haelfte_einer_Karte_ueberfahren_wird_dann_zielt_sie_auf_die_Fuge_dahinter()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Ablagestellen.Fuge(0, Kartenhaelfte.Unten), Is.EqualTo(1));
            Assert.That(Ablagestellen.Fuge(2, Kartenhaelfte.Unten), Is.EqualTo(3));
        });
    }

    // Zielbahn [A, B, C], der Zug kommt aus einer anderen Bahn: über A oben ergibt Position 1.
    [Test]
    public void Wenn_die_Karte_aus_einer_anderen_Bahn_ueber_die_obere_Haelfte_der_ersten_kommt_dann_landet_sie_auf_Position_eins()
    {
        Assert.That(Ablagestellen.Zielposition(0, Kartenhaelfte.Oben, null), Is.EqualTo(1));
    }

    // Dieselbe Zielbahn: über A unten ergibt Position 2.
    [Test]
    public void Wenn_die_Karte_aus_einer_anderen_Bahn_ueber_die_untere_Haelfte_der_ersten_kommt_dann_landet_sie_auf_Position_zwei()
    {
        Assert.That(Ablagestellen.Zielposition(0, Kartenhaelfte.Unten, null), Is.EqualTo(2));
    }

    // Dieselbe Zielbahn: über C unten ergibt Position 4, also hinter die letzte.
    [Test]
    public void Wenn_die_Karte_aus_einer_anderen_Bahn_ueber_die_untere_Haelfte_der_letzten_kommt_dann_landet_sie_dahinter()
    {
        Assert.That(Ablagestellen.Zielposition(2, Kartenhaelfte.Unten, null), Is.EqualTo(4));
    }

    // Bahn [A, B, C, D], gezogen wird D (Index 3): über A oben ergibt [D, A, B, C].
    [Test]
    public void Wenn_die_letzte_Karte_derselben_Bahn_vor_die_erste_gezogen_wird_dann_wird_sie_die_erste()
    {
        Assert.That(Ablagestellen.Zielposition(0, Kartenhaelfte.Oben, 3), Is.EqualTo(1));
    }

    // Dieselbe Bahn, D gezogen: über B unten ergibt [A, B, D, C], also Position 3.
    [Test]
    public void Wenn_die_letzte_Karte_derselben_Bahn_hinter_die_zweite_gezogen_wird_dann_ruecken_die_Positionen_um_eins_vor()
    {
        Assert.That(Ablagestellen.Zielposition(1, Kartenhaelfte.Unten, 3), Is.EqualTo(3));
    }

    // Bahn [A, B, C, D], gezogen wird A (Index 0): über C unten ergibt [B, C, A, D], also Position 3.
    [Test]
    public void Wenn_die_erste_Karte_derselben_Bahn_hinter_die_dritte_gezogen_wird_dann_zaehlt_die_Fuge_ohne_sie()
    {
        Assert.That(Ablagestellen.Zielposition(2, Kartenhaelfte.Unten, 0), Is.EqualTo(3));
    }

    // Beide Hälften der gezogenen Karte selbst meinen ihre eigene Stelle: die Reihenfolge
    // darf sich nicht ändern, wenn eine Karte über sich losgelassen wird.
    [Test]
    public void Wenn_eine_Karte_auf_ihrer_eigenen_oberen_Haelfte_abgelegt_wird_dann_bleibt_ihre_Position_dieselbe()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Ablagestellen.Zielposition(0, Kartenhaelfte.Oben, 0), Is.EqualTo(1));
            Assert.That(Ablagestellen.Zielposition(2, Kartenhaelfte.Oben, 2), Is.EqualTo(3));
        });
    }

    [Test]
    public void Wenn_eine_Karte_auf_ihrer_eigenen_unteren_Haelfte_abgelegt_wird_dann_bleibt_ihre_Position_dieselbe()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Ablagestellen.Zielposition(0, Kartenhaelfte.Unten, 0), Is.EqualTo(1));
            Assert.That(Ablagestellen.Zielposition(2, Kartenhaelfte.Unten, 2), Is.EqualTo(3));
        });
    }

    // Zielbahn [A, B, C], der Zug kommt aus einer anderen Bahn auf die Restfläche: Position 4.
    [Test]
    public void Wenn_eine_fremde_Karte_auf_der_Restflaeche_landet_dann_wird_sie_die_letzte_der_Bahn()
    {
        Assert.That(Ablagestellen.ZielpositionAmEnde(3, null), Is.EqualTo(4));
    }

    // Bahn [A, B, C], A wird auf die eigene Restfläche gezogen: [B, C, A], also Position 3.
    [Test]
    public void Wenn_eine_Karte_derselben_Bahn_auf_der_Restflaeche_landet_dann_zaehlt_die_Bahn_ohne_sie()
    {
        Assert.That(Ablagestellen.ZielpositionAmEnde(3, 0), Is.EqualTo(3));
    }

    [Test]
    public void Wenn_die_Zielbahn_leer_ist_dann_wird_die_Karte_ihre_erste()
    {
        Assert.That(Ablagestellen.ZielpositionAmEnde(0, null), Is.EqualTo(1));
    }

    // A, B, C, D in einer Bahn; D wird gezogen. Die fünf Fugen zeigen auf die Positionen, die D
    // nach dem Herausnehmen tatsächlich einnehmen kann — die letzte ist 4, nicht 5.
    [Test]
    public void Wenn_die_letzte_Karte_einer_Bahn_gezogen_wird_dann_bleibt_die_hoechste_Zielposition_die_Kartenzahl()
    {
        var positionen = new[]
        {
            Ablagestellen.Zielposition(0, Kartenhaelfte.Oben, 3),
            Ablagestellen.Zielposition(0, Kartenhaelfte.Unten, 3),
            Ablagestellen.Zielposition(1, Kartenhaelfte.Unten, 3),
            Ablagestellen.Zielposition(2, Kartenhaelfte.Unten, 3),
            Ablagestellen.ZielpositionAmEnde(4, 3),
        };

        Assert.That(positionen, Is.EqualTo(new[] { 1, 2, 3, 4, 4 }));
    }

    // A, B, C, D; A wird gezogen. Vor A und hinter A bedeuten dasselbe: A bleibt an Position 1.
    [Test]
    public void Wenn_die_erste_Karte_einer_Bahn_gezogen_wird_dann_zeigen_die_beiden_Fugen_an_ihr_auf_Position_eins()
    {
        var positionen = new[]
        {
            Ablagestellen.Zielposition(0, Kartenhaelfte.Oben, 0),
            Ablagestellen.Zielposition(0, Kartenhaelfte.Unten, 0),
            Ablagestellen.Zielposition(1, Kartenhaelfte.Unten, 0),
            Ablagestellen.Zielposition(2, Kartenhaelfte.Unten, 0),
            Ablagestellen.ZielpositionAmEnde(4, 0),
        };

        Assert.That(positionen, Is.EqualTo(new[] { 1, 1, 2, 3, 4 }));
    }
}
