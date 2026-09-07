using KanbanC.BL.Models.Auswertungen;
using KanbanC.BL.Operations.Auswertungen;

namespace KanbanC.BL.Tests.Operations.Auswertungen;

// Das Ende ist immer heute; der Anfang ist das angefragte „seit", sonst der früheste Abschluss,
// sonst heute. Ein „bis" gibt es nicht.
public class BurndownzeitraumTests
{
    private static readonly DateOnly Heute = new(2026, 9, 7);
    private static readonly DateOnly DritterSeptember = new(2026, 9, 3);
    private static readonly DateOnly FuenfterSeptember = new(2026, 9, 5);

    [Test]
    public void Wenn_kein_Zeitraum_gewaehlt_ist_dann_beginnt_die_Achse_beim_fruehesten_Erledigungstag()
    {
        var bestand = new Erledigungsstandkarten([Karte(1, FuenfterSeptember), Karte(2, DritterSeptember)]);

        var achse = Burndownzeitraum.Bestimme(bestand, seit: null, Heute);

        Assert.Multiple(() =>
        {
            Assert.That(achse[0], Is.EqualTo(DritterSeptember));
            Assert.That(achse[achse.Tageanzahl - 1], Is.EqualTo(Heute));
            Assert.That(achse.Tageanzahl, Is.EqualTo(5));
        });
    }

    [Test]
    public void Wenn_ein_Zeitraum_gewaehlt_ist_dann_beginnt_die_Achse_an_seinem_Tag()
    {
        var bestand = new Erledigungsstandkarten([Karte(1, DritterSeptember)]);

        var achse = Burndownzeitraum.Bestimme(bestand, FuenfterSeptember, Heute);

        Assert.Multiple(() =>
        {
            Assert.That(achse[0], Is.EqualTo(FuenfterSeptember));
            Assert.That(achse.Tageanzahl, Is.EqualTo(3));
        });
    }

    [Test]
    public void Wenn_der_gewaehlte_Beginn_vor_dem_fruehesten_Abschluss_liegt_dann_laeuft_die_Achse_ab_ihm()
    {
        var bestand = new Erledigungsstandkarten([Karte(1, DritterSeptember)]);

        var achse = Burndownzeitraum.Bestimme(bestand, new DateOnly(2026, 9, 1), Heute);

        Assert.Multiple(() =>
        {
            Assert.That(achse[0], Is.EqualTo(new DateOnly(2026, 9, 1)));
            Assert.That(achse.Tageanzahl, Is.EqualTo(7));
        });
    }

    // Eine leere Achse wäre keine Antwort: ein Beginn nach heute liefert genau den Tag heute.
    [Test]
    public void Wenn_der_gewaehlte_Beginn_nach_heute_liegt_dann_steht_genau_der_eine_Tag_heute_da()
    {
        var bestand = new Erledigungsstandkarten([Karte(1, DritterSeptember)]);

        var achse = Burndownzeitraum.Bestimme(bestand, new DateOnly(2026, 9, 20), Heute);

        Assert.Multiple(() =>
        {
            Assert.That(achse.Tageanzahl, Is.EqualTo(1));
            Assert.That(achse[0], Is.EqualTo(Heute));
        });
    }

    [Test]
    public void Wenn_keine_Karte_ein_Erledigungsdatum_traegt_dann_steht_genau_der_eine_Tag_heute_da()
    {
        var bestand = new Erledigungsstandkarten([
            new Erledigungsstandkarte(1, "WBS-01", "[I0001] Board anlegen", null, IstArchiviert: false, StehtInAbschlussspalte: false),
        ]);

        var achse = Burndownzeitraum.Bestimme(bestand, seit: null, Heute);

        Assert.Multiple(() =>
        {
            Assert.That(achse.Tageanzahl, Is.EqualTo(1));
            Assert.That(achse[0], Is.EqualTo(Heute));
        });
    }

    [Test]
    public void Wenn_der_Bestand_leer_ist_dann_steht_genau_der_eine_Tag_heute_da()
    {
        var achse = Burndownzeitraum.Bestimme(new Erledigungsstandkarten([]), seit: null, Heute);

        Assert.Multiple(() =>
        {
            Assert.That(achse.Tageanzahl, Is.EqualTo(1));
            Assert.That(achse[0], Is.EqualTo(Heute));
        });
    }

    // Ein künftiges Erledigungsdatum verlängert die Achse nicht — sie endet heute.
    [Test]
    public void Wenn_eine_Karte_ein_kuenftiges_Erledigungsdatum_traegt_dann_endet_die_Achse_trotzdem_heute()
    {
        var bestand = new Erledigungsstandkarten([Karte(1, DritterSeptember), Karte(2, new DateOnly(2026, 9, 9))]);

        var achse = Burndownzeitraum.Bestimme(bestand, seit: null, Heute);

        Assert.That(achse[achse.Tageanzahl - 1], Is.EqualTo(Heute));
    }

    private static Erledigungsstandkarte Karte(long karteId, DateOnly erledigtAm)
    {
        return new Erledigungsstandkarte(karteId, $"WBS-0{karteId}", "[I0001] Board anlegen", erledigtAm, IstArchiviert: false, StehtInAbschlussspalte: true);
    }
}
