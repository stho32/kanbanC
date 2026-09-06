using KanbanC.BL.Operations.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class AnhangnameTests
{
    [Test]
    public void Wenn_der_Name_keinen_Weg_traegt_dann_bleibt_er_unveraendert()
    {
        Assert.That(Anhangname.Normalisiert("wbs-export.md"), Is.EqualTo("wbs-export.md"));
    }

    [Test]
    public void Wenn_der_Browser_einen_Windows_Weg_meldet_dann_bleibt_nur_der_Dateiname_stehen()
    {
        Assert.That(Anhangname.Normalisiert(@"C:\Temp\wbs-export.md"), Is.EqualTo("wbs-export.md"));
    }

    [Test]
    public void Wenn_der_Browser_einen_Ordnerweg_meldet_dann_bleibt_ebenso_nur_der_Dateiname_stehen()
    {
        Assert.That(Anhangname.Normalisiert("ordner/wbs-export.md"), Is.EqualTo("wbs-export.md"));
    }

    [Test]
    public void Wenn_der_Name_Randleerzeichen_traegt_dann_fallen_sie_weg()
    {
        Assert.That(Anhangname.Normalisiert("  burndown-r2.png  "), Is.EqualTo("burndown-r2.png"));
    }

    [Test]
    public void Wenn_der_Name_nur_aus_einem_Weg_besteht_dann_bleibt_er_leer()
    {
        Assert.That(Anhangname.Normalisiert("ordner/"), Is.EqualTo(string.Empty));
    }

    // Punkte im Namen sind keine Wegtrenner: eine Datei darf mehrere Endungen tragen.
    [Test]
    public void Wenn_der_Name_mehrere_Punkte_traegt_dann_bleibt_er_ganz()
    {
        Assert.That(Anhangname.Normalisiert("kanbanc.wbs.export.md"), Is.EqualTo("kanbanc.wbs.export.md"));
    }
}
