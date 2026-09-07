using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Über den Browser ist diese Regel nicht zu zeigen: eine Sicht, die auf das Ereignis eines fremden
// Boards hin nachlädt, zeigt danach genau dasselbe Bild wie vorher — der überflüssige Abruf bliebe
// unsichtbar. Genau dafür gibt es dieses Testprojekt.
public class BoardereignisseTests
{
    [Test]
    public void Wenn_das_Ereignis_dasselbe_Board_nennt_dann_geht_es_die_Sicht_an()
    {
        Assert.That(Boardereignisse.GehoertZurSicht(4, 4), Is.True);
    }

    [Test]
    public void Wenn_das_Ereignis_ein_anderes_Board_nennt_dann_geht_es_die_Sicht_nichts_an()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Boardereignisse.GehoertZurSicht(5, 4), Is.False);
            Assert.That(Boardereignisse.GehoertZurSicht(4, 5), Is.False);
        });
    }
}
