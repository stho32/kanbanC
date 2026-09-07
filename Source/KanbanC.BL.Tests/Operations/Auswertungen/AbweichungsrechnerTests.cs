using KanbanC.BL.Operations.Auswertungen;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.BL.Tests.Operations.Auswertungen;

public class AbweichungsrechnerTests
{
    // Die drei Rechenbeispiele der Anforderung, gegen das Band der Karte [I0030].
    private static readonly Zeitband BandDerKarteI0030 = new(38.0m, 44.0m);

    [Test]
    public void Wenn_die_erfasste_Zeit_unter_der_Untergrenze_liegt_dann_steht_sie_unter_dem_Band()
    {
        var abweichung = Abweichungsrechner.Rechne(TimeSpan.FromMinutes(144), BandDerKarteI0030);

        Assert.That(abweichung, Is.EqualTo(new Abweichung(Abweichungslage.UnterDemBand, null)));
    }

    [Test]
    public void Wenn_die_erfasste_Zeit_zwischen_den_Grenzen_liegt_dann_steht_sie_im_Band()
    {
        var abweichung = Abweichungsrechner.Rechne(TimeSpan.FromHours(40), BandDerKarteI0030);

        Assert.That(abweichung, Is.EqualTo(new Abweichung(Abweichungslage.ImBand, null)));
    }

    [Test]
    public void Wenn_die_erfasste_Zeit_die_Obergrenze_ueberschreitet_dann_nennt_die_Abweichung_den_Ueberschuss()
    {
        var abweichung = Abweichungsrechner.Rechne(TimeSpan.FromHours(50), BandDerKarteI0030);

        Assert.That(abweichung, Is.EqualTo(new Abweichung(Abweichungslage.UeberDemBand, 6.0m)));
    }

    // Die Ränder gehören ins Band: genau die Untergrenze und genau die Obergrenze sind keine
    // Abweichung.
    [Test]
    public void Wenn_die_erfasste_Zeit_genau_auf_der_Untergrenze_liegt_dann_steht_sie_im_Band()
    {
        var abweichung = Abweichungsrechner.Rechne(TimeSpan.FromHours(38), BandDerKarteI0030);

        Assert.That(abweichung!.Lage, Is.EqualTo(Abweichungslage.ImBand));
    }

    [Test]
    public void Wenn_die_erfasste_Zeit_genau_auf_der_Obergrenze_liegt_dann_steht_sie_im_Band()
    {
        var abweichung = Abweichungsrechner.Rechne(TimeSpan.FromHours(44), BandDerKarteI0030);

        Assert.That(abweichung!.Lage, Is.EqualTo(Abweichungslage.ImBand));
    }

    // Ohne Band gibt es nichts zu vergleichen: nicht `im Band` und nicht `0`.
    [Test]
    public void Wenn_die_Karte_kein_Sollband_traegt_dann_gibt_es_keine_Abweichung()
    {
        Assert.That(Abweichungsrechner.Rechne(TimeSpan.FromHours(3), null), Is.Null);
    }

    [Test]
    public void Wenn_die_Karte_keinen_Zeiteintrag_hat_dann_steht_sie_unter_dem_Band()
    {
        var abweichung = Abweichungsrechner.Rechne(TimeSpan.Zero, BandDerKarteI0030);

        Assert.That(abweichung, Is.EqualTo(new Abweichung(Abweichungslage.UnterDemBand, null)));
    }

    // Ein Einzelwert der Datei setzt beide Grenzen gleich; das Band ist dann ein Punkt, und jede
    // Minute darüber ist ein Überschuss.
    [Test]
    public void Wenn_das_Band_ein_Einzelwert_ist_dann_ist_jede_Minute_darueber_ein_Ueberschuss()
    {
        var abweichung = Abweichungsrechner.Rechne(TimeSpan.FromMinutes(30), new Zeitband(0.4m, 0.4m));

        Assert.That(abweichung, Is.EqualTo(new Abweichung(Abweichungslage.UeberDemBand, 0.1m)));
    }
}
