using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Die Dauer einer Zeile und die Summe einer Karte tragen dieselbe Form: „h:mm". Geprüft wird die
// Zahl, die dabei herauskommt — reine Operation, kein Browser noetig.
public class DauerformTests
{
    [Test]
    public void Wenn_eine_Dauer_erscheint_dann_steht_die_Stunde_ohne_fuehrende_Null_und_die_Minute_zweistellig()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Dauerform.AlsText(new TimeSpan(2, 14, 0)), Is.EqualTo("2:14"));
            Assert.That(Dauerform.AlsText(new TimeSpan(0, 45, 0)), Is.EqualTo("0:45"));
            Assert.That(Dauerform.AlsText(new TimeSpan(0, 37, 0)), Is.EqualTo("0:37"));
            Assert.That(Dauerform.AlsText(new TimeSpan(1, 22, 0)), Is.EqualTo("1:22"));
        });
    }

    [Test]
    public void Wenn_die_Minute_einstellig_waere_dann_traegt_sie_trotzdem_zwei_Stellen()
    {
        Assert.That(Dauerform.AlsText(new TimeSpan(0, 5, 0)), Is.EqualTo("0:05"));
    }

    // Eine Karte sammelt Arbeitszeit ueber Wochen: die Stunden laufen ueber 24 hinaus, statt in
    // Tage umzubrechen. „1:02:03" waere eine dritte Zeitform in derselben Spalte.
    [Test]
    public void Wenn_eine_Dauer_jenseits_eines_Tages_liegt_dann_laufen_die_Stunden_weiter()
    {
        Assert.That(Dauerform.AlsText(new TimeSpan(1, 2, 3, 0)), Is.EqualTo("26:03"));
    }

    // US-8: drei ueberlappende Zehnstuender desselben Tages ergeben 30:00 — weder auf 24 Stunden
    // gekappt noch als Tag ausgewiesen.
    [Test]
    public void Wenn_drei_ueberlappende_Zehnstuender_summiert_werden_dann_steht_dort_30_00()
    {
        var summe = TimeSpan.FromHours(10) + TimeSpan.FromHours(10) + TimeSpan.FromHours(10);

        Assert.That(Dauerform.AlsText(summe), Is.EqualTo("30:00"));
    }

    [Test]
    public void Wenn_eine_Spanne_unter_einer_Minute_liegt_dann_steht_dort_0_00()
    {
        Assert.That(Dauerform.AlsText(TimeSpan.FromSeconds(42)), Is.EqualTo("0:00"));
    }

    // Uhren, die auseinanderlaufen, sind keine Aussage ueber geleistete Arbeit: eine negative
    // Spanne wird zu „0:00" und traegt nie ein Minuszeichen.
    [Test]
    public void Wenn_eine_Spanne_rueckwaerts_laeuft_dann_steht_dort_0_00_und_kein_Minuszeichen()
    {
        var rueckwaerts = Dauerform.AlsText(TimeSpan.FromMinutes(-90));

        Assert.Multiple(() =>
        {
            Assert.That(rueckwaerts, Is.EqualTo("0:00"));
            Assert.That(rueckwaerts, Does.Not.Contain("-"));
        });
    }
}
