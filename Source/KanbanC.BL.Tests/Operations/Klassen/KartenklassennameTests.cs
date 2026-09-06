using KanbanC.BL.Operations.Klassen;

namespace KanbanC.BL.Tests.Operations.Klassen;

public class KartenklassennameTests
{
    [Test]
    public void Wenn_der_Name_Raender_traegt_dann_fallen_sie_beim_Normalisieren_weg()
    {
        var normalisiert = Kartenklassenname.Normalisiert("  Dokumentation  ");

        Assert.That(normalisiert, Is.EqualTo("Dokumentation"));
    }

    [Test]
    public void Wenn_der_Name_nur_aus_Leerzeichen_besteht_dann_bleibt_nichts_uebrig()
    {
        var normalisiert = Kartenklassenname.Normalisiert("   ");

        Assert.That(normalisiert, Is.Empty);
    }

    // Nur die Ränder fallen weg: die Schreibweise und die Leerzeichen im Namen bleiben, wie sie
    // getippt wurden — anders als beim Präfix gibt es hier keinen Zeichenvorrat.
    [Test]
    public void Wenn_der_Name_Leerzeichen_und_Grossschreibung_traegt_dann_bleibt_er_sonst_unveraendert()
    {
        var normalisiert = Kartenklassenname.Normalisiert(" WBS der Wartung ");

        Assert.That(normalisiert, Is.EqualTo("WBS der Wartung"));
    }
}
