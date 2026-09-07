using KanbanC.BL.Models.Auswertungen;

namespace KanbanC.BL.Tests.Models.Auswertungen;

// Lückenlos ist die ganze Aussage dieses Typs.
public class KalenderachseTests
{
    [Test]
    public void Wenn_die_Achse_ueber_fuenf_Tage_laeuft_dann_steht_jeder_Tag_dazwischen_darin()
    {
        var achse = new Kalenderachse(new DateOnly(2026, 9, 3), new DateOnly(2026, 9, 7));

        var tage = new List<DateOnly>();
        foreach (var tag in achse)
        {
            tage.Add(tag);
        }

        Assert.That(tage, Is.EqualTo(new[]
        {
            new DateOnly(2026, 9, 3),
            new DateOnly(2026, 9, 4),
            new DateOnly(2026, 9, 5),
            new DateOnly(2026, 9, 6),
            new DateOnly(2026, 9, 7),
        }));
    }

    [Test]
    public void Wenn_erster_und_letzter_Tag_derselbe_sind_dann_traegt_die_Achse_genau_einen_Tag()
    {
        var achse = new Kalenderachse(new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 7));

        Assert.Multiple(() =>
        {
            Assert.That(achse.Tageanzahl, Is.EqualTo(1));
            Assert.That(achse[0], Is.EqualTo(new DateOnly(2026, 9, 7)));
        });
    }

    // Über einen Monatswechsel hinweg zählt der Kalender, nicht die Tageszahl im Monat.
    [Test]
    public void Wenn_die_Achse_ueber_einen_Monatswechsel_laeuft_dann_zaehlt_der_Kalender()
    {
        var achse = new Kalenderachse(new DateOnly(2026, 8, 30), new DateOnly(2026, 9, 2));

        Assert.Multiple(() =>
        {
            Assert.That(achse.Tageanzahl, Is.EqualTo(4));
            Assert.That(achse[2], Is.EqualTo(new DateOnly(2026, 9, 1)));
        });
    }

    [Test]
    public void Wenn_die_Achse_vor_ihrem_Beginn_enden_soll_dann_wird_sie_zurueckgewiesen()
    {
        Assert.That(
            () => new Kalenderachse(new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 3)),
            Throws.InstanceOf<ArgumentException>());
    }
}
