using KanbanC.BL.Operations.Boards;

namespace KanbanC.BL.Tests.Operations.Boards;

public class SpaltenValidatorTests
{
    [Test]
    public void Wenn_eine_Bezeichnung_ohne_Markierung_geprueft_wird_dann_gibt_es_keinen_Befund()
    {
        var befunde = SpaltenValidator.Pruefe("Wartet auf Zulieferung", false, null);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_eine_Abschlussspalte_mit_Anzeigegrenze_geprueft_wird_dann_gibt_es_keinen_Befund()
    {
        var befunde = SpaltenValidator.Pruefe("Abgenommen", true, 10);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_die_Bezeichnung_nur_aus_Leerzeichen_besteht_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("   ", false, null);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0], Does.Contain("Bezeichnung"));
    }

    [Test]
    public void Wenn_eine_Abschlussspalte_keine_Anzeigegrenze_traegt_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("Abgenommen", true, null);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0], Does.Contain("Anzeigegrenze"));
    }

    [Test]
    public void Wenn_die_Anzeigegrenze_null_ist_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("Abgenommen", true, 0);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0], Does.Contain("größer"));
    }

    [Test]
    public void Wenn_die_Anzeigegrenze_negativ_ist_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("Abgenommen", true, -1);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0], Does.Contain("größer"));
    }

    [Test]
    public void Wenn_eine_nicht_markierte_Spalte_eine_Anzeigegrenze_traegt_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("In Arbeit", false, 5);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0], Does.Contain("nur an einer Abschlussspalte"));
    }

    [Test]
    public void Wenn_Bezeichnung_und_Anzeigegrenze_zugleich_falsch_sind_dann_kommen_beide_Befunde()
    {
        var befunde = SpaltenValidator.Pruefe("", true, null);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(2));
    }
}
