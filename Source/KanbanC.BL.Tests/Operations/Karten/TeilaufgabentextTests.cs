using KanbanC.BL.Operations.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class TeilaufgabentextTests
{
    // Das Rechenbeispiel der Anforderung: „  Kaffee  " wird als „Kaffee" gespeichert.
    [Test]
    public void Wenn_der_Text_umschliessende_Leerzeichen_traegt_dann_liefert_Normalisiert_ihn_getrimmt()
    {
        var normalisiert = Teilaufgabentext.Normalisiert("  Kaffee  ");

        Assert.That(normalisiert, Is.EqualTo("Kaffee"));
    }

    [Test]
    public void Wenn_der_Text_innen_Leerzeichen_traegt_dann_bleiben_sie_stehen()
    {
        var normalisiert = Teilaufgabentext.Normalisiert("Kaffee  holen");

        Assert.That(normalisiert, Is.EqualTo("Kaffee  holen"));
    }

    // Gross- und Kleinschreibung bleibt: „Lizenztext lesen" und „lizenztext lesen" sind zwei
    // Schreibweisen desselben Schritts, und die Anwendung entscheidet nicht, welche gilt.
    [Test]
    public void Wenn_der_Text_Grossbuchstaben_traegt_dann_bleiben_sie_stehen()
    {
        var normalisiert = Teilaufgabentext.Normalisiert("  Lizenztext Lesen  ");

        Assert.That(normalisiert, Is.EqualTo("Lizenztext Lesen"));
    }

    [Test]
    public void Wenn_der_Text_nur_aus_Leerzeichen_besteht_dann_bleibt_nach_dem_Trimmen_nichts_uebrig()
    {
        var normalisiert = Teilaufgabentext.Normalisiert("   ");

        Assert.That(normalisiert, Is.Empty);
    }
}
