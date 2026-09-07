using KanbanC.Blazor.Services;
using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Tests.Services;

// Vier Marken, eine je Zeile: `+` angelegt, `~` geändert, `?` nicht mehr in der Datei, `!` eine
// Zeile mit Grund.
public class ImportzeilenmarkeTests
{
    [Test]
    public void Wenn_eine_Karte_angelegt_wird_dann_traegt_ihre_Zeile_das_Pluszeichen()
    {
        Assert.That(Importzeilenmarke.Fuer(Zeile(Importwirkung.Angelegt, grund: null)), Is.EqualTo("+"));
    }

    [Test]
    public void Wenn_eine_Karte_nachgezogen_wird_dann_traegt_ihre_Zeile_die_Tilde()
    {
        Assert.That(Importzeilenmarke.Fuer(Zeile(Importwirkung.Geaendert, grund: null)), Is.EqualTo("~"));
    }

    // Eine verwaiste Karte trägt **immer** einen Grund und bekäme sonst nie ihr eigenes Zeichen.
    [Test]
    public void Wenn_eine_Karte_nicht_mehr_in_der_Datei_steht_dann_traegt_ihre_Zeile_das_Fragezeichen()
    {
        Assert.That(Importzeilenmarke.Fuer(Zeile(Importwirkung.Verwaist, "steht nicht mehr in der Datei")), Is.EqualTo("?"));
    }

    [Test]
    public void Wenn_an_einer_Zeile_ein_Grund_steht_dann_traegt_sie_das_Ausrufezeichen()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Importzeilenmarke.Fuer(Zeile(Importwirkung.Uebersprungen, "Der Kartentitel misst 1.200 Zeichen.")), Is.EqualTo("!"));
            Assert.That(Importzeilenmarke.Fuer(Zeile(Importwirkung.Geaendert, "1 Abhakung zurückgenommen")), Is.EqualTo("!"));
            Assert.That(Importzeilenmarke.Fuer(Zeile(Importwirkung.Angelegt, "ähnlich zu WBS-08")), Is.EqualTo("!"));
        });
    }

    [Test]
    public void Wenn_eine_Zeile_nichts_zu_sagen_hat_dann_traegt_sie_keine_Marke()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Importzeilenmarke.Fuer(Zeile(Importwirkung.Unveraendert, grund: null)), Is.Empty);
            Assert.That(Importzeilenmarke.Fuer(Zeile(Importwirkung.Etikett, grund: null)), Is.Empty);
            Assert.That(Importzeilenmarke.Fuer(Zeile(Importwirkung.Teilaufgabe, grund: null)), Is.Empty);
            Assert.That(Importzeilenmarke.Fuer(Zeile(Importwirkung.Zielboard, grund: null)), Is.Empty);
        });
    }

    private static Importzeile Zeile(Importwirkung wirkung, string? grund)
    {
        return new Importzeile("I0031", "Interaction", wirkung, grund, Kartennummer: null);
    }
}
