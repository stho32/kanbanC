using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

public class WbsbaumbildnerTests
{
    [Test]
    public void Wenn_die_Knoten_gebildet_werden_dann_bleibt_die_Dateireihenfolge_stehen()
    {
        IReadOnlyList<Wbsknoten> knoten =
        [
            Knoten("A0001", Wbsebene.Application, "—", 1),
            Knoten("D0001", Wbsebene.Dialog, "A0001", 2),
            Knoten("I0002", Wbsebene.Interaction, "D0001", 3),
            Knoten("I0001", Wbsebene.Interaction, "D0001", 4),
        ];

        var bildung = Wbsbaumbildner.Bilde(knoten);

        Assert.Multiple(() =>
        {
            Assert.That(bildung.Baum.KnotenAnzahl, Is.EqualTo(4));
            Assert.That(bildung.Baum[2].Id, Is.EqualTo("I0002"));
            Assert.That(bildung.Baum[3].Id, Is.EqualTo("I0001"));
            Assert.That(bildung.Uebersprungene, Is.Empty);
        });
    }

    [Test]
    public void Wenn_ein_Knoten_einen_unbekannten_Eltern_nennt_dann_wird_er_uebersprungen()
    {
        IReadOnlyList<Wbsknoten> knoten =
        [
            Knoten("A0001", Wbsebene.Application, "—", 1),
            Knoten("I0009", Wbsebene.Interaction, "D0099", 2),
        ];

        var bildung = Wbsbaumbildner.Bilde(knoten);

        Assert.Multiple(() =>
        {
            Assert.That(bildung.Baum.KnotenAnzahl, Is.EqualTo(1));
            Assert.That(bildung.Uebersprungene.Single().Kennung, Is.EqualTo("I0009"));
            Assert.That(bildung.Uebersprungene.Single().Grund, Does.Contain("D0099"));
        });
    }

    // Der Teilbaum verschwindet nicht still: jeder Nachfahre bekommt eine eigene Zeile.
    [Test]
    public void Wenn_ein_Eltern_uebersprungen_wird_dann_wird_sein_Teilbaum_mitgemeldet()
    {
        IReadOnlyList<Wbsknoten> knoten =
        [
            Knoten("A0001", Wbsebene.Application, "—", 1),
            Knoten("I0009", Wbsebene.Interaction, "D0099", 2),
            Knoten("F0009", Wbsebene.Feature, "I0009", 3),
            Knoten("B0009", Wbsebene.Bubble, "F0009", 4),
        ];

        var bildung = Wbsbaumbildner.Bilde(knoten);

        Assert.Multiple(() =>
        {
            Assert.That(bildung.Baum.KnotenAnzahl, Is.EqualTo(1));
            Assert.That(bildung.Uebersprungene, Has.Count.EqualTo(3));
            Assert.That(bildung.Uebersprungene[1].Kennung, Is.EqualTo("F0009"));
            Assert.That(bildung.Uebersprungene[1].Grund, Does.Contain("I0009"));
            Assert.That(bildung.Uebersprungene[2].Kennung, Is.EqualTo("B0009"));
        });
    }

    // Die Wurzel braucht keinen Eltern — ihre Zelle trägt in einer erzeugten Datei einen
    // Gedankenstrich.
    [Test]
    public void Wenn_die_Application_keinen_Eltern_nennt_dann_bleibt_sie_im_Baum()
    {
        IReadOnlyList<Wbsknoten> knoten = [Knoten("A0001", Wbsebene.Application, "—", 1)];

        var bildung = Wbsbaumbildner.Bilde(knoten);

        Assert.That(bildung.Baum.KnotenAnzahl, Is.EqualTo(1));
    }

    [Test]
    public void Wenn_eine_Kennung_zweimal_vorkommt_dann_wird_die_zweite_Zeile_gemeldet()
    {
        IReadOnlyList<Wbsknoten> knoten =
        [
            Knoten("A0001", Wbsebene.Application, "—", 1),
            Knoten("I0001", Wbsebene.Interaction, "A0001", 2),
            Knoten("I0001", Wbsebene.Interaction, "A0001", 3),
        ];

        var bildung = Wbsbaumbildner.Bilde(knoten);

        Assert.Multiple(() =>
        {
            Assert.That(bildung.Baum.KnotenAnzahl, Is.EqualTo(2));
            Assert.That(bildung.Uebersprungene.Single().Zeilennummer, Is.EqualTo(3));
            Assert.That(bildung.Uebersprungene.Single().Grund, Does.Contain("Zeile 2"));
        });
    }

    [Test]
    public void Wenn_der_Baum_steht_dann_liefert_er_Kinder_Nachfahren_und_Vorfahren_in_Dateireihenfolge()
    {
        IReadOnlyList<Wbsknoten> knoten =
        [
            Knoten("A0001", Wbsebene.Application, "—", 1),
            Knoten("D0001", Wbsebene.Dialog, "A0001", 2),
            Knoten("I0001", Wbsebene.Interaction, "D0001", 3),
            Knoten("F0001", Wbsebene.Feature, "I0001", 4),
            Knoten("B0001", Wbsebene.Bubble, "F0001", 5),
            Knoten("F0002", Wbsebene.Feature, "I0001", 6),
        ];

        var baum = Wbsbaumbildner.Bilde(knoten).Baum;

        var nachfahren = baum.NachfahrenInDateireihenfolge("I0001");
        var vorfahren = baum.VorfahrenVonObenNachUnten(baum[4]);
        Assert.Multiple(() =>
        {
            Assert.That(baum.Kinder("I0001").Select(kind => kind.Id), Is.EqualTo(new[] { "F0001", "F0002" }));
            Assert.That(nachfahren.Select(kind => kind.Id), Is.EqualTo(new[] { "F0001", "B0001", "F0002" }));
            Assert.That(vorfahren.Select(vorfahr => vorfahr.Id), Is.EqualTo(new[] { "A0001", "D0001", "I0001", "F0001" }));
            Assert.That(baum.HatNachfahrenAuf("D0001", Wbsebene.Bubble), Is.True);
            Assert.That(baum.HatNachfahrenAuf("F0002", Wbsebene.Bubble), Is.False);
        });
    }

    private static Wbsknoten Knoten(string id, Wbsebene ebene, string eltern, int zeilennummer)
    {
        return new Wbsknoten(id, ebene, eltern, id, Wbsstatus.Rot, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, zeilennummer);
    }
}
