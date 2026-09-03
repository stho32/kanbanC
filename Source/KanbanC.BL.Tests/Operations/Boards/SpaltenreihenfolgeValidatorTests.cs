using KanbanC.BL.Tests.TestHelpers;
using KanbanC.BL.Operations.Boards;

namespace KanbanC.BL.Tests.Operations.Boards;

public class SpaltenreihenfolgeValidatorTests
{
    [Test]
    public void Wenn_die_Reihenfolge_alle_Spalten_genau_einmal_nennt_dann_gibt_es_keinen_Befund()
    {
        var befunde = SpaltenreihenfolgeValidator.Pruefe([3, 1, 2], [1, 2, 3]);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_ein_Board_keine_Spalte_hat_dann_ist_die_leere_Reihenfolge_ohne_Befund()
    {
        var befunde = SpaltenreihenfolgeValidator.Pruefe([], []);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_die_Reihenfolge_nur_zwei_von_drei_Spalten_nennt_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenreihenfolgeValidator.Pruefe([2, 1], [1, 2, 3]);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("alle Spalten"));
    }

    [Test]
    public void Wenn_die_Reihenfolge_eine_Spalte_doppelt_nennt_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenreihenfolgeValidator.Pruefe([1, 1, 2], [1, 2, 3]);

        Assert.That(befunde.IstOhneBefund, Is.False);
        Assert.That(befunde[0].Meldung, Does.Contain("mehrfach"));
    }

    [Test]
    public void Wenn_die_Reihenfolge_eine_fremde_SpalteId_nennt_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenreihenfolgeValidator.Pruefe([1, 2, 9], [1, 2, 3]);

        Assert.That(befunde.IstOhneBefund, Is.False);
        Assert.That(befunde[0].Meldung, Does.Contain("nicht zu diesem Board"));
    }

    [Test]
    public void Wenn_die_Reihenfolge_leer_ist_obwohl_das_Board_Spalten_hat_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenreihenfolgeValidator.Pruefe([], [1, 2, 3]);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("alle Spalten"));
    }

    [Test]
    public void Wenn_die_Reihenfolge_alle_drei_Regeln_verletzt_dann_traegt_jeder_Befund_Code_Meldung_und_Kompensationsaktion()
    {
        var befunde = SpaltenreihenfolgeValidator.Pruefe([1, 1, 9], [1, 2]);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(3));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "reihenfolge-nennt-spalte-mehrfach");
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[1], "reihenfolge-nennt-fremde-spalte");
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[2], "reihenfolge-unvollstaendig");
    }

    [Test]
    public void Wenn_eine_fremde_Spalte_genannt_wird_dann_nennt_die_Kompensationsaktion_ihre_SpalteId()
    {
        var befunde = SpaltenreihenfolgeValidator.Pruefe([1, 2, 9], [1, 2]);

        Assert.That(befunde[0].Kompensation, Does.Contain("9"));
    }

}
