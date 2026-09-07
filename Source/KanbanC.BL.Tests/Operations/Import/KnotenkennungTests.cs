using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

// Die Form, aus der die Schnittebene eines früheren Laufs ablesbar ist — **kein neues Feld, kein
// Flag**.
public class KnotenkennungTests
{
    [Test]
    public void Wenn_der_Text_mit_einer_Kennung_und_einem_Namen_beginnt_dann_kommt_die_Kennung()
    {
        Assert.That(Knotenkennung.FuehrendeKennung("B0446 Fremde Etiketten bleiben"), Is.EqualTo("B0446"));
    }

    [Test]
    public void Wenn_der_Text_von_Hand_geschrieben_wurde_dann_gibt_es_keine_Kennung()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Knotenkennung.FuehrendeKennung("Mit Stefan sprechen"), Is.Null);
            Assert.That(Knotenkennung.FuehrendeKennung("B0446"), Is.Null, "Ohne Namen dahinter ist es keine Teilaufgabe der Datei.");
            Assert.That(Knotenkennung.FuehrendeKennung("X0446 Falscher Buchstabe"), Is.Null);
            Assert.That(Knotenkennung.FuehrendeKennung("B044 Zu kurz"), Is.Null);
        });
    }

    [Test]
    public void Wenn_die_Kennung_gelesen_wird_dann_sagt_ihr_Anfangsbuchstabe_die_Ebene()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Knotenkennung.EbeneAus("A0001"), Is.EqualTo(Wbsebene.Application));
            Assert.That(Knotenkennung.EbeneAus("D0008"), Is.EqualTo(Wbsebene.Dialog));
            Assert.That(Knotenkennung.EbeneAus("I0031"), Is.EqualTo(Wbsebene.Interaction));
            Assert.That(Knotenkennung.EbeneAus("F0055"), Is.EqualTo(Wbsebene.Feature));
            Assert.That(Knotenkennung.EbeneAus("B0436"), Is.EqualTo(Wbsebene.Bubble));
            Assert.That(Knotenkennung.EbeneAus("R00034"), Is.Null);
        });
    }
}
