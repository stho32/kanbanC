using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

public class AnhangausfallTests
{
    [Test]
    public void Wenn_die_Uebertragung_abbricht_dann_nennt_die_Meldung_die_Datei_ihr_Scheitern_und_die_Kompensation()
    {
        var meldung = Anhangausfall.Abbruchmeldung("wbs-export.md");

        Assert.That(meldung, Does.Contain("wbs-export.md"));
        Assert.That(meldung, Does.Contain("nicht angehängt"));
        Assert.That(meldung, Does.Contain("erneut ablegen"));
    }

    [Test]
    public void Wenn_ein_anderes_Anhaengen_laeuft_dann_nennt_die_Meldung_beide_Dateien()
    {
        var meldung = Anhangausfall.Sperrmeldung("burndown-r2.png", "wbs-export.md");

        Assert.That(meldung, Does.Contain("burndown-r2.png"));
        Assert.That(meldung, Does.Contain("wbs-export.md"));
        Assert.That(meldung, Does.Contain("nicht angehängt"));
        Assert.That(meldung, Does.Contain("erneut ablegen"));
    }

    // „Die WebApi ist nicht erreichbar" wäre hier eine Falschaussage: sie war erreichbar, die
    // Datei war es nicht.
    [Test]
    public void Wenn_ein_Anhang_ausfaellt_dann_steht_dort_nicht_die_Meldung_des_WebApi_Ausfalls()
    {
        Assert.That(Anhangausfall.Abbruchmeldung("wbs-export.md"), Is.Not.EqualTo(WebApiAusfall.Meldung));
        Assert.That(Anhangausfall.Abbruchmeldung("wbs-export.md"), Does.Not.Contain("WebApi"));
        Assert.That(Anhangausfall.Sperrmeldung("burndown-r2.png", "wbs-export.md"), Does.Not.Contain("WebApi"));
    }
}
