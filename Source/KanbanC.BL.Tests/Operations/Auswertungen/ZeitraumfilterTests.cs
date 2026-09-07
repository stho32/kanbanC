using KanbanC.BL.Operations.Auswertungen;

namespace KanbanC.BL.Tests.Operations.Auswertungen;

// Der Zwilling des Archivfilters: „seit" kommt als Text herein und wird an der Grenze gelesen.
// Ein unlesbarer Wert wird zurückgewiesen statt geworfen oder still ignoriert.
public class ZeitraumfilterTests
{
    private const string Route = "/api/boards/4/kartenklassen/1/burndown";

    [Test]
    public void Wenn_der_Parameter_fehlt_dann_wird_die_Achse_nicht_geschnitten()
    {
        var ergebnis = Zeitraumfilter.Aus(null, Route);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Seit, Is.Null);
    }

    [Test]
    public void Wenn_der_Parameter_leer_ist_dann_wird_die_Achse_nicht_geschnitten()
    {
        var ergebnis = Zeitraumfilter.Aus("   ", Route);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Seit, Is.Null);
    }

    [Test]
    public void Wenn_der_Parameter_ein_Datum_in_ISO_Form_traegt_dann_wird_es_gelesen()
    {
        var ergebnis = Zeitraumfilter.Aus("2026-09-05", Route);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Seit, Is.EqualTo(new DateOnly(2026, 9, 5)));
    }

    [TestCase("gestern")]
    [TestCase("05.09.2026")]
    [TestCase("2026-13-01")]
    [TestCase("2026-9-5")]
    public void Wenn_der_Parameter_unlesbar_ist_dann_kommt_eine_Zurueckweisung_mit_dem_gelesenen_Wert(string abfragewert)
    {
        var ergebnis = Zeitraumfilter.Aus(abfragewert, Route);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
        var befund = ergebnis.Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeitraum-filter-unlesbar"));
            Assert.That(befund.Meldung, Does.Contain(abfragewert));
            Assert.That(befund.Meldung, Does.Contain("yyyy-MM-dd"));
        });
    }

    // Die Kompensation nennt die Adresse, die der Aufrufer wirklich gerufen hat — und die Route
    // ohne Parameter als den Weg zur Standardachse.
    [Test]
    public void Wenn_der_Parameter_unlesbar_ist_dann_nennt_die_Kompensation_die_gerufene_Route_ohne_Parameter()
    {
        var ergebnis = Zeitraumfilter.Aus("gestern", Route);

        Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain($"`{Route}` ohne Parameter"));
    }

    [Test]
    public void Wenn_der_Parameter_unlesbar_ist_dann_wird_nichts_geworfen()
    {
        Assert.That(() => Zeitraumfilter.Aus("gestern", Route), Throws.Nothing);
    }
}
