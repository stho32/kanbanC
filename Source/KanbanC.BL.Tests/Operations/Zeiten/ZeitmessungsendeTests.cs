using KanbanC.BL.Operations.Zeiten;

namespace KanbanC.BL.Tests.Operations.Zeiten;

// Beide Randfälle der Zeitmessung an einer isoliert prüfbaren Stelle: Dauer null ist erlaubt,
// eine negative Dauer entsteht nie.
public class ZeitmessungsendeTests
{
    private static readonly DateTimeOffset AchtUhrVier = new(2026, 9, 6, 8, 4, 0, TimeSpan.Zero);

    [Test]
    public void Wenn_die_Uhr_nach_dem_Beginn_steht_dann_ist_das_Ende_die_Uhrzeit()
    {
        var neunUhrVierzig = new DateTimeOffset(2026, 9, 6, 9, 40, 0, TimeSpan.Zero);

        var ende = Zeitmessungsende.Fuer(AchtUhrVier, neunUhrVierzig);

        Assert.That(ende, Is.EqualTo(neunUhrVierzig));
        Assert.That(ende - AchtUhrVier, Is.EqualTo(TimeSpan.FromMinutes(96)));
    }

    // Start und Stopp im selben Zeitpunkt sind eine wahre Aussage über eine sehr kurze Messung
    // und keine Fehleingabe.
    [Test]
    public void Wenn_die_Uhr_genau_auf_dem_Beginn_steht_dann_ist_die_Dauer_null_und_das_Ende_gilt()
    {
        var ende = Zeitmessungsende.Fuer(AchtUhrVier, AchtUhrVier);

        Assert.That(ende, Is.EqualTo(AchtUhrVier));
        Assert.That(ende - AchtUhrVier, Is.EqualTo(TimeSpan.Zero));
    }

    // Beginn 08:04:00, Uhr beim Stopp 08:03:30 — das Ende ist 08:04:00 und nicht 08:03:30, die
    // Dauer 0:00 und nicht minus 30 Sekunden.
    [Test]
    public void Wenn_die_Uhr_hinter_den_Beginn_zurueckgesprungen_ist_dann_wird_das_Ende_auf_den_Beginn_geklemmt()
    {
        var einehalbeMinuteVorAchtUhrVier = new DateTimeOffset(2026, 9, 6, 8, 3, 30, TimeSpan.Zero);

        var ende = Zeitmessungsende.Fuer(AchtUhrVier, einehalbeMinuteVorAchtUhrVier);

        Assert.That(ende, Is.EqualTo(AchtUhrVier));
        Assert.That(ende - AchtUhrVier, Is.EqualTo(TimeSpan.Zero));
        Assert.That(ende, Is.Not.EqualTo(einehalbeMinuteVorAchtUhrVier));
    }

    // Der Versatz der Uhr entscheidet nicht mit: gerechnet wird über den Zeitpunkt, nicht über
    // seine Schreibweise.
    [Test]
    public void Wenn_die_Uhr_in_einer_anderen_Zeitzone_laeuft_dann_entscheidet_der_Zeitpunkt_und_nicht_der_Versatz()
    {
        var zehnUhrVierMitteleuropaeisch = new DateTimeOffset(2026, 9, 6, 10, 4, 0, TimeSpan.FromHours(2));

        var ende = Zeitmessungsende.Fuer(AchtUhrVier, zehnUhrVierMitteleuropaeisch);

        Assert.That(ende, Is.EqualTo(zehnUhrVierMitteleuropaeisch));
        Assert.That(ende - AchtUhrVier, Is.EqualTo(TimeSpan.Zero));
    }
}
