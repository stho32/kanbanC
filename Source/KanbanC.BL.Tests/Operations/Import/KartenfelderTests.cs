using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;
using KanbanC.BL.Tests.TestHelpers;

namespace KanbanC.BL.Tests.Operations.Import;

public class KartenfelderTests
{
    [Test]
    public void Wenn_ein_Knoten_zur_Karte_wird_dann_lautet_der_Titel_ID_in_Klammern_und_dann_der_Name()
    {
        var titel = Kartenfelder.Titel(Knoten("I0001", "Board anlegen"));

        Assert.That(titel, Is.EqualTo("[I0001] Board anlegen"));
    }

    [Test]
    public void Wenn_ein_Knoten_zur_Teilaufgabe_wird_dann_steht_die_ID_vorn_ohne_Klammern()
    {
        var text = Kartenfelder.Teilaufgabentext(Knoten("B0001", "Standardspalten erzeugen"));

        Assert.That(text, Is.EqualTo("B0001 Standardspalten erzeugen"));
    }

    [Test]
    public void Wenn_Fertigkriterium_Notiz_Braucht_und_Requirement_dastehen_dann_stehen_sie_in_der_Beschreibung()
    {
        var knoten = Knoten("I0001", "Board anlegen") with
        {
            Fertigkriterium = "Ein neues Board entsteht",
            Braucht = "I0021",
            Requirement = "R00023",
            Notiz = "Aus Vision",
        };

        var beschreibung = Kartenfelder.Beschreibung(knoten);

        Assert.Multiple(() =>
        {
            Assert.That(beschreibung, Does.Contain("Ein neues Board entsteht"));
            Assert.That(beschreibung, Does.Contain("Anforderung: R00023"));
            Assert.That(beschreibung, Does.Contain("Braucht: I0021"));
            Assert.That(beschreibung, Does.Contain("Notiz: Aus Vision"));
        });
    }

    // Aufwand, Eingabe → Ausgabe und Ausbaustufe sind Entwurfsangaben der Bubble-Ebene; auf dem
    // Board liest sie niemand, und für den Aufwand gibt es im Bestand keine Sollzeit.
    [Test]
    public void Wenn_ein_Knoten_Aufwand_Fluss_und_Ausbaustufe_traegt_dann_kommen_sie_nicht_in_die_Beschreibung()
    {
        var knoten = Knoten("B0001", "Standardspalten erzeugen") with
        {
            Fertigkriterium = "Test gruen",
            Fluss = "— → StandardspaltenVorlage → 3 Spalten",
            Aufwand = "2",
            Ausbaustufe = "Stufe 1",
        };

        var beschreibung = Kartenfelder.Beschreibung(knoten);

        Assert.Multiple(() =>
        {
            Assert.That(beschreibung, Does.Not.Contain("StandardspaltenVorlage"));
            Assert.That(beschreibung, Does.Not.Contain("Stufe 1"));
            Assert.That(beschreibung, Is.EqualTo("Test gruen"));
        });
    }

    [Test]
    public void Wenn_ein_Knoten_nichts_ausser_Namen_traegt_dann_bleibt_die_Beschreibung_leer()
    {
        var beschreibung = Kartenfelder.Beschreibung(Knoten("I0001", "Board anlegen"));

        Assert.That(beschreibung, Is.Null);
    }

    // Die Beschreibung ist das einzige Kartenfeld ohne Längengrenze — und genau deshalb landet die
    // 8.000-Zeichen-Notiz dort und nirgends sonst.
    [Test]
    public void Wenn_eine_Notiz_8000_Zeichen_traegt_dann_steht_sie_ungekuerzt_in_der_Beschreibung()
    {
        var lange = new string('x', 8000);
        var knoten = Knoten("I0027", "Zeiten sehen") with { Notiz = lange };

        var beschreibung = Kartenfelder.Beschreibung(knoten);

        Assert.That(beschreibung, Does.Contain(lange));
    }

    private static Wbsknoten Knoten(string id, string name)
    {
        return Probewbs.Knoten(id, Wbsebene.Interaction, "D0001", name, Wbsstatus.Gruen, 1);
    }
}
