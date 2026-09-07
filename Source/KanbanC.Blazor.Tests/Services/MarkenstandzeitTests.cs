using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Die Standzeit der Einflugmarke kommt aus der Konfiguration, damit ein Testlauf sie kürzen kann.
// Ohne gesetzten Wert gelten die zehn Sekunden der Größenordnung; ein unbrauchbarer Wert ist keine
// Zurückweisung, sondern derselbe Vorgabewert — eine Anwendung, die wegen einer Standzeit nicht
// startet, wäre die schlechtere Antwort.
public class MarkenstandzeitTests
{
    [Test]
    public void Wenn_nichts_gesetzt_ist_dann_steht_die_Marke_etwa_zehn_Sekunden()
    {
        Assert.That(Markenstandzeit.Aus(null).Dauer, Is.EqualTo(TimeSpan.FromSeconds(10)));
    }

    [Test]
    public void Wenn_der_Wert_leer_ist_dann_gilt_ebenfalls_die_Vorgabe()
    {
        Assert.That(Markenstandzeit.Aus("").Dauer, Is.EqualTo(Markenstandzeit.Vorgabe));
    }

    [Test]
    public void Wenn_eine_Zahl_gesetzt_ist_dann_gilt_sie_als_Sekunden()
    {
        Assert.That(Markenstandzeit.Aus("1.5").Dauer, Is.EqualTo(TimeSpan.FromSeconds(1.5)));
    }

    [Test]
    public void Wenn_der_Wert_unlesbar_oder_nicht_positiv_ist_dann_gilt_die_Vorgabe()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Markenstandzeit.Aus("bald").Dauer, Is.EqualTo(Markenstandzeit.Vorgabe));
            Assert.That(Markenstandzeit.Aus("0").Dauer, Is.EqualTo(Markenstandzeit.Vorgabe));
            Assert.That(Markenstandzeit.Aus("-3").Dauer, Is.EqualTo(Markenstandzeit.Vorgabe));
        });
    }
}
