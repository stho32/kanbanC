using KanbanC.Blazor.Services;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Tests.Services;

public class KartenfarbformTests
{
    [Test]
    public void Wenn_die_Farben_aufgezaehlt_werden_dann_stehen_alle_fuenf_mit_ohne_zuerst_da()
    {
        Assert.That(Kartenfarbform.Alle, Is.EqualTo(new[]
        {
            Kartenfarbe.Ohne, Kartenfarbe.Sand, Kartenfarbe.Terrakotta, Kartenfarbe.Olive, Kartenfarbe.Nebel,
        }));
    }

    [Test]
    public void Wenn_eine_Farbe_beschriftet_wird_dann_traegt_ohne_die_Kleinschreibung_und_die_uebrigen_ihren_Namen()
    {
        var beschriftungen = Kartenfarbform.Alle.Select(Kartenfarbform.Beschriftung);

        Assert.That(beschriftungen, Is.EqualTo(new[] { "ohne", "Sand", "Terrakotta", "Olive", "Nebel" }));
    }

    [Test]
    public void Wenn_jede_Farbe_ihren_Punkt_bekommt_dann_traegt_keine_zwei_dieselbe_Klasse()
    {
        var klassen = Kartenfarbform.Alle.Select(Kartenfarbform.Punktklasse).ToList();

        Assert.That(klassen, Is.Unique);
        Assert.That(klassen, Has.All.StartsWith("farbpunkt-"));
    }

    [Test]
    public void Wenn_eine_unbekannte_Farbe_beschriftet_werden_soll_dann_faellt_das_auf_statt_still_zu_bleiben()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Kartenfarbform.Beschriftung((Kartenfarbe)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => Kartenfarbform.Punktklasse((Kartenfarbe)99));
        });
    }
}
