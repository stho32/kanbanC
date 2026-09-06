using KanbanC.BL.Operations.Fehler;
using KanbanC.BL.Tests.TestHelpers;

namespace KanbanC.BL.Tests.Operations.Fehler;

public class DoppeltTests
{
    [Test]
    public void Wenn_der_Pfad_schon_steht_dann_nennt_der_Befund_den_Pfad_und_die_Kartennummer()
    {
        var befund = Doppelt.Dateiverweis(14, "Dokumentation/Planung/kanbanc.md");

        Befundpruefung.ErwarteVollstaendigenBefund(befund, "dateiverweis-doppelt");
        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Does.Contain("Dokumentation/Planung/kanbanc.md"));
            Assert.That(befund.Meldung, Does.Contain("14"));
            Assert.That(befund.Kompensation, Does.Contain("GET /api/karten/14"));
        });
    }

    // Der Aufrufer trifft nie auf eine nackte Datenbankmeldung ueber einen verletzten Index:
    // weder „UNIQUE" noch der Indexname stehen im Befund.
    [Test]
    public void Wenn_der_Pfad_schon_steht_dann_traegt_der_Befund_keine_Datenbankmeldung()
    {
        var befund = Doppelt.Dateiverweis(14, "kanbanc.md");

        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Does.Not.Contain("UNIQUE"));
            Assert.That(befund.Meldung, Does.Not.Contain("UX_Dateiverweis"));
            Assert.That(befund.Meldung, Does.Not.Contain("constraint"));
        });
    }

    // 400 und nicht 404: es fehlt kein Ding, es wurde eine Regel verletzt. Dieselbe Logik wie
    // bei kontributor-stillgelegt — und die Statusabbildung liest genau das.
    [Test]
    public void Wenn_der_Befund_geprueft_wird_dann_meldet_er_kein_fehlendes_Ding()
    {
        Assert.That(Nichtgefunden.MeldetEinFehlendesDing(Doppelt.Dateiverweis(14, "kanbanc.md")), Is.False);
    }

    // Ein eigener Code neben dem des fehlenden Dateiverweises: die beiden Lagen sind verschieden
    // und tragen verschiedene Kompensationen.
    [Test]
    public void Wenn_der_Befund_neben_den_fehlenden_Dateiverweis_gestellt_wird_dann_tragen_beide_verschiedene_Codes()
    {
        Assert.That(Doppelt.Dateiverweis(14, "kanbanc.md").Code, Is.Not.EqualTo(Nichtgefunden.Dateiverweis(14, 7).Code));
    }
}
