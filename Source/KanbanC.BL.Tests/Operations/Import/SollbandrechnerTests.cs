using KanbanC.BL.Models.Import;
using KanbanC.BL.Operations.Import;
using KanbanC.BL.Tests.TestHelpers;

namespace KanbanC.BL.Tests.Operations.Import;

public class SollbandrechnerTests
{
    // Der Fall, um dessentwillen es diese Operation gibt: eine Interaction-Zeile trägt in der WBS
    // keinen eigenen Aufwand, geschätzt wird auf Bubble-Ebene.
    [Test]
    public void Wenn_eine_Interaction_ohne_eigenen_Aufwand_Bubbles_mit_Aufwand_hat_dann_traegt_sie_deren_Summe()
    {
        var baum = Probewbs.Baum(
            Knoten("A0001", Wbsebene.Application, "—", Wbsstatus.Gelb, string.Empty, 1),
            Knoten("D0005", Wbsebene.Dialog, "A0001", Wbsstatus.Gelb, string.Empty, 2),
            Knoten("I0022", Wbsebene.Interaction, "D0005", Wbsstatus.Gruen, string.Empty, 3),
            Knoten("B0301", Wbsebene.Bubble, "I0022", Wbsstatus.Gruen, "0,4", 4),
            Knoten("B0302", Wbsebene.Bubble, "I0022", Wbsstatus.Gruen, "0,4", 5),
            Knoten("B0303", Wbsebene.Bubble, "I0022", Wbsstatus.Gruen, "2", 6),
            Knoten("B0304", Wbsebene.Bubble, "I0022", Wbsstatus.Gruen, "0,4-1,5", 7));

        var baender = Sollbandrechner.RechneJeKartenknoten(baum, [baum.Knoten("I0022")!]);

        Assert.That(baender["I0022"], Is.EqualTo(new Sollband(3.2m, 4.3m)));
    }

    // Über zwei Ebenen: der Aufwand hängt nicht nur an den unmittelbaren Kindern, und der eigene
    // Aufwand der Karte zählt mit.
    [Test]
    public void Wenn_der_Teilbaum_zwei_Ebenen_tief_ist_dann_zaehlen_der_eigene_Aufwand_und_alle_Nachfahren()
    {
        var baum = Probewbs.Baum(
            Knoten("A0001", Wbsebene.Application, "—", Wbsstatus.Gelb, string.Empty, 1),
            Knoten("I0001", Wbsebene.Interaction, "A0001", Wbsstatus.Rot, "1", 2),
            Knoten("F0001", Wbsebene.Feature, "I0001", Wbsstatus.Rot, "0,5", 3),
            Knoten("B0001", Wbsebene.Bubble, "F0001", Wbsstatus.Rot, "2-4", 4));

        var baender = Sollbandrechner.RechneJeKartenknoten(baum, [baum.Knoten("I0001")!]);

        Assert.That(baender["I0001"], Is.EqualTo(new Sollband(3.5m, 5.5m)));
    }

    // Der Filter läuft vor dem Bildner und entfernt option, ausbau und verworfen samt Teilbaum.
    // Gerechnet wird auf dem gefilterten Baum, und die Summe erbt den Ausschluss.
    [Test]
    public void Wenn_der_Umfangsfilter_gelaufen_ist_dann_zaehlen_option_ausbau_und_verworfen_samt_Teilbaum_nicht_mit()
    {
        var baum = Probewbs.Baum(
            Knoten("A0001", Wbsebene.Application, "—", Wbsstatus.Gelb, string.Empty, 1),
            Knoten("I0001", Wbsebene.Interaction, "A0001", Wbsstatus.Rot, string.Empty, 2),
            Knoten("B0001", Wbsebene.Bubble, "I0001", Wbsstatus.Rot, "2", 3),
            Knoten("F0002", Wbsebene.Feature, "I0001", Wbsstatus.Option, "1", 4),
            Knoten("B0002", Wbsebene.Bubble, "F0002", Wbsstatus.Rot, "8", 5),
            Knoten("B0003", Wbsebene.Bubble, "I0001", Wbsstatus.Ausbau, "16", 6),
            Knoten("B0004", Wbsebene.Bubble, "I0001", Wbsstatus.Verworfen, "32", 7));
        var gefiltert = Umfangsfilter.Filtere(baum).Baum;

        var baender = Sollbandrechner.RechneJeKartenknoten(gefiltert, [gefiltert.Knoten("I0001")!]);

        Assert.That(baender["I0001"], Is.EqualTo(new Sollband(2m, 2m)));
    }

    // Kein Band statt 0,0–0,0: die Karte steht dann ohne Soll da, und die Fußzeile der Auswertung
    // zählt sie.
    [Test]
    public void Wenn_kein_Knoten_des_Teilbaums_einen_Aufwand_traegt_dann_bekommt_die_Karte_kein_Band()
    {
        var baum = Probewbs.Baum(
            Knoten("A0001", Wbsebene.Application, "—", Wbsstatus.Gelb, string.Empty, 1),
            Knoten("I0001", Wbsebene.Interaction, "A0001", Wbsstatus.Rot, string.Empty, 2),
            Knoten("B0001", Wbsebene.Bubble, "I0001", Wbsstatus.Rot, string.Empty, 3),
            Knoten("B0002", Wbsebene.Bubble, "I0001", Wbsstatus.Rot, "unklar", 4));

        var baender = Sollbandrechner.RechneJeKartenknoten(baum, [baum.Knoten("I0001")!]);

        Assert.That(baender.ContainsKey("I0001"), Is.False);
    }

    // Eine unlesbare Zelle zwischen lesbaren nimmt der Karte ihr Band nicht — sie steuert nur
    // nichts bei. Die Zahl ist dann ein Untermaß, und das ist die bewusste Wahl gegen ein
    // Vollständigkeitszeichen an jeder Zeile.
    [Test]
    public void Wenn_eine_Zelle_des_Teilbaums_unlesbar_ist_dann_zaehlen_die_uebrigen_weiter()
    {
        var baum = Probewbs.Baum(
            Knoten("A0001", Wbsebene.Application, "—", Wbsstatus.Gelb, string.Empty, 1),
            Knoten("I0001", Wbsebene.Interaction, "A0001", Wbsstatus.Rot, string.Empty, 2),
            Knoten("B0001", Wbsebene.Bubble, "I0001", Wbsstatus.Rot, "2", 3),
            Knoten("B0002", Wbsebene.Bubble, "I0001", Wbsstatus.Rot, "später", 4));

        var baender = Sollbandrechner.RechneJeKartenknoten(baum, [baum.Knoten("I0001")!]);

        Assert.That(baender["I0001"], Is.EqualTo(new Sollband(2m, 2m)));
    }

    [Test]
    public void Wenn_zwei_Karten_gerechnet_werden_dann_traegt_jede_die_Summe_ihres_eigenen_Teilbaums()
    {
        var baum = Probewbs.Baum(
            Knoten("A0001", Wbsebene.Application, "—", Wbsstatus.Gelb, string.Empty, 1),
            Knoten("D0001", Wbsebene.Dialog, "A0001", Wbsstatus.Gelb, string.Empty, 2),
            Knoten("I0001", Wbsebene.Interaction, "D0001", Wbsstatus.Rot, string.Empty, 3),
            Knoten("B0001", Wbsebene.Bubble, "I0001", Wbsstatus.Rot, "2", 4),
            Knoten("I0002", Wbsebene.Interaction, "D0001", Wbsstatus.Rot, string.Empty, 5),
            Knoten("B0002", Wbsebene.Bubble, "I0002", Wbsstatus.Rot, "0,4", 6));

        var baender = Sollbandrechner.RechneJeKartenknoten(baum, [baum.Knoten("I0001")!, baum.Knoten("I0002")!]);

        Assert.Multiple(() =>
        {
            Assert.That(baender["I0001"], Is.EqualTo(new Sollband(2m, 2m)));
            Assert.That(baender["I0002"], Is.EqualTo(new Sollband(0.4m, 0.4m)));
        });
    }

    private static Wbsknoten Knoten(string id, Wbsebene ebene, string eltern, Wbsstatus status, string aufwand, int zeilennummer)
    {
        return new Wbsknoten(id, ebene, eltern, $"Knoten {id}", status, string.Empty, string.Empty, aufwand, string.Empty, string.Empty, string.Empty, string.Empty, zeilennummer);
    }
}
