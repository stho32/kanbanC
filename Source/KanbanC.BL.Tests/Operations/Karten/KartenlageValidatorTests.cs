using KanbanC.BL.Operations.Karten;
using KanbanC.BL.Tests.TestHelpers;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Karten;

namespace KanbanC.BL.Tests.Operations.Karten;

public class KartenlageValidatorTests
{
    private static readonly Spalte InArbeit = new(7, "In Arbeit", 2, false, null, [], Kartenzahl: 0);

    [Test]
    public void Wenn_die_Position_1_ist_dann_gibt_es_keinen_Befund()
    {
        var befunde = KartenlageValidator.Pruefe(3, InArbeit, 4, new Kartenlage(7, 1));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_die_Position_die_Hoechstposition_ist_dann_gibt_es_keinen_Befund()
    {
        var befunde = KartenlageValidator.Pruefe(3, InArbeit, 4, new Kartenlage(7, 4));

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_die_Position_0_ist_dann_wird_sie_zurueckgewiesen()
    {
        var befunde = KartenlageValidator.Pruefe(3, InArbeit, 4, new Kartenlage(7, 0));

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], "position-ausserhalb");
    }

    [Test]
    public void Wenn_die_Position_negativ_ist_dann_wird_sie_zurueckgewiesen()
    {
        var befunde = KartenlageValidator.Pruefe(3, InArbeit, 4, new Kartenlage(7, -1));

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Code, Is.EqualTo("position-ausserhalb"));
    }

    [Test]
    public void Wenn_die_Position_eins_ueber_der_Hoechstposition_liegt_dann_wird_sie_zurueckgewiesen()
    {
        var befunde = KartenlageValidator.Pruefe(3, InArbeit, 4, new Kartenlage(7, 5));

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(befunde[0].Code, Is.EqualTo("position-ausserhalb"));
    }

    [Test]
    public void Wenn_eine_Position_zurueckgewiesen_wird_dann_nennt_die_Meldung_die_gelieferte_Position_die_Spalte_und_die_Obergrenze()
    {
        var befunde = KartenlageValidator.Pruefe(3, InArbeit, 4, new Kartenlage(7, 5));

        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Meldung, Does.Contain("Position 5"));
            Assert.That(befunde[0].Meldung, Does.Contain("In Arbeit"));
            Assert.That(befunde[0].Meldung, Does.Contain("SpalteId 7"));
            Assert.That(befunde[0].Meldung, Does.Contain("4 Karten"));
            Assert.That(befunde[0].Meldung, Does.Contain("gültig sind 1 bis 4"));
        });
    }

    [Test]
    public void Wenn_eine_Position_zurueckgewiesen_wird_dann_nennt_die_Kompensationsaktion_die_Route_des_Boards_und_den_gueltigen_Bereich()
    {
        var befunde = KartenlageValidator.Pruefe(3, InArbeit, 4, new Kartenlage(7, 5));

        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Kompensation, Does.Contain("GET /api/boards/3"));
            Assert.That(befunde[0].Kompensation, Does.Contain("zwischen 1 und 4"));
        });
    }

    // Rechenbeispiel der Anforderung: liegt die Karte schon in der Zielspalte, traegt diese nach
    // dem Zug unveraendert 3 Karten — Position 4 ist dann keine gueltige Stelle mehr.
    [Test]
    public void Wenn_die_Zielspalte_nach_dem_Zug_drei_Karten_traegt_dann_ist_1_gueltig_und_4_nicht()
    {
        var innerhalb = KartenlageValidator.Pruefe(3, InArbeit, 3, new Kartenlage(7, 3));
        var ausserhalb = KartenlageValidator.Pruefe(3, InArbeit, 3, new Kartenlage(7, 4));

        Assert.Multiple(() =>
        {
            Assert.That(innerhalb.IstOhneBefund, Is.True);
            Assert.That(ausserhalb.IstOhneBefund, Is.False);
        });
    }

    [Test]
    public void Wenn_die_Zielspalte_nach_dem_Zug_genau_eine_Karte_traegt_dann_nennt_die_Meldung_sie_in_der_Einzahl()
    {
        var befunde = KartenlageValidator.Pruefe(3, InArbeit, 1, new Kartenlage(7, 2));

        Assert.That(befunde[0].Meldung, Does.Contain("1 Karte,"));
    }
}
