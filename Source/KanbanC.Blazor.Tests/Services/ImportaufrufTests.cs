using KanbanC.Blazor.Services;
using KanbanC.Contracts.Import;

namespace KanbanC.Blazor.Tests.Services;

// Der Aufruf im Schirm ist kein Schmuck: er ist die eingelöste Zusage, dass ein Agent denselben
// Weg geht. Weicht er von der Route ab, ist die Zusage eine Behauptung.
public class ImportaufrufTests
{
    [Test]
    public void Wenn_der_gleichwertige_Aufruf_gezeigt_wird_dann_nennt_er_Route_Klasse_Schnittebene_und_trocken()
    {
        var aufruf = Importaufruf.Fuer(2, 3, Schnittebene.Interaction, trocken: true);

        Assert.Multiple(() =>
        {
            Assert.That(aufruf, Does.Contain("POST /api/boards/2/wbs-import"));
            Assert.That(aufruf, Does.Contain("klasse=3"));
            Assert.That(aufruf, Does.Contain("schnittebene=Interaction"));
            Assert.That(aufruf, Does.Contain("trocken=true"));
        });
    }

    [Test]
    public void Wenn_der_Schreiblauf_gezeigt_wird_dann_steht_trocken_auf_false()
    {
        var aufruf = Importaufruf.Fuer(2, 3, Schnittebene.Bubble, trocken: false);

        Assert.Multiple(() =>
        {
            Assert.That(aufruf, Does.Contain("schnittebene=Bubble"));
            Assert.That(aufruf, Does.Contain("trocken=false"));
        });
    }
}
