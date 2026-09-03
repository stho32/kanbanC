using KanbanC.BL.Tests.TestHelpers;
using KanbanC.BL.Operations.Boards;

namespace KanbanC.BL.Tests.Operations.Boards;

public class SpaltenValidatorTests
{
    [Test]
    public void Wenn_eine_Bezeichnung_ohne_Markierung_geprueft_wird_dann_gibt_es_keinen_Befund()
    {
        var befunde = SpaltenValidator.Pruefe("Wartet auf Zulieferung", false, null, []);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_eine_Abschlussspalte_mit_Anzeigegrenze_geprueft_wird_dann_gibt_es_keinen_Befund()
    {
        var befunde = SpaltenValidator.Pruefe("Abgenommen", true, 10, []);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_die_Bezeichnung_nur_aus_Leerzeichen_besteht_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("   ", false, null, []);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("Bezeichnung"));
    }

    [Test]
    public void Wenn_eine_Abschlussspalte_keine_Anzeigegrenze_traegt_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("Abgenommen", true, null, []);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("Anzeigegrenze"));
    }

    [Test]
    public void Wenn_die_Anzeigegrenze_null_ist_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("Abgenommen", true, 0, []);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("größer"));
    }

    [Test]
    public void Wenn_die_Anzeigegrenze_negativ_ist_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("Abgenommen", true, -1, []);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("größer"));
    }

    [Test]
    public void Wenn_eine_nicht_markierte_Spalte_eine_Anzeigegrenze_traegt_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("In Arbeit", false, 5, []);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("nur an einer Abschlussspalte"));
    }

    [Test]
    public void Wenn_Bezeichnung_und_Anzeigegrenze_zugleich_falsch_sind_dann_kommen_beide_Befunde()
    {
        var befunde = SpaltenValidator.Pruefe("", true, null, []);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(2));
    }

    [Test]
    public void Wenn_die_Bezeichnung_auf_dem_Board_schon_vergeben_ist_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("Erledigt", false, null, ["Zu erledigen", "In Arbeit", "Erledigt"]);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("schon vergeben"));
        Assert.That(befunde[0].Meldung, Does.Contain("Erledigt"));
    }

    [Test]
    public void Wenn_die_vergebene_Bezeichnung_anders_geschrieben_ist_dann_wird_sie_trotzdem_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("erledigt", false, null, ["Erledigt"]);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("schon vergeben"));
    }

    [Test]
    public void Wenn_die_Bezeichnung_nur_durch_umschliessende_Leerzeichen_abweicht_dann_wird_sie_bemaengelt()
    {
        var befunde = SpaltenValidator.Pruefe("Erledigt ", false, null, ["Erledigt"]);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("schon vergeben"));
    }

    [Test]
    public void Wenn_die_eigene_Bezeichnung_nicht_unter_den_vergebenen_steht_dann_gibt_es_keinen_Befund()
    {
        var befunde = SpaltenValidator.Pruefe("Erledigt", true, 20, ["Zu erledigen", "In Arbeit"]);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_eine_freie_Bezeichnung_neben_vergebenen_steht_dann_gibt_es_keinen_Befund()
    {
        var befunde = SpaltenValidator.Pruefe("Wartet auf Zulieferung", false, null, ["Zu erledigen", "In Arbeit", "Erledigt"]);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_die_Bezeichnung_leer_ist_dann_bleibt_es_beim_Leer_Befund_ohne_Konfliktbefund()
    {
        var befunde = SpaltenValidator.Pruefe("   ", false, null, ["", "In Arbeit"]);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("nicht leer"));
    }

    [Test]
    public void Wenn_Bezeichnung_und_Markierung_zugleich_verletzt_sind_dann_traegt_jeder_Befund_Code_Meldung_und_Kompensationsaktion()
    {
        var befunde = SpaltenValidator.Pruefe("", true, null, []);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(2));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "spalte-bezeichnung-leer");
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[1], "abschlussspalte-ohne-anzeigegrenze");
    }

    [Test]
    public void Wenn_eine_vergebene_Bezeichnung_mit_unmoeglicher_Anzeigegrenze_kommt_dann_tragen_alle_drei_Befunde_ihren_Code()
    {
        var befunde = SpaltenValidator.Pruefe("Erledigt", false, 0, ["Erledigt"]);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(3));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "spalte-bezeichnung-vergeben");
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[1], "anzeigegrenze-nicht-positiv");
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[2], "anzeigegrenze-ohne-abschlussspalte");
    }

}
