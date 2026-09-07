using KanbanC.Blazor.Services;
using KanbanC.Contracts.Auswertungen;

namespace KanbanC.Blazor.Tests.Services;

public class AbweichungswortTests
{
    [Test]
    public void Wenn_die_Zeit_unter_dem_Band_liegt_dann_steht_unter_dem_Band_da()
    {
        Assert.That(Abweichungswort.AlsText(new Abweichung(Abweichungslage.UnterDemBand, null)), Is.EqualTo("unter dem Band"));
    }

    [Test]
    public void Wenn_die_Zeit_im_Band_liegt_dann_steht_im_Band_da()
    {
        Assert.That(Abweichungswort.AlsText(new Abweichung(Abweichungslage.ImBand, null)), Is.EqualTo("im Band"));
    }

    [Test]
    public void Wenn_die_Zeit_ueber_dem_Band_liegt_dann_nennt_der_Satz_den_Ueberschuss_in_Stunden()
    {
        Assert.That(Abweichungswort.AlsText(new Abweichung(Abweichungslage.UeberDemBand, 6.0m)), Is.EqualTo("über dem Band um 6,0 h"));
    }

    // Ohne Sollband gibt es keine Abweichung — derselbe Gedankenstrich wie in der Soll-Spalte und
    // nicht „im Band".
    [Test]
    public void Wenn_es_keine_Abweichung_gibt_dann_steht_ein_Gedankenstrich_da()
    {
        Assert.That(Abweichungswort.AlsText(null), Is.EqualTo("—"));
    }
}
