using KanbanC.BL.Operations.Boardimport;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.Operations.Boardimport;

// **Der Preis der Kontributoren-Entscheidung, vor dem Schreiben sichtbar**: welche Namen ein
// zweites Mal entstehen. Zusammengefuehrt wird nichts.
public class NamensdublettenTests
{
    [Test]
    public void Wenn_ein_Name_hier_schon_steht_dann_nennt_die_Vorschau_ihn()
    {
        var doppelte = Namensdubletten.Finde([Person(7, "Stefan"), Person(11, "Alt-Kollege")], [Person(7, "Stefan"), Person(8, "Zora")]);

        Assert.That(doppelte, Is.EqualTo(new[] { "Stefan" }));
    }

    [Test]
    public void Wenn_kein_Name_hier_steht_dann_bleibt_die_Liste_leer()
    {
        var doppelte = Namensdubletten.Finde([Person(11, "Alt-Kollege")], [Person(7, "Stefan"), Person(8, "Zora")]);

        Assert.That(doppelte, Is.Empty);
    }

    // Der Name steht **so oft, wie er entsteht**: zwei gleichnamige Personen in der Datei ergeben
    // zwei Zeilen in der Personenliste und zwei Nennungen in der Vorschau.
    [Test]
    public void Wenn_die_Datei_zwei_gleiche_Namen_traegt_dann_steht_der_Name_zweimal()
    {
        var doppelte = Namensdubletten.Finde([Person(7, "Stefan"), Person(9, "Stefan")], [Person(7, "Stefan")]);

        Assert.That(doppelte, Is.EqualTo(new[] { "Stefan", "Stefan" }));
    }

    // Gross- und Kleinschreibung trennt nicht: „stefan" neben „Stefan" ist genau die
    // Verwechslung, vor der der Hinweis warnt.
    [Test]
    public void Wenn_sich_die_Namen_nur_in_der_Schreibweise_unterscheiden_dann_gilt_der_Name_als_doppelt()
    {
        var doppelte = Namensdubletten.Finde([Person(7, "stefan")], [Person(7, "Stefan")]);

        Assert.That(doppelte, Is.EqualTo(new[] { "stefan" }));
    }

    [Test]
    public void Wenn_es_hier_noch_keine_Person_gibt_dann_bleibt_die_Liste_leer()
    {
        var doppelte = Namensdubletten.Finde([Person(7, "Stefan")], []);

        Assert.That(doppelte, Is.Empty);
    }

    private static Kontributor Person(long kontributorId, string name)
    {
        return new Kontributor(kontributorId, name, Kontributorart.Mensch, null);
    }
}
