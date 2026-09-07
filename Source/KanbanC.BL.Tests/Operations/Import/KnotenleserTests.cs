using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

public class KnotenleserTests
{
    [Test]
    public void Wenn_zwoelf_gueltige_Zellen_vorliegen_dann_entsteht_ein_Knoten_mit_allen_Textspalten()
    {
        var zellen = Zellen("I0001", "Interaction", "D0001", "Board anlegen", "gruen", "Fertig", "Ein → Aus", "2", "Stufe", "I0021", "R00001", "Notiz");

        var lesung = Knotenleser.Lies(zellen, 17);

        Assert.That(lesung.WurdeUebersprungen, Is.False);
        var knoten = lesung.Knoten;
        Assert.Multiple(() =>
        {
            Assert.That(knoten.Id, Is.EqualTo("I0001"));
            Assert.That(knoten.Ebene, Is.EqualTo(Wbsebene.Interaction));
            Assert.That(knoten.Eltern, Is.EqualTo("D0001"));
            Assert.That(knoten.Name, Is.EqualTo("Board anlegen"));
            Assert.That(knoten.Status, Is.EqualTo(Wbsstatus.Gruen));
            Assert.That(knoten.Fertigkriterium, Is.EqualTo("Fertig"));
            Assert.That(knoten.Fluss, Is.EqualTo("Ein → Aus"));
            Assert.That(knoten.Aufwand, Is.EqualTo("2"));
            Assert.That(knoten.Ausbaustufe, Is.EqualTo("Stufe"));
            Assert.That(knoten.Braucht, Is.EqualTo("I0021"));
            Assert.That(knoten.Requirement, Is.EqualTo("R00001"));
            Assert.That(knoten.Notiz, Is.EqualTo("Notiz"));
            Assert.That(knoten.Zeilennummer, Is.EqualTo(17));
        });
    }

    [Test]
    public void Wenn_die_Zellenzahl_abweicht_dann_wird_die_Zeile_mit_Nummer_und_gefundener_Zahl_gemeldet()
    {
        var zellen = new Wbszellen(["I0001", "Interaction", "D0001"]);

        var lesung = Knotenleser.Lies(zellen, 42);

        Assert.That(lesung.WurdeUebersprungen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(lesung.Uebersprungene.Zeilennummer, Is.EqualTo(42));
            Assert.That(lesung.Uebersprungene.Kennung, Is.EqualTo("Zeile 42"));
            Assert.That(lesung.Uebersprungene.Grund, Does.Contain("3 Zellen statt 12"));
        });
    }

    [Test]
    public void Wenn_die_Ebene_unbekannt_ist_dann_steht_der_gefundene_Wert_im_Grund()
    {
        var zellen = Zellen("M0001", "Meilenstein", "D0001", "Name", "gruen", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        var lesung = Knotenleser.Lies(zellen, 7);

        Assert.That(lesung.WurdeUebersprungen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(lesung.Uebersprungene.Kennung, Is.EqualTo("M0001"));
            Assert.That(lesung.Uebersprungene.Grund, Does.Contain("Meilenstein"));
            Assert.That(lesung.Uebersprungene.Grund, Does.Contain("Application, Dialog, Interaction, Feature und Bubble"));
        });
    }

    [Test]
    public void Wenn_der_Status_unbekannt_ist_dann_steht_der_gefundene_Wert_im_Grund()
    {
        var zellen = Zellen("I0001", "Interaction", "D0001", "Name", "fertig", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        var lesung = Knotenleser.Lies(zellen, 9);

        Assert.That(lesung.WurdeUebersprungen, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(lesung.Uebersprungene.Grund, Does.Contain("fertig"));
            Assert.That(lesung.Uebersprungene.Grund, Does.Contain("rot, gelb, gruen, bestehend, option, ausbau und verworfen"));
        });
    }

    [Test]
    public void Wenn_der_Name_leer_ist_dann_wird_die_Zeile_uebersprungen()
    {
        var zellen = Zellen("I0001", "Interaction", "D0001", string.Empty, "gruen", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        var lesung = Knotenleser.Lies(zellen, 11);

        Assert.That(lesung.WurdeUebersprungen, Is.True);
        Assert.That(lesung.Uebersprungene.Grund, Does.Contain("keinen Namen"));
    }

    [Test]
    public void Wenn_die_Kennung_fehlt_dann_wird_die_Zeile_unter_ihrer_Zeilennummer_gemeldet()
    {
        var zellen = Zellen(string.Empty, "Interaction", "D0001", "Name", "gruen", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        var lesung = Knotenleser.Lies(zellen, 13);

        Assert.That(lesung.WurdeUebersprungen, Is.True);
        Assert.That(lesung.Uebersprungene.Kennung, Is.EqualTo("Zeile 13"));
    }

    // bestehend ist ein echter Statuswert und keine kaputte Zeile — der Skill
    // work-breakdown-structure führt ihn, und die echte Planungsdatei trägt ihn einmal.
    [Test]
    public void Wenn_der_Status_bestehend_lautet_dann_wird_die_Zeile_gelesen()
    {
        var zellen = Zellen("B0380", "Bubble", "F0047", "Name", "bestehend", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        var lesung = Knotenleser.Lies(zellen, 3);

        Assert.That(lesung.WurdeUebersprungen, Is.False);
        Assert.That(lesung.Knoten.Status, Is.EqualTo(Wbsstatus.Bestehend));
    }

    private static Wbszellen Zellen(params string[] werte)
    {
        return new Wbszellen(werte);
    }
}
