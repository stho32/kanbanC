using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Der Melder trägt die eigene Handlung von der Kartenseite in die Kopfzeile — beide sitzen im
// selben Blazor-Kreislauf und kennen einander nicht. Geprüft wird, dass die Meldung ankommt und
// nach dem Abmelden ausbleibt: ohne das Abmelden hinge die Kopfzeile an einem abgerissenen
// Kreislauf.
public class LaufzeitmelderTests
{
    [Test]
    public void Wenn_ein_Timer_gemeldet_wird_dann_erfaehrt_es_der_Eingehaengte()
    {
        var melder = new Laufzeitmelder();
        var meldungen = 0;
        melder.Gemeldet += () => meldungen++;

        melder.Melde();

        Assert.That(meldungen, Is.EqualTo(1));
    }

    [Test]
    public void Wenn_zweimal_gemeldet_wird_dann_kommt_die_Meldung_zweimal_an()
    {
        var melder = new Laufzeitmelder();
        var meldungen = 0;
        melder.Gemeldet += () => meldungen++;

        melder.Melde();
        melder.Melde();

        Assert.That(meldungen, Is.EqualTo(2));
    }

    [Test]
    public void Wenn_der_Eingehaengte_sich_abmeldet_dann_erreicht_ihn_keine_Meldung_mehr()
    {
        var melder = new Laufzeitmelder();
        var meldungen = 0;
        void AufMeldung() => meldungen++;
        melder.Gemeldet += AufMeldung;
        melder.Melde();

        melder.Gemeldet -= AufMeldung;
        melder.Melde();

        Assert.That(meldungen, Is.EqualTo(1));
    }

    // Hängt niemand daran, ist die Meldung folgenlos — die Kartenseite muss nicht wissen, ob eine
    // Kopfzeile zuhört.
    [Test]
    public void Wenn_niemand_eingehaengt_ist_dann_bleibt_die_Meldung_folgenlos()
    {
        var melder = new Laufzeitmelder();

        Assert.That(melder.Melde, Throws.Nothing);
    }
}
