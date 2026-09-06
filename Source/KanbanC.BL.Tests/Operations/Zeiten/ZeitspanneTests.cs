using KanbanC.BL.Models;
using KanbanC.BL.Operations.Zeiten;
using KanbanC.BL.Tests.TestHelpers;

namespace KanbanC.BL.Tests.Operations.Zeiten;

// Die beiden Invarianten einer von Hand erfassten Spanne, isoliert geprüft — mit der Uhr als
// Parameter, damit beide Ränder der Toleranz ohne Zeitmanipulation nachweisbar sind.
public class ZeitspanneTests
{
    private const string EndeVorBeginn = "zeiteintrag-ende-vor-beginn";
    private const string InDerZukunft = "zeiteintrag-in-der-zukunft";
    private static readonly DateTimeOffset Serveruhr = new(2026, 9, 6, 14, 0, 30, TimeSpan.Zero);

    [Test]
    public void Wenn_das_Ende_nach_dem_Beginn_liegt_dann_bleibt_die_Pruefung_ohne_Befund()
    {
        var beginn = new DateTimeOffset(2026, 9, 5, 14, 0, 0, TimeSpan.Zero);
        var ende = new DateTimeOffset(2026, 9, 5, 15, 30, 0, TimeSpan.Zero);

        var befunde = Zeitspanne.Pruefe(beginn, ende, Serveruhr);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    [Test]
    public void Wenn_das_Ende_vor_dem_Beginn_liegt_dann_nennt_der_Befund_beide_Werte()
    {
        var beginn = new DateTimeOffset(2026, 9, 5, 15, 30, 0, TimeSpan.Zero);
        var ende = new DateTimeOffset(2026, 9, 5, 14, 0, 0, TimeSpan.Zero);

        var befunde = Zeitspanne.Pruefe(beginn, ende, Serveruhr);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], EndeVorBeginn);
        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Meldung, Does.Contain("2026-09-05T15:30:00Z"));
            Assert.That(befunde[0].Meldung, Does.Contain("2026-09-05T14:00:00Z"));
            Assert.That(befunde[0].Meldung, Does.StartWith("Das Ende liegt vor dem Beginn"));
        });
    }

    // Dieselbe Entscheidung wie beim Stopp: beginn == ende ist eine wahre Aussage über eine sehr
    // kurze Arbeit, und eine Zurückweisung hätte als Kompensation nur „nimm eine andere Zahl".
    [Test]
    public void Wenn_Beginn_und_Ende_derselbe_Zeitpunkt_sind_dann_bleibt_die_Pruefung_ohne_Befund()
    {
        var vierzehnUhr = new DateTimeOffset(2026, 9, 5, 14, 0, 0, TimeSpan.Zero);

        var befunde = Zeitspanne.Pruefe(vierzehnUhr, vierzehnUhr, Serveruhr);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Der eine Rand der Toleranz: 30 Sekunden voraus geht durch, weil das Formular minutengenau
    // ist und „bis jetzt" sonst zum Fehler würde.
    [Test]
    public void Wenn_das_Ende_dreissig_Sekunden_nach_der_Serveruhr_liegt_dann_bleibt_die_Pruefung_ohne_Befund()
    {
        var beginn = new DateTimeOffset(2026, 9, 6, 13, 0, 0, TimeSpan.Zero);
        var ende = new DateTimeOffset(2026, 9, 6, 14, 1, 0, TimeSpan.Zero);

        var befunde = Zeitspanne.Pruefe(beginn, ende, Serveruhr);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Der andere Rand: 90 Sekunden voraus liegen außerhalb der Toleranz.
    [Test]
    public void Wenn_das_Ende_neunzig_Sekunden_nach_der_Serveruhr_liegt_dann_meldet_die_Pruefung_die_Zukunft()
    {
        var beginn = new DateTimeOffset(2026, 9, 6, 13, 0, 0, TimeSpan.Zero);
        var ende = new DateTimeOffset(2026, 9, 6, 14, 2, 0, TimeSpan.Zero);

        var befunde = Zeitspanne.Pruefe(beginn, ende, Serveruhr);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], InDerZukunft);
        Assert.Multiple(() =>
        {
            Assert.That(befunde[0].Meldung, Does.Contain("2026-09-06T14:02:00Z"));
            Assert.That(befunde[0].Meldung, Does.Contain("1 Minute"));
        });
    }

    // Genau eine Minute voraus ist noch innerhalb: die Toleranz ist eingeschlossen.
    [Test]
    public void Wenn_das_Ende_genau_eine_Minute_nach_der_Serveruhr_liegt_dann_bleibt_die_Pruefung_ohne_Befund()
    {
        var beginn = new DateTimeOffset(2026, 9, 6, 13, 0, 0, TimeSpan.Zero);
        var ende = Serveruhr.AddMinutes(1);

        var befunde = Zeitspanne.Pruefe(beginn, ende, Serveruhr);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Ein Ende von null heißt „läuft" und ist kein Zeitpunkt: beanstandet wird nur der Beginn.
    [Test]
    public void Wenn_kein_Ende_gesetzt_ist_dann_beanstandet_die_Pruefung_allein_den_Beginn()
    {
        var beginnInDerZukunft = new DateTimeOffset(2026, 9, 7, 9, 0, 0, TimeSpan.Zero);

        var befunde = Zeitspanne.Pruefe(beginnInDerZukunft, ende: null, Serveruhr);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(1));
        Befundpruefung.ErwarteVollstaendigenBefund(befunde[0], InDerZukunft);
        Assert.That(befunde[0].Meldung, Does.StartWith("Der Beginn"));
    }

    [Test]
    public void Wenn_kein_Ende_gesetzt_ist_und_der_Beginn_in_der_Vergangenheit_liegt_dann_bleibt_die_Pruefung_ohne_Befund()
    {
        var beginn = new DateTimeOffset(2026, 9, 6, 9, 12, 0, TimeSpan.Zero);

        var befunde = Zeitspanne.Pruefe(beginn, ende: null, Serveruhr);

        Assert.That(befunde.IstOhneBefund, Is.True);
    }

    // Zwei verletzte Invarianten ergeben zwei Befunde und nicht einen: der Aufrufer soll beide
    // Fehler in einem Durchgang sehen.
    [Test]
    public void Wenn_der_Beginn_in_der_Zukunft_liegt_und_das_Ende_davor_dann_entstehen_zwei_Befunde()
    {
        var beginn = new DateTimeOffset(2026, 9, 7, 15, 0, 0, TimeSpan.Zero);
        var ende = new DateTimeOffset(2026, 9, 7, 14, 0, 0, TimeSpan.Zero);

        var befunde = Zeitspanne.Pruefe(beginn, ende, Serveruhr);

        Assert.That(befunde.BefundAnzahl, Is.EqualTo(2));
        Assert.That(Codes(befunde), Is.EquivalentTo(new[] { EndeVorBeginn, InDerZukunft }));
    }

    // Die Uhr geht als Parameter herein: dieselbe Spanne ist gegen eine spätere Uhr befundfrei.
    [Test]
    public void Wenn_die_Uhr_weitergestellt_wird_dann_ist_dieselbe_Spanne_nicht_mehr_in_der_Zukunft()
    {
        var beginn = new DateTimeOffset(2026, 9, 6, 15, 0, 0, TimeSpan.Zero);
        var ende = new DateTimeOffset(2026, 9, 6, 16, 0, 0, TimeSpan.Zero);

        var gegenFrueheUhr = Zeitspanne.Pruefe(beginn, ende, Serveruhr);
        var gegenSpaeteUhr = Zeitspanne.Pruefe(beginn, ende, new DateTimeOffset(2026, 9, 6, 17, 0, 0, TimeSpan.Zero));

        Assert.That(gegenFrueheUhr.BefundAnzahl, Is.EqualTo(1));
        Assert.That(gegenSpaeteUhr.IstOhneBefund, Is.True);
    }

    private static IReadOnlyList<string> Codes(Pruefbefunde befunde)
    {
        var codes = new List<string>();
        foreach (var befund in befunde)
        {
            codes.Add(befund.Code);
        }

        return codes;
    }
}
