using KanbanC.BL.Operations.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class KommentartextTests
{
    // Das Rechenbeispiel der Anforderung: „  Bitte prüfen  " wird als „Bitte prüfen" gespeichert.
    [Test]
    public void Wenn_der_Text_umschliessende_Leerzeichen_traegt_dann_liefert_Normalisiert_ihn_getrimmt()
    {
        var normalisiert = Kommentartext.Normalisiert("  Bitte prüfen  ");

        Assert.That(normalisiert, Is.EqualTo("Bitte prüfen"));
    }

    [Test]
    public void Wenn_der_Text_innen_Leerzeichen_traegt_dann_bleiben_sie_stehen()
    {
        var normalisiert = Kommentartext.Normalisiert("Die Lizenz gilt  nur pro Rechner.");

        Assert.That(normalisiert, Is.EqualTo("Die Lizenz gilt  nur pro Rechner."));
    }

    // Gross- und Kleinschreibung bleibt: eine Aeusserung steht so da, wie sie geschrieben wurde.
    [Test]
    public void Wenn_der_Text_Grossbuchstaben_traegt_dann_bleiben_sie_stehen()
    {
        var normalisiert = Kommentartext.Normalisiert("  Bitte PRÜFEN  ");

        Assert.That(normalisiert, Is.EqualTo("Bitte PRÜFEN"));
    }

    [Test]
    public void Wenn_der_Text_nur_aus_Leerzeichen_besteht_dann_bleibt_nach_dem_Trimmen_nichts_uebrig()
    {
        var normalisiert = Kommentartext.Normalisiert("   ");

        Assert.That(normalisiert, Is.Empty);
    }
}
