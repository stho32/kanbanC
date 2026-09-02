using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

[TestFixture]
public class BoardadresseTests
{
    [TestCase("http://localhost:5180/boards/1")]
    [TestCase("http://localhost:5180/boards/42")]
    [TestCase("http://localhost:5180/boards/1/")]
    public void Wenn_die_Adresse_auf_ein_einzelnes_Board_zeigt_dann_meldet_ZeigtAufEinBoard_wahr(string adresse)
    {
        Assert.That(Boardadresse.ZeigtAufEinBoard(adresse), Is.True);
    }

    [TestCase("http://localhost:5180/boards", TestName = "die Uebersicht ist kein einzelnes Board")]
    [TestCase("http://localhost:5180/", TestName = "die Startseite")]
    [TestCase("http://localhost:5180/boards/abc", TestName = "eine Adresse ohne Nummer")]
    [TestCase("http://localhost:5180/boardsammlung/1", TestName = "ein anderer Pfad mit demselben Anfang")]
    [TestCase("http://localhost:5180/boards/1/karten", TestName = "eine Unterseite des Boards")]
    public void Wenn_die_Adresse_nicht_auf_ein_einzelnes_Board_zeigt_dann_meldet_ZeigtAufEinBoard_falsch(string adresse)
    {
        Assert.That(Boardadresse.ZeigtAufEinBoard(adresse), Is.False);
    }
}
