using KanbanC.BL.Operations.Boards;

namespace KanbanC.BL.Tests.Operations.Boards;

public class SpaltenbezeichnungTests
{
    [Test]
    public void Wenn_eine_Bezeichnung_umschliessende_Leerzeichen_traegt_dann_liefert_Normalisiert_sie_ohne_diese()
    {
        var normalisiert = Spaltenbezeichnung.Normalisiert("  Erledigt  ");

        Assert.That(normalisiert, Is.EqualTo("Erledigt"));
    }

    [Test]
    public void Wenn_eine_Bezeichnung_innere_Leerzeichen_traegt_dann_bleiben_sie_erhalten()
    {
        var normalisiert = Spaltenbezeichnung.Normalisiert(" In Arbeit ");

        Assert.That(normalisiert, Is.EqualTo("In Arbeit"));
    }

    [Test]
    public void Wenn_eine_Bezeichnung_nur_aus_Leerzeichen_besteht_dann_bleibt_nichts_uebrig()
    {
        var normalisiert = Spaltenbezeichnung.Normalisiert("   ");

        Assert.That(normalisiert, Is.Empty);
    }

    [Test]
    public void Wenn_sich_zwei_Bezeichnungen_nur_in_der_Schreibweise_unterscheiden_dann_sind_sie_gleich()
    {
        Assert.That(Spaltenbezeichnung.SindGleich("Erledigt", "erledigt"), Is.True);
    }

    [Test]
    public void Wenn_sich_zwei_Bezeichnungen_nur_in_umschliessenden_Leerzeichen_unterscheiden_dann_sind_sie_gleich()
    {
        Assert.That(Spaltenbezeichnung.SindGleich("Erledigt", "Erledigt "), Is.True);
    }

    [Test]
    public void Wenn_sich_zwei_Bezeichnungen_nur_in_der_Schreibweise_eines_Umlauts_unterscheiden_dann_sind_sie_gleich()
    {
        Assert.That(Spaltenbezeichnung.SindGleich("Prüfung", "PRÜFUNG"), Is.True);
    }

    [Test]
    public void Wenn_zwei_Bezeichnungen_verschiedene_Woerter_sind_dann_sind_sie_nicht_gleich()
    {
        Assert.That(Spaltenbezeichnung.SindGleich("Erledigt", "Erledigung"), Is.False);
    }

    [Test]
    public void Wenn_sich_zwei_Bezeichnungen_in_inneren_Leerzeichen_unterscheiden_dann_sind_sie_nicht_gleich()
    {
        Assert.That(Spaltenbezeichnung.SindGleich("In Arbeit", "InArbeit"), Is.False);
    }
}
