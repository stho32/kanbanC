using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Die Fläche des Boardimports kennt **einen** Sperrgrund: ein Lauf läuft. Anders als die Fläche
// an der Karte braucht sie keine gewählte Identität — der Import hat keinen Urheber.
public class BoardablegeflaecheTests
{
    [Test]
    public void Wenn_kein_Lauf_laeuft_dann_ist_die_Flaeche_offen_und_fordert_zum_Ablegen_auf()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Boardablegeflaeche.IstGesperrt(null), Is.False);
            Assert.That(Boardablegeflaeche.Text(null), Is.EqualTo(Boardablegeflaeche.Ablegeaufforderung));
        });
    }

    // Eine zweite Ablage während des ersten Laufs verlöre den ersten still.
    [Test]
    public void Wenn_ein_Lauf_laeuft_dann_ist_die_Flaeche_gesperrt_und_nennt_die_laufende_Datei()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Boardablegeflaeche.IstGesperrt("release-2.kanbanc.json"), Is.True);
            Assert.That(Boardablegeflaeche.Text("release-2.kanbanc.json"), Does.Contain("release-2.kanbanc.json"));
        });
    }
}
