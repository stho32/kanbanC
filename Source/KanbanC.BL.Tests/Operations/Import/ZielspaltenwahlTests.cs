using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

public class ZielspaltenwahlTests
{
    private static readonly IReadOnlyList<Importspalte> Bahnen =
    [
        new Importspalte(11, "In Arbeit", 2, false),
        new Importspalte(10, "Bereit", 1, false),
        new Importspalte(12, "Erledigt", 3, true),
    ];

    [Test]
    public void Wenn_der_Knoten_gruen_ist_dann_entsteht_die_Karte_in_der_Abschlussspalte()
    {
        var spalte = Zielspaltenwahl.Fuer(Wbsstatus.Gruen, Bahnen);

        Assert.That(spalte?.Bezeichnung, Is.EqualTo("Erledigt"));
    }

    // bestehend zählt wie gruen — der Skill work-breakdown-structure rechnet es so, und in der
    // echten Planungsdatei trägt genau eine Zeile diesen Status.
    [Test]
    public void Wenn_der_Knoten_bestehend_ist_dann_entsteht_die_Karte_ebenfalls_in_der_Abschlussspalte()
    {
        var spalte = Zielspaltenwahl.Fuer(Wbsstatus.Bestehend, Bahnen);

        Assert.That(spalte?.Bezeichnung, Is.EqualTo("Erledigt"));
    }

    [Test]
    public void Wenn_der_Knoten_rot_ist_dann_entsteht_die_Karte_in_der_ersten_Bahn_nach_Position()
    {
        var spalte = Zielspaltenwahl.Fuer(Wbsstatus.Rot, Bahnen);

        Assert.That(spalte?.Bezeichnung, Is.EqualTo("Bereit"));
    }

    // Zwei Ziele und nicht drei: gelb bekommt kein eigenes, weil IstAbschlussspalte die einzige
    // markierte Spalteneigenschaft ist. Der Preis ist ein Knoten, der einmal von Hand wandert.
    [Test]
    public void Wenn_der_Knoten_gelb_ist_dann_entsteht_die_Karte_ebenfalls_in_der_ersten_Bahn()
    {
        var spalte = Zielspaltenwahl.Fuer(Wbsstatus.Gelb, Bahnen);

        Assert.That(spalte?.Bezeichnung, Is.EqualTo("Bereit"));
    }

    [Test]
    public void Wenn_das_Board_keine_Abschlussspalte_markiert_hat_dann_entsteht_auch_die_gruene_Karte_in_der_ersten_Bahn()
    {
        IReadOnlyList<Importspalte> ohneAbschluss = [new Importspalte(10, "Bereit", 1, false), new Importspalte(11, "Läuft", 2, false)];

        var spalte = Zielspaltenwahl.Fuer(Wbsstatus.Gruen, ohneAbschluss);

        Assert.Multiple(() =>
        {
            Assert.That(spalte?.Bezeichnung, Is.EqualTo("Bereit"));
            Assert.That(spalte?.IstAbschlussspalte, Is.False);
        });
    }

    [Test]
    public void Wenn_das_Board_keine_Bahn_hat_dann_gibt_es_keine_Zielspalte()
    {
        var spalte = Zielspaltenwahl.Fuer(Wbsstatus.Rot, []);

        Assert.That(spalte, Is.Null);
    }
}
