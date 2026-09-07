using KanbanC.Blazor.Services;

namespace KanbanC.Blazor.Tests.Services;

// Was über den Bahnen steht, nachdem die Leitung zurück ist — und wann dort nichts steht. Die
// Ränder (null Änderungen, die Schwelle) sind über den Browser nur mit einer echten Trennung je
// Fall zu zeigen; die Zusage selbst ist eine Rechnung.
public class AufschliessbandTests
{
    private static readonly DateTimeOffset Abriss = Zeitpunktform.AusOrtszeit(new DateOnly(2026, 9, 7), new TimeOnly(9, 12));
    private static readonly Aufschliessschwelle Zehn = new(10);

    [Test]
    public void Wenn_sich_nichts_geaendert_hat_dann_gibt_es_kein_Band()
    {
        var band = Aufschliessband.Fuer(0, Abriss, Zehn);

        Assert.That(band, Is.Null);
    }

    // Wortlaut der Anforderung: „Wieder verbunden. 4 Änderungen seit 09:12 sind nachgeholt und
    // unten markiert."
    [Test]
    public void Wenn_vier_Karten_sich_bewegt_haben_dann_nennt_das_Band_Zahl_Zeitpunkt_und_die_Marken()
    {
        var band = Aufschliessband.Fuer(4, Abriss, Zehn);

        Assert.Multiple(() =>
        {
            Assert.That(band!.Beschriftung, Is.EqualTo("Wieder verbunden. 4 Änderungen seit 09:12 sind nachgeholt und unten markiert."));
            Assert.That(band.ZeigtMarken, Is.True);
        });
    }

    [Test]
    public void Wenn_genau_eine_Karte_sich_bewegt_hat_dann_steht_das_Band_im_Singular()
    {
        var band = Aufschliessband.Fuer(1, Abriss, Zehn);

        Assert.That(band!.Beschriftung, Is.EqualTo("Wieder verbunden. 1 Änderung seit 09:12 ist nachgeholt und unten markiert."));
    }

    // Über der Schwelle nennt das Band nur die Zahl — und verspricht keine Marken, die es dann
    // nicht gibt.
    [Test]
    public void Wenn_die_Schwelle_ueberschritten_ist_dann_nennt_das_Band_nur_die_Zahl()
    {
        var band = Aufschliessband.Fuer(25, Abriss, Zehn);

        Assert.Multiple(() =>
        {
            Assert.That(band!.Beschriftung, Is.EqualTo("Wieder verbunden. 25 Änderungen seit 09:12 sind nachgeholt."));
            Assert.That(band.ZeigtMarken, Is.False);
            Assert.That(band.Beschriftung, Does.Not.Contain("markiert"));
        });
    }

    // Genau auf der Schwelle wird noch markiert: „ab wann nur noch gezählt wird" heißt darüber.
    [Test]
    public void Wenn_genau_so_viele_Karten_wie_die_Schwelle_sich_bewegt_haben_dann_bleibt_es_bei_den_Marken()
    {
        var band = Aufschliessband.Fuer(10, Abriss, Zehn);

        Assert.That(band!.ZeigtMarken, Is.True);
    }

    // Die Schwelle ist eine gesetzte Größenordnung und keine Konstante im Renderzweig: eine
    // gesenkte gilt.
    [Test]
    public void Wenn_die_Schwelle_gesenkt_ist_dann_bleibt_es_frueher_bei_der_Zahl()
    {
        var band = Aufschliessband.Fuer(2, Abriss, new Aufschliessschwelle(1));

        Assert.That(band!.ZeigtMarken, Is.False);
    }
}
