using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

public class AnhangadresseTests
{
    [Test]
    public void Wenn_die_Basisadresse_mit_Schraegstrich_endet_dann_entsteht_eine_absolute_Adresse_ohne_doppelten_Schraegstrich()
    {
        var adresse = Anhangadresse.Fuer("http://localhost:5280/", 14, 7);

        Assert.That(adresse, Is.EqualTo("http://localhost:5280/api/karten/14/anhaenge/7"));
    }

    [Test]
    public void Wenn_die_Basisadresse_ohne_Schraegstrich_endet_dann_entsteht_dieselbe_Adresse()
    {
        var mitSchraegstrich = Anhangadresse.Fuer("http://localhost:5280/", 14, 7);
        var ohneSchraegstrich = Anhangadresse.Fuer("http://localhost:5280", 14, 7);

        Assert.That(ohneSchraegstrich, Is.EqualTo(mitSchraegstrich));
    }

    // Die Adresse zeigt auf die WebApi und nicht auf eine Blazor-Route: sie traegt den Rechner
    // der API und den Weg /api/.
    [Test]
    public void Wenn_die_WebApi_im_LAN_liegt_dann_traegt_die_Adresse_ihren_Rechner_und_nicht_den_der_Oberflaeche()
    {
        var adresse = Anhangadresse.Fuer("http://kanban-rechner:5280/", 14, 7);

        Assert.That(adresse, Does.StartWith("http://kanban-rechner:5280/api/"));
        Assert.That(adresse, Is.EqualTo("http://kanban-rechner:5280/api/karten/14/anhaenge/7"));
    }

    [Test]
    public void Wenn_zwei_Anhaenge_derselben_Karte_gefragt_sind_dann_unterscheiden_sich_ihre_Adressen()
    {
        Assert.That(Anhangadresse.Fuer("http://localhost:5280/", 14, 7), Is.Not.EqualTo(Anhangadresse.Fuer("http://localhost:5280/", 14, 8)));
    }
}
