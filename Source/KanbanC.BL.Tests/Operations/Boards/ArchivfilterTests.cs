using KanbanC.BL.Operations.Boards;
using KanbanC.BL.Tests.TestHelpers;

namespace KanbanC.BL.Tests.Operations.Boards;

public class ArchivfilterTests
{
    private const string Listenroute = "GET /api/boards";

    [Test]
    public void Wenn_der_Parameter_fehlt_dann_gilt_die_Standardliste()
    {
        var ergebnis = Archivfilter.Aus(null, Listenroute);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.IstArchiviert, Is.False);
    }

    [Test]
    public void Wenn_der_Parameter_leer_ist_dann_gilt_ebenfalls_die_Standardliste()
    {
        var ergebnis = Archivfilter.Aus("  ", Listenroute);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.IstArchiviert, Is.False);
    }

    [Test]
    public void Wenn_der_Parameter_true_traegt_dann_sind_die_archivierten_gemeint()
    {
        var ergebnis = Archivfilter.Aus("true", Listenroute);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.IstArchiviert, Is.True);
    }

    [Test]
    public void Wenn_der_Parameter_false_traegt_dann_sind_dieselben_Boards_gemeint_wie_ohne_Parameter()
    {
        var mitFalse = Archivfilter.Aus("false", Listenroute);
        var ohneParameter = Archivfilter.Aus(null, Listenroute);

        Assert.That(mitFalse.IstErfolg, Is.True);
        Assert.That(mitFalse.Wert, Is.EqualTo(ohneParameter.Wert));
    }

    [Test]
    public void Wenn_der_Parameter_gross_geschrieben_ist_dann_wird_er_trotzdem_gelesen()
    {
        var ergebnis = Archivfilter.Aus("TRUE", Listenroute);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.IstArchiviert, Is.True);
    }

    [Test]
    public void Wenn_der_Parameter_weder_wahr_noch_falsch_bedeutet_dann_wird_er_mit_Befund_zurueckgewiesen()
    {
        var ergebnis = Archivfilter.Aus("vielleicht", Listenroute);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Befundpruefung.ErwarteVollstaendigenBefund(ergebnis.Befunde[0], "archiv-filter-unlesbar");
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("vielleicht"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain("archiviert=true"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain(Listenroute));
        });
    }

    // Jede Adresse erklaert sich selbst: die Kompensation nennt die Route, die der Aufrufer
    // wirklich gerufen hat, nicht die Boardliste.
    [Test]
    public void Wenn_eine_andere_Route_den_Filter_benutzt_dann_nennt_die_Kompensation_diese_Route()
    {
        const string kartenroute = "GET /api/boards/1/spalten/2/karten";

        var ergebnis = Archivfilter.Aus("vielleicht", kartenroute);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Contain($"{kartenroute}?archiviert=true"));
            Assert.That(ergebnis.Befunde[0].Kompensation, Does.Not.Contain("/api/boards`"));
        });
    }

    [Test]
    public void Wenn_der_Parameter_eine_Zahl_traegt_dann_wird_er_ebenfalls_zurueckgewiesen()
    {
        var ergebnis = Archivfilter.Aus("1", Listenroute);

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.That(ergebnis.Befunde[0].Code, Is.EqualTo("archiv-filter-unlesbar"));
    }
}
