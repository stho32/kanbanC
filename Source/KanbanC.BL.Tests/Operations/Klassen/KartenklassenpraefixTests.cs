using KanbanC.BL.Operations.Klassen;

namespace KanbanC.BL.Tests.Operations.Klassen;

public class KartenklassenpraefixTests
{
    [Test]
    public void Wenn_das_Praefix_Raender_traegt_dann_fallen_sie_beim_Normalisieren_weg()
    {
        var normalisiert = Kartenklassenpraefix.Normalisiert("  WBS-  ");

        Assert.That(normalisiert, Is.EqualTo("WBS-"));
    }

    [Test]
    public void Wenn_das_Praefix_kleingeschrieben_ist_dann_schreibt_die_Normalisierung_es_nicht_um()
    {
        var normalisiert = Kartenklassenpraefix.Normalisiert("wbs-");

        Assert.That(normalisiert, Is.EqualTo("wbs-"));
    }

    [Test]
    public void Wenn_sich_zwei_Praefixe_nur_in_der_Schreibweise_unterscheiden_dann_gelten_sie_als_gleich()
    {
        var sindGleich = Kartenklassenpraefix.SindGleich("WBS-", "wbs-");

        Assert.That(sindGleich, Is.True);
    }

    [Test]
    public void Wenn_sich_zwei_Praefixe_nur_an_den_Raendern_unterscheiden_dann_gelten_sie_als_gleich()
    {
        var sindGleich = Kartenklassenpraefix.SindGleich("WBS-", " WBS- ");

        Assert.That(sindGleich, Is.True);
    }

    // Der Trenner ist Teil des Praefix: WBS- und WBS_ erzeugen verschiedene Nummern und sind
    // deshalb verschiedene Praefixe.
    [Test]
    public void Wenn_sich_zwei_Praefixe_im_Trenner_unterscheiden_dann_gelten_sie_als_verschieden()
    {
        var sindGleich = Kartenklassenpraefix.SindGleich("WBS-", "WBS_");

        Assert.That(sindGleich, Is.False);
    }
}
