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
            Assert.That(befunde[0].Kompensation, Does.Not.Contain("PUT"));
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

    [Test]
    public void Wenn_beim_Aendern_der_Name_leer_ist_dann_gibt_es_denselben_Befund_wie_beim_Anlegen()
    {
        var anfrage = new KontributorAendernAnfrage("", Kontributorart.Mensch);

        var befunde = KontributorenValidator.Pruefe(7, anfrage);

        Assert.That(befunde.IstOhneBefund, Is.False);
        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kontributor-name-leer");
    }

    [Test]
    public void Wenn_beim_Aendern_der_Name_nur_aus_Leerzeichen_besteht_dann_gibt_es_denselben_Befund()
    {
        var befunde = KontributorenValidator.Pruefe(7, new KontributorAendernAnfrage("   ", Kontributorart.Agent));

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Code, Is.EqualTo("kontributor-name-leer"));
    }

    [Test]
    public void Wenn_beim_Aendern_der_Name_leer_ist_dann_nennt_die_Kompensation_die_Aenderungsroute_dieses_Kontributors()
    {
        var befunde = KontributorenValidator.Pruefe(7, new KontributorAendernAnfrage("", Kontributorart.Mensch));

        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Kompensation, Does.Contain("PUT /api/kontributoren/7"));
            Assert.That(befunde[0].Kompensation, Does.Contain("name"));
            Assert.That(befunde[0].Kompensation, Does.Not.Contain("POST"));
        });
    }

    [Test]
    public void Wenn_beim_Aendern_die_Art_unbekannt_ist_dann_nennt_auch_dieser_Befund_die_Aenderungsroute()
    {
        var befunde = KontributorenValidator.Pruefe(7, new KontributorAendernAnfrage("Zora", (Kontributorart)7));

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kontributor-art-unbekannt");
        Assert.That(befunde[0].Kompensation, Does.Contain("PUT /api/kontributoren/7"));
    }

    [Test]
    public void Wenn_beim_Aendern_alle_drei_Arten_geprueft_werden_dann_gibt_keine_von_ihnen_einen_Befund()
    {
        var mensch = KontributorenValidator.Pruefe(1, new KontributorAendernAnfrage("Zora", Kontributorart.Mensch));
        var agent = KontributorenValidator.Pruefe(1, new KontributorAendernAnfrage("Zora", Kontributorart.Agent));
        var abgebildete = KontributorenValidator.Pruefe(1, new KontributorAendernAnfrage("Zora", Kontributorart.Abgebildet));

        Assert.Multiple(() =>
        {
            Assert.That(mensch.IstOhneBefund, Is.True);
            Assert.That(agent.IstOhneBefund, Is.True);
            Assert.That(abgebildete.IstOhneBefund, Is.True);
        });
    }
}
