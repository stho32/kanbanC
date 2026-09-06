using KanbanC.BL.Models;
using KanbanC.BL.Operations.Klassen;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.Operations.Klassen;

public class KartenklassenValidatorTests
{
    private const long BoardId = 2;
    private static readonly IReadOnlyList<Kartenklasse> OhneVergebene = [];

    [Test]
    public void Wenn_Name_und_Praefix_stimmen_dann_gibt_es_keinen_Befund()
    {
        var befunde = Pruefe("WBS", "WBS-", OhneVergebene);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_der_Name_leer_ist_dann_meldet_der_Befund_dass_eine_Klasse_einen_Namen_braucht()
    {
        var befunde = Pruefe("", "WBS-", OhneVergebene);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartenklasse-name-leer");
        Assert.That(befunde[0].Meldung, Is.EqualTo("Eine Klasse braucht einen Namen."));
    }

    [Test]
    public void Wenn_der_Name_nur_aus_Leerzeichen_besteht_dann_gilt_er_als_leer()
    {
        var befunde = Pruefe("   ", "WBS-", OhneVergebene);

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartenklasse-name-leer");
    }

    [Test]
    public void Wenn_das_Praefix_leer_ist_dann_wird_es_zurueckgewiesen()
    {
        var befunde = Pruefe("WBS", "  ", OhneVergebene);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartenklasse-praefix-leer");
    }

    // Eine Kartennummer wandert in Zweignamen, Meldungen und Suchfelder — ein Leerzeichen darin
    // hielte dort nicht.
    [Test]
    public void Wenn_das_Praefix_ein_Leerzeichen_traegt_dann_wird_es_zurueckgewiesen()
    {
        var befunde = Pruefe("WBS", "WB S-", OhneVergebene);

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartenklasse-praefix-ungueltig");
    }

    [Test]
    [TestCase("WBS/")]
    [TestCase("WBS.")]
    [TestCase("WBS#")]
    public void Wenn_das_Praefix_ein_Zeichen_ausserhalb_des_Vorrats_traegt_dann_wird_es_zurueckgewiesen(string praefix)
    {
        var befunde = Pruefe("WBS", praefix, OhneVergebene);

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartenklasse-praefix-ungueltig");
    }

    [Test]
    [TestCase("WBS-")]
    [TestCase("WBS_2")]
    [TestCase("WBS")]
    [TestCase("wbs-")]
    public void Wenn_das_Praefix_aus_erlaubten_Zeichen_besteht_dann_geht_es_durch(string praefix)
    {
        var befunde = Pruefe("WBS", praefix, OhneVergebene);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_das_Praefix_genau_die_Hoechstlaenge_hat_dann_geht_es_durch()
    {
        var befunde = Pruefe("Auslieferung", "ABCDEFGH", OhneVergebene);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_das_Praefix_ueber_der_Hoechstlaenge_liegt_dann_nennt_der_Befund_die_Hoechstlaenge()
    {
        var befunde = Pruefe("Auslieferung", "ABCDEFGHI", OhneVergebene);

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartenklasse-praefix-ungueltig");
        Assert.That(befunde[0].Meldung, Does.Contain("8"));
    }

    [Test]
    public void Wenn_das_Praefix_auf_diesem_Board_vergeben_ist_dann_nennt_der_Befund_die_haltende_Klasse()
    {
        var befunde = Pruefe("Arbeitspakete", "WBS-", [new Kartenklasse(1, "WBS", "WBS-", 0)]);

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartenklasse-praefix-vergeben");
        Assert.That(befunde[0].Meldung, Is.EqualTo("Das Präfix WBS- führt auf diesem Board schon die Klasse „WBS“. Wähle ein anderes Präfix."));
    }

    [Test]
    public void Wenn_das_vergebene_Praefix_in_anderer_Schreibweise_kommt_dann_greift_die_Pruefung_trotzdem()
    {
        var befunde = Pruefe("Arbeitspakete", " wbs- ", [new Kartenklasse(1, "WBS", "WBS-", 0)]);

        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "kartenklasse-praefix-vergeben");
    }

    // Die Identitaet einer Kartenklasse ist ihr Praefix, nicht ihr Name.
    [Test]
    public void Wenn_der_Name_schon_vergeben_ist_das_Praefix_aber_frei_dann_gibt_es_keinen_Befund()
    {
        var befunde = Pruefe("WBS", "WB2-", [new Kartenklasse(1, "WBS", "WBS-", 0)]);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_ein_Befund_entsteht_dann_nennt_seine_Kompensation_die_Route_samt_Boardnummer()
    {
        var befunde = Pruefe("", "WBS-", OhneVergebene);

        Assert.That(befunde[0].Kompensation, Does.Contain("POST /api/boards/2/kartenklassen"));
    }

    private static Pruefbefunde Pruefe(string name, string praefix, IReadOnlyList<Kartenklasse> vergebene)
    {
        return KartenklassenValidator.Pruefe(BoardId, new KartenklasseAnlegenAnfrage(name, praefix), vergebene);
    }
}
