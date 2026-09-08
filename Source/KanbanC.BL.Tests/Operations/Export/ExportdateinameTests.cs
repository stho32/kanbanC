using KanbanC.BL.Operations.Export;

namespace KanbanC.BL.Tests.Operations.Export;

// Der Name nennt Board und Tag, damit zwei Ausleitungen desselben Boards nebeneinander liegen
// können.
public class ExportdateinameTests
{
    private static readonly DateOnly Achter = new(2026, 9, 8);

    [Test]
    public void Wenn_ein_Board_ausgeleitet_wird_dann_nennt_der_Dateiname_Board_und_Tag()
    {
        var name = Exportdateiname.Fuer("KanbanC — Release 2", Achter);

        Assert.That(name, Is.EqualTo("kanbanc-release-2-2026-09-08.kanbanc.json"));
    }

    // Nicht tragbare Zeichen werden **ersetzt** und nicht weggelassen: sonst bekämen zwei
    // verschiedene Boards denselben Namen.
    [Test]
    public void Wenn_ein_Boardname_ein_nicht_tragbares_Zeichen_traegt_dann_wird_es_ersetzt_und_nicht_weggelassen()
    {
        var mitSchraegstrich = Exportdateiname.Fuer("Release 1/2", Achter);
        var ohneSchraegstrich = Exportdateiname.Fuer("Release 12", Achter);

        Assert.Multiple(() =>
        {
            Assert.That(mitSchraegstrich, Is.EqualTo("release-1-2-2026-09-08.kanbanc.json"));
            Assert.That(ohneSchraegstrich, Is.EqualTo("release-12-2026-09-08.kanbanc.json"));
            Assert.That(mitSchraegstrich, Is.Not.EqualTo(ohneSchraegstrich));
        });
    }

    [Test]
    public void Wenn_ein_Boardname_Umlaute_traegt_dann_stehen_sie_umschrieben_im_Dateinamen()
    {
        var name = Exportdateiname.Fuer("Prüfstände & Größen", Achter);

        Assert.That(name, Is.EqualTo("pruefstaende-groessen-2026-09-08.kanbanc.json"));
    }

    // Ein Boardname, von dem nach der Umschrift nichts übrig bleibt, ergibt trotzdem einen Namen.
    [Test]
    public void Wenn_vom_Boardnamen_kein_tragbares_Zeichen_bleibt_dann_heisst_die_Datei_board()
    {
        var name = Exportdateiname.Fuer("///", Achter);

        Assert.That(name, Is.EqualTo("board-2026-09-08.kanbanc.json"));
    }
}
