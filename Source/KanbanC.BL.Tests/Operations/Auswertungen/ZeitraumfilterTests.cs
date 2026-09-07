using KanbanC.BL.Operations.Auswertungen;

namespace KanbanC.BL.Tests.Operations.Auswertungen;

// Der Zwilling des Archivfilters: eine Tagesgrenze kommt als Text herein und wird an der Grenze
// gelesen. Ein unlesbarer Wert wird zurückgewiesen statt geworfen oder still ignoriert.
// Der Name der Grenze kommt als Eingang — der Burndown ruft mit „seit", der Zeitexport mit „von"
// und „bis".
public class ZeitraumfilterTests
{
    private const string Route = "/api/boards/4/kartenklassen/1/burndown";
    private const string Exportroute = "/api/boards/4/kartenklassen/1/zeitexport.csv";
    private const string Seitgrenze = "seit";

    [Test]
    public void Wenn_der_Parameter_fehlt_dann_wird_die_Achse_nicht_geschnitten()
    {
        var ergebnis = Zeitraumfilter.Aus(null, Seitgrenze, Route);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Von, Is.Null);
    }

    [Test]
    public void Wenn_der_Parameter_leer_ist_dann_wird_die_Achse_nicht_geschnitten()
    {
        var ergebnis = Zeitraumfilter.Aus("   ", Seitgrenze, Route);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Von, Is.Null);
    }

    [Test]
    public void Wenn_der_Parameter_ein_Datum_in_ISO_Form_traegt_dann_wird_es_gelesen()
    {
        var ergebnis = Zeitraumfilter.Aus("2026-09-05", Seitgrenze, Route);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Von, Is.EqualTo(new DateOnly(2026, 9, 5)));
    }

    [TestCase("gestern")]
    [TestCase("05.09.2026")]
    [TestCase("2026-13-01")]
    [TestCase("2026-9-5")]
    public void Wenn_der_Parameter_unlesbar_ist_dann_kommt_eine_Zurueckweisung_mit_dem_gelesenen_Wert(string abfragewert)
    {
        var ergebnis = Zeitraumfilter.Aus(abfragewert, Seitgrenze, Route);

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
    // ohne Parameter als den Weg zum ungeschnittenen Bestand.
    [Test]
    public void Wenn_der_Parameter_unlesbar_ist_dann_nennt_die_Kompensation_die_gerufene_Route_ohne_Parameter()
    {
        var ergebnis = Zeitraumfilter.Aus("gestern", Seitgrenze, Route);

        Assert.That(ergebnis.Befunde.BefundAnzahl, Is.EqualTo(1));
        Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain($"`{Route}` ohne Parameter"));
    }

    [Test]
    public void Wenn_der_Parameter_unlesbar_ist_dann_wird_nichts_geworfen()
    {
        Assert.That(() => Zeitraumfilter.Aus("gestern", Seitgrenze, Route), Throws.Nothing);
    }

    // Die Meldung nennt **welche** der beiden Grenzen gemeint ist: „von“ und „bis“ tragen denselben
    // Befundcode, und ohne den Namen wüsste der Aufrufer nicht, welchen Wert er reparieren soll.
    [TestCase("von")]
    [TestCase("bis")]
    public void Wenn_eine_benannte_Grenze_unlesbar_ist_dann_nennt_die_Meldung_ihren_Namen(string parametername)
    {
        var ergebnis = Zeitraumfilter.Aus("gestern", parametername, Exportroute);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain(parametername));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain($"?{parametername}="));
        });
    }

    [Test]
    public void Wenn_beide_Grenzen_fehlen_dann_schneidet_keine()
    {
        var ergebnis = Zeitraumfilter.AusPaar(null, null, Exportroute);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Von, Is.Null);
            Assert.That(ergebnis.Wert.Bis, Is.Null);
        });
    }

    [Test]
    public void Wenn_beide_Grenzen_lesbar_sind_dann_stehen_beide_in_der_Wahl()
    {
        var ergebnis = Zeitraumfilter.AusPaar("2026-09-06", "2026-09-07", Exportroute);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Wert.Von, Is.EqualTo(new DateOnly(2026, 9, 6)));
            Assert.That(ergebnis.Wert.Bis, Is.EqualTo(new DateOnly(2026, 9, 7)));
        });
    }

    [Test]
    public void Wenn_nur_eine_der_beiden_Grenzen_gesetzt_ist_dann_bleibt_die_andere_offen()
    {
        var nurVon = Zeitraumfilter.AusPaar("2026-09-01", null, Exportroute);
        var nurBis = Zeitraumfilter.AusPaar(null, "2026-09-06", Exportroute);

        Assert.Multiple(() =>
        {
            Assert.That(nurVon.Wert.Von, Is.EqualTo(new DateOnly(2026, 9, 1)));
            Assert.That(nurVon.Wert.Bis, Is.Null);
            Assert.That(nurBis.Wert.Von, Is.Null);
            Assert.That(nurBis.Wert.Bis, Is.EqualTo(new DateOnly(2026, 9, 6)));
        });
    }

    [Test]
    public void Wenn_eine_der_beiden_Grenzen_unlesbar_ist_dann_kommt_ihr_eigener_Befund()
    {
        var ergebnis = Zeitraumfilter.AusPaar("2026-09-01", "uebermorgen", Exportroute);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("zeitraum-filter-unlesbar"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("uebermorgen"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("bis"));
        });
    }

    // Die verdrehte Spanne ist dieselbe Grenze wie die Lesbarkeit: sie wird hier geprüft und nicht
    // im Dienst und nicht im Schirm.
    [Test]
    public void Wenn_bis_vor_von_liegt_dann_kommt_eine_Zurueckweisung_mit_beiden_gelesenen_Werten()
    {
        var ergebnis = Zeitraumfilter.AusPaar("2026-09-07", "2026-09-01", Exportroute);

        Assert.That(ergebnis.IstErfolg, Is.False);
        var befund = ergebnis.Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("zeitraum-filter-verdreht"));
            Assert.That(befund.Meldung, Does.Contain("2026-09-07"));
            Assert.That(befund.Meldung, Does.Contain("2026-09-01"));
            Assert.That(befund.Kompensation, Does.Contain("tauschen"));
            Assert.That(befund.Kompensation, Does.Contain("?von=2026-09-01&bis=2026-09-07"));
        });
    }

    [Test]
    public void Wenn_beide_Grenzen_denselben_Tag_nennen_dann_ist_die_Spanne_nicht_verdreht()
    {
        var ergebnis = Zeitraumfilter.AusPaar("2026-09-06", "2026-09-06", Exportroute);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Von, Is.EqualTo(ergebnis.Wert.Bis));
    }
}
