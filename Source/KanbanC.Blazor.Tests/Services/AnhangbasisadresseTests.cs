using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Services;

// Die Voreinstellung steht in Program.cs, und der neue Schluessel steht in appsettings.json. Der
// bestehende Schluessel WebApi:BasisAdresse bleibt in Bedeutung und Wirkung unveraendert.
// stil-check: C03 die Ablage ist hier der Pruefgegenstand
public class AnhangbasisadresseTests
{
    [Test]
    public void Wenn_appsettings_gelesen_wird_dann_traegt_es_beide_Schluessel_der_WebApi()
    {
        var einstellungen = File.ReadAllText(Quelltextbaum.BlazorDatei("appsettings.json"));

        Assert.Multiple(() =>
        {
            Assert.That(einstellungen, Does.Contain("\"BasisAdresse\""));
            Assert.That(einstellungen, Does.Contain("\"OeffentlicheBasisAdresse\""));
        });
    }

    [Test]
    public void Wenn_Program_cs_gelesen_wird_dann_faellt_die_oeffentliche_Adresse_auf_die_interne_zurueck()
    {
        var programm = File.ReadAllText(Quelltextbaum.BlazorDatei("Program.cs"));

        Assert.That(programm, Does.Contain("WebApi:OeffentlicheBasisAdresse"));
        Assert.That(programm, Does.Contain("?? webApiBasisAdresse"));
    }

    [Test]
    public void Wenn_die_Basisadresse_weitergereicht_wird_dann_traegt_sie_der_kleine_Wert_unveraendert()
    {
        var basis = new Anhangbasisadresse("http://kanban-rechner:5280/");

        Assert.That(Anhangadresse.Fuer(basis.Adresse, 14, 7), Is.EqualTo("http://kanban-rechner:5280/api/karten/14/anhaenge/7"));
    }
}
