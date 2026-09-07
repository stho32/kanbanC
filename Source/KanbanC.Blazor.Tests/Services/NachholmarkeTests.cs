using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Die einzige Zusage der Nachholmarke ist eine Zusage über das, was sie **nicht** sagt: ein
// Vergleich kennt weder den Urheber noch den Zeitpunkt, und eine erfundene Genauigkeit wäre die
// teuerste Art, eine Fußzeile zu füllen. Über den Browser ist eine Abwesenheit nicht zu belegen.
public class NachholmarkeTests
{
    [Test]
    public void Wenn_die_Marke_gelesen_wird_dann_nennt_sie_den_Anlass_und_sonst_nichts()
    {
        Assert.That(Nachholmarke.Wortlaut, Is.EqualTo("geändert, während die Verbindung weg war"));
    }

    // Weder eine Uhrzeit noch eine relative Zeitangabe: der Zeitpunkt steht einmal im Band.
    [Test]
    public void Wenn_die_Marke_gelesen_wird_dann_traegt_sie_keine_Zeitangabe()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Nachholmarke.Wortlaut, Does.Not.Match(@"\d"), "Eine Zahl in der Marke wäre geraten.");
            Assert.That(Nachholmarke.Wortlaut, Does.Not.Contain("vor "));
            Assert.That(Nachholmarke.Wortlaut, Does.Not.Contain("gerade eben"));
        });
    }

    // Kein Name und kein Weg: die Einflugmarke trennt beides mit „ · ", die Nachholmarke hat
    // nichts zu trennen.
    [Test]
    public void Wenn_die_Marke_gelesen_wird_dann_traegt_sie_keinen_Urheber()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Nachholmarke.Wortlaut, Does.Not.Contain(" · "));
            Assert.That(Nachholmarke.Wortlaut, Does.Not.Contain("über die API"));
        });
    }
}
