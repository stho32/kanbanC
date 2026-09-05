using KanbanC.Blazor.Services;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Tests.Services;

public class TeilaufgabenfortschrittTests
{
    // Das Rechenbeispiel der Anforderung: 4 Teilaufgaben, 2 abgehakt → „2 von 4", Balken bei 50 %.
    [Test]
    public void Wenn_zwei_von_vier_abgehakt_sind_dann_lautet_der_Text_2_von_4_und_der_Balken_steht_bei_50_Prozent()
    {
        var teilaufgaben = Teilaufgaben(true, true, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(Teilaufgabenfortschritt.AlsText(teilaufgaben), Is.EqualTo("2 von 4"));
            Assert.That(Teilaufgabenfortschritt.AlsProzent(teilaufgaben), Is.EqualTo(50));
        });
    }

    // Das zweite Rechenbeispiel: alle vier abgehakt → „4 von 4", Balken voll.
    [Test]
    public void Wenn_alle_abgehakt_sind_dann_lautet_der_Text_4_von_4_und_der_Balken_steht_bei_100_Prozent()
    {
        var teilaufgaben = Teilaufgaben(true, true, true, true);

        Assert.Multiple(() =>
        {
            Assert.That(Teilaufgabenfortschritt.AlsText(teilaufgaben), Is.EqualTo("4 von 4"));
            Assert.That(Teilaufgabenfortschritt.AlsProzent(teilaufgaben), Is.EqualTo(100));
        });
    }

    // Das Rechenbeispiel aus US-2: eine von zwei abgehakt.
    [Test]
    public void Wenn_eine_von_zwei_abgehakt_ist_dann_lautet_der_Text_1_von_2()
    {
        var teilaufgaben = Teilaufgaben(false, true);

        Assert.Multiple(() =>
        {
            Assert.That(Teilaufgabenfortschritt.AlsText(teilaufgaben), Is.EqualTo("1 von 2"));
            Assert.That(Teilaufgabenfortschritt.AlsProzent(teilaufgaben), Is.EqualTo(50));
        });
    }

    [Test]
    public void Wenn_keine_abgehakt_ist_dann_steht_der_Balken_bei_0_Prozent()
    {
        var teilaufgaben = Teilaufgaben(false, false);

        Assert.Multiple(() =>
        {
            Assert.That(Teilaufgabenfortschritt.AlsText(teilaufgaben), Is.EqualTo("0 von 2"));
            Assert.That(Teilaufgabenfortschritt.AlsProzent(teilaufgaben), Is.EqualTo(0));
        });
    }

    // Der Anteil wird abgerundet: ein Balken, der bei einer von dreien schon auf 34 % stuende,
    // sagte mehr, als getan ist.
    [Test]
    public void Wenn_eine_von_drei_abgehakt_ist_dann_steht_der_Balken_bei_33_Prozent()
    {
        var teilaufgaben = Teilaufgaben(true, false, false);

        Assert.That(Teilaufgabenfortschritt.AlsProzent(teilaufgaben), Is.EqualTo(33));
    }

    // Die leere Liste zeigt der Abschnitt als „Keine Teilaufgaben · anlegen" und nicht als
    // „0 von 0". Gerechnet wird sie trotzdem, damit die Division nicht durch null geht.
    [Test]
    public void Wenn_die_Liste_leer_ist_dann_steht_der_Balken_bei_0_Prozent_ohne_Division_durch_null()
    {
        Assert.That(Teilaufgabenfortschritt.AlsProzent([]), Is.EqualTo(0));
    }

    private static IReadOnlyList<Teilaufgabe> Teilaufgaben(params bool[] abhakstaende)
    {
        return abhakstaende
            .Select((abgehakt, stelle) => new Teilaufgabe(stelle + 1, $"Schritt {stelle + 1}", stelle + 1, abgehakt))
            .ToList();
    }
}
