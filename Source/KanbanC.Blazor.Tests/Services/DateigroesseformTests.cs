using KanbanC.Blazor.Services;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Tests.Services;

public class DateigroesseformTests
{
    // Die Rechenbeispiele der Anforderung, eins nach dem anderen.
    [TestCase(0L, "0 kB")]
    [TestCase(1000L, "1 kB")]
    [TestCase(41000L, "41 kB")]
    [TestCase(118000L, "118 kB")]
    [TestCase(1200000L, "1,2 MB")]
    [TestCase(10485760L, "10,5 MB")]
    public void Wenn_eine_Groesse_geformt_wird_dann_steht_dort_der_gezeichnete_Text(long dateigroesse, string erwartet)
    {
        Assert.That(Dateigroesseform.AlsText(dateigroesse), Is.EqualTo(erwartet));
    }

    // kB ohne Nachkommastelle, MB mit genau einer.
    [Test]
    public void Wenn_die_Groesse_unter_einem_Megabyte_liegt_dann_traegt_der_Text_keine_Nachkommastelle()
    {
        Assert.That(Dateigroesseform.AlsText(999999), Does.Not.Contain(","));
        Assert.That(Dateigroesseform.AlsText(999999), Does.EndWith(" kB"));
    }

    [Test]
    public void Wenn_die_Groesse_genau_ein_Megabyte_ist_dann_wechselt_die_Einheit()
    {
        Assert.That(Dateigroesseform.AlsText(1000000), Is.EqualTo("1,0 MB"));
    }

    // Das Komma steht in der Zeichnung; ein Punkt waere die Kultur des Servers und nicht die
    // Gestaltungsvorgabe.
    [Test]
    public void Wenn_eine_Groesse_in_Megabyte_geformt_wird_dann_trennt_ein_Komma_und_kein_Punkt()
    {
        Assert.That(Dateigroesseform.AlsText(1200000), Does.Not.Contain("."));
    }

    [Test]
    public void Wenn_die_Obergrenze_geformt_wird_dann_bleibt_sie_lesbar()
    {
        Assert.That(Dateigroesseform.AlsText(Anhangsgrenze.HoechsteDateigroesse), Is.EqualTo("10,5 MB"));
    }
}
