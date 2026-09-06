using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Tests.Services;

// Der Blazor-Host ist von hier aus nicht startbar — sein Program.cs traegt keine sichtbare
// Einstiegsklasse. Geprueft wird deshalb die Datei selbst, wie in den Gestaltungspruefungen: dass
// die Nachrichtengrenze des SignalR-Kreislaufs ueberhaupt gesetzt ist und dass sie aus derselben
// Konstante kommt wie Validator und WebApi-Route.
// Dass die Grenze im laufenden Betrieb wirklich traegt, belegt die E2E-Strecke mit einer
// 41-kB-Datei — die Voreinstellung von 32 KB liesse sie nicht durch.
// stil-check: C03 die Ablage ist hier der Pruefgegenstand
public class AnhangsgrenzeImKreislaufTests
{
    [Test]
    public void Wenn_Program_cs_gelesen_wird_dann_setzt_es_die_Nachrichtengrenze_des_Kreislaufs_aus_der_Anhangsgrenze()
    {
        var programm = File.ReadAllText(Quelltextbaum.BlazorDatei("Program.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(programm, Does.Contain("MaximumReceiveMessageSize"));
            Assert.That(programm, Does.Contain("Anhangsgrenze.HoechsteDateigroesse"));
        });
    }

    // Keine zweite Zahl: die Grenze steht nirgends in der Oberflaeche als Literal.
    [Test]
    public void Wenn_Program_cs_gelesen_wird_dann_steht_die_Zahl_selbst_nicht_darin()
    {
        var programm = File.ReadAllText(Quelltextbaum.BlazorDatei("Program.cs"));

        Assert.That(programm, Does.Not.Contain(Anhangsgrenze.HoechsteDateigroesse.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }
}
