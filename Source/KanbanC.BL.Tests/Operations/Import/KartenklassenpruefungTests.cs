using KanbanC.BL.Operations.Import;
using KanbanC.Contracts.Klassen;

namespace KanbanC.BL.Tests.Operations.Import;

public class KartenklassenpruefungTests
{
    private static readonly IReadOnlyList<Kartenklasse> WbsKlasse = [new Kartenklasse(3, "WBS", "WBS-", 0)];

    [Test]
    public void Wenn_die_Klasse_dem_Board_gehoert_dann_gibt_es_keinen_Befund()
    {
        var befund = Kartenklassenpruefung.Pruefe(4, "Umsetzung", 3, WbsKlasse, 4);

        Assert.That(befund, Is.Null);
    }

    // Der Import legt nie eine Klasse an — die Zurückweisung führt zu der Stelle, die es tut.
    [Test]
    public void Wenn_das_Board_keine_Klasse_fuehrt_dann_nennt_der_Befund_Boardname_und_den_Weg_zur_Klassenpflege()
    {
        var befund = Kartenklassenpruefung.Pruefe(5, "Ideen", 3, [], null);

        Assert.That(befund, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(befund!.Code, Is.EqualTo("board-ohne-kartenklasse"));
            Assert.That(befund.Meldung, Does.Contain("Ideen"));
            Assert.That(befund.Kompensation, Does.Contain("Layout-Modus"));
            Assert.That(befund.Kompensation, Does.Contain("/api/boards/5/kartenklassen"));
        });
    }

    [Test]
    public void Wenn_die_Klasse_einem_anderen_Board_gehoert_dann_sagt_der_Befund_welchem()
    {
        var befund = Kartenklassenpruefung.Pruefe(4, "Umsetzung", 9, WbsKlasse, 7);

        Assert.That(befund, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(befund!.Code, Is.EqualTo("kartenklasse-fremd"));
            Assert.That(befund.Meldung, Does.Contain("7"));
        });
    }

    [Test]
    public void Wenn_es_die_Klasse_nirgends_gibt_dann_schickt_der_Befund_an_die_Klassenliste_des_Boards()
    {
        var befund = Kartenklassenpruefung.Pruefe(4, "Umsetzung", 99, WbsKlasse, null);

        Assert.That(befund, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(befund!.Code, Is.EqualTo("kartenklasse-unbekannt"));
            Assert.That(befund.Kompensation, Does.Contain("/api/boards/4/kartenklassen"));
        });
    }
}
