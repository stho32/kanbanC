using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Zwei Einstellungen der Oberfläche kommen als Sekundenzahl aus der Konfiguration — die Standzeit
// der Einflugmarke und die Pause der Ereignisleitung. Beide lesen dieselbe Regel: leer, unlesbar
// oder nicht positiv heißt „nicht gesetzt".
public class SekundenwertTests
{
    private static readonly TimeSpan Vorgabe = TimeSpan.FromSeconds(7);

    [Test]
    public void Wenn_eine_Zahl_gesetzt_ist_dann_gilt_sie_als_Sekunden()
    {
        Assert.That(Sekundenwert.Aus("1.5", Vorgabe), Is.EqualTo(TimeSpan.FromSeconds(1.5)));
    }

    [Test]
    public void Wenn_nichts_gesetzt_ist_dann_gilt_die_Vorgabe()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Sekundenwert.Aus(null, Vorgabe), Is.EqualTo(Vorgabe));
            Assert.That(Sekundenwert.Aus("", Vorgabe), Is.EqualTo(Vorgabe));
        });
    }

    [Test]
    public void Wenn_der_Wert_unlesbar_oder_nicht_positiv_ist_dann_gilt_ebenfalls_die_Vorgabe()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Sekundenwert.Aus("bald", Vorgabe), Is.EqualTo(Vorgabe));
            Assert.That(Sekundenwert.Aus("0", Vorgabe), Is.EqualTo(Vorgabe));
            Assert.That(Sekundenwert.Aus("-3", Vorgabe), Is.EqualTo(Vorgabe));
        });
    }
}
