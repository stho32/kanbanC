using KanbanC.BL.Operations.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class KartentitelTests
{
    [Test]
    public void Wenn_der_Titel_umschliessende_Leerzeichen_traegt_dann_liefert_Normalisiert_ihn_getrimmt()
    {
        var normalisiert = Kartentitel.Normalisiert("  Migration schreiben  ");

        Assert.That(normalisiert, Is.EqualTo("Migration schreiben"));
    }

    [Test]
    public void Wenn_der_Titel_innen_Leerzeichen_traegt_dann_bleiben_sie_stehen()
    {
        var normalisiert = Kartentitel.Normalisiert("Migration  schreiben");

        Assert.That(normalisiert, Is.EqualTo("Migration  schreiben"));
    }

    [Test]
    public void Wenn_der_Titel_nur_aus_Leerzeichen_besteht_dann_bleibt_nach_dem_Trimmen_nichts_uebrig()
    {
        var normalisiert = Kartentitel.Normalisiert("   ");

        Assert.That(normalisiert, Is.Empty);
    }
}
