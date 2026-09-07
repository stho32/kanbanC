using KanbanC.Contracts.Ereignisse;

namespace KanbanC.WebApi.IntegrationTests.Api;

// Die Ableitung des Wegs aus dem Kopf der Anfrage — eine reine Funktion, geprüft ohne HTTP. Am
// Endpunkt belegen EreignisEndpunkteTests dieselbe Regel über die Leitung; hier stehen die Ränder,
// die eine Anfrage nicht bequem herstellt.
public class WegkopfTests
{
    [Test]
    public void Wenn_der_Kopf_die_Oberflaeche_nennt_dann_ist_der_Weg_Oberflaeche()
    {
        Assert.That(Wegkopf.Aus(Wegkopf.Oberflaechenwert), Is.EqualTo(Ereignisweg.Oberflaeche));
    }

    // Ein Kopf ist keine Zusage, sondern eine Auskunft: Groß- und Kleinschreibung sollen sie nicht
    // zunichtemachen.
    [Test]
    public void Wenn_der_Kopf_die_Oberflaeche_in_anderer_Schreibweise_nennt_dann_gilt_sie_trotzdem()
    {
        Assert.That(Wegkopf.Aus("Oberflaeche"), Is.EqualTo(Ereignisweg.Oberflaeche));
    }

    [Test]
    public void Wenn_der_Kopf_fehlt_dann_gilt_Api()
    {
        Assert.That(Wegkopf.Aus(null), Is.EqualTo(Ereignisweg.Api));
    }

    [Test]
    public void Wenn_der_Kopf_einen_fremden_Wert_traegt_dann_gilt_ebenfalls_Api()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Wegkopf.Aus(""), Is.EqualTo(Ereignisweg.Api));
            Assert.That(Wegkopf.Aus("raumschiff"), Is.EqualTo(Ereignisweg.Api));
        });
    }

    [Test]
    public void Wenn_der_Name_des_Kopfes_gelesen_wird_dann_traegt_er_das_Kuerzel_der_Anwendung()
    {
        Assert.That(Wegkopf.Name, Is.EqualTo("X-KanbanC-Weg"));
    }
}
