using KanbanC.BL.Models.Boards;
using KanbanC.BL.Operations.Boards;

namespace KanbanC.BL.Tests.Operations.Boards;

public class StandardspaltenVorlageTests
{
    [Test]
    public void Wenn_ein_neues_Board_entsteht_dann_hat_es_drei_Spalten()
    {
        var spalten = StandardspaltenVorlage.FuerNeuesBoard();

        Assert.That(spalten.SpaltenAnzahl, Is.EqualTo(3));
    }

    [Test]
    public void Wenn_ein_neues_Board_entsteht_dann_stehen_die_Spalten_in_fester_Reihenfolge()
    {
        var spalten = StandardspaltenVorlage.FuerNeuesBoard();

        Assert.Multiple(() =>
        {
            Assert.That(spalten[0], Is.EqualTo(new Spaltenvorlage("Zu erledigen", 1, false, null)));
            Assert.That(spalten[1], Is.EqualTo(new Spaltenvorlage("In Arbeit", 2, false, null)));
            Assert.That(spalten[2].Bezeichnung, Is.EqualTo("Erledigt"));
            Assert.That(spalten[2].Position, Is.EqualTo(3));
        });
    }

    [Test]
    public void Wenn_ein_neues_Board_entsteht_dann_ist_genau_Erledigt_die_Abschlussspalte_mit_Anzeigegrenze_20()
    {
        var spalten = StandardspaltenVorlage.FuerNeuesBoard();

        var abschlussspalten = new List<Spaltenvorlage>();
        foreach (var spalte in spalten)
        {
            if (spalte.IstAbschlussspalte)
            {
                abschlussspalten.Add(spalte);
            }
        }

        Assert.That(abschlussspalten, Has.Count.EqualTo(1));
        Assert.That(abschlussspalten[0], Is.EqualTo(new Spaltenvorlage("Erledigt", 3, true, 20)));
    }
}
