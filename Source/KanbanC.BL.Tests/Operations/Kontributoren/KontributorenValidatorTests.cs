using KanbanC.BL.Operations.Kontributoren;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Kontributoren;

namespace KanbanC.BL.Tests.Operations.Kontributoren;

public class KontributorenValidatorTests
{
    [Test]
    public void Wenn_Name_und_Art_gueltig_sind_dann_gibt_es_keinen_Befund()
    {
        var anfrage = new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch);

        var befunde = KontributorenValidator.Pruefe(anfrage);

        Assert.That(befunde.IstOhneBefund, Is.True);
        Assert.That(befunde.BefundAnzahl, Is.EqualTo(0));
    }

    [Test]
    public void Wenn_alle_drei_Arten_geprueft_werden_dann_gibt_keine_von_ihnen_einen_Befund()
    {
        var mensch = KontributorenValidator.Pruefe(new KontributorAnlegenAnfrage("Stefan", Kontributorart.Mensch));
        var agent = KontributorenValidator.Pruefe(new KontributorAnlegenAnfrage("Codex-Agent", Kontributorart.Agent));
        var abgebildete = KontributorenValidator.Pruefe(new KontributorAnlegenAnfrage("Nina Barth", Kontributorart.Abgebildet));

        Assert.Multiple(() =>
        {
            Assert.That(mensch.IstOhneBefund, Is.True);
            Assert.That(agent.IstOhneBefund, Is.True);
            Assert.That(abgebildete.IstOhneBefund, Is.True);
        });
    }

    [Test]
    public void Wenn_der_Name_leer_ist_dann_gibt_es_genau_einen_Befund_zum_Namen()
    {
        var anfrage = new KontributorAnlegenAnfrage("", Kontributorart.Mensch);

        var befunde = KontributorenValidator.Pruefe(anfrage);

        Assert.That(befunde.IstOhneBefund, Is.False);
        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Is.EqualTo("Der Name darf nicht leer sein."));
    }

    [Test]
    public void Wenn_der_Name_nur_aus_Leerzeichen_besteht_dann_gibt_es_denselben_Befund()
    {
        var anfrage = new KontributorAnlegenAnfrage("   ", Kontributorart.Agent);

        var befunde = KontributorenValidator.Pruefe(anfrage);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kontributor-name-leer");
    }

    [Test]
    public void Wenn_der_Name_leer_ist_dann_nennt_die_Kompensation_die_Anlegeroute_mit_einem_nichtleeren_Namen()
    {
        var befunde = KontributorenValidator.Pruefe(new KontributorAnlegenAnfrage("", Kontributorart.Mensch));

        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Kompensation, Does.Contain("POST /api/kontributoren"));
            Assert.That(befunde[0].Kompensation, Does.Contain("name"));
        });
    }

    [Test]
    public void Wenn_die_Art_keinem_bekannten_Wert_entspricht_dann_gibt_es_einen_Befund_zur_Art()
    {
        var anfrage = new KontributorAnlegenAnfrage("Stefan", (Kontributorart)7);

        var befunde = KontributorenValidator.Pruefe(anfrage);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Meldung, Does.Contain("Kontributorart"));
        Assert.That(befunde[0].Kompensation, Does.Contain("Abgebildet"));
    }

    [Test]
    public void Wenn_Name_und_Art_beide_untauglich_sind_dann_traegt_jeder_Befund_Code_Meldung_und_Kompensationsaktion()
    {
        var anfrage = new KontributorAnlegenAnfrage("  ", (Kontributorart)99);

        var befunde = KontributorenValidator.Pruefe(anfrage);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(2));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kontributor-name-leer");
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[1], "kontributor-art-unbekannt");
    }
}
