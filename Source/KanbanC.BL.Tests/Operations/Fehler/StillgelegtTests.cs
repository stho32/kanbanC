using KanbanC.BL.Operations.Fehler;
using KanbanC.BL.Tests.TestHelpers;

namespace KanbanC.BL.Tests.Operations.Fehler;

// Vier Schwestern mit **demselben** Code und **verschiedenen** Meldungen. Der gemeinsame Code
// haelt Nichtgefunden.AlleCodes und die Statusabbildung unberuehrt; die eigene Meldung
// verhindert eine Falschaussage ueber die Lage, in der jemand gerade steht.
public class StillgelegtTests
{
    [Test]
    public void Wenn_ein_stillgelegter_Kontributor_einen_Dateiverweis_eintragen_will_dann_sagt_die_Meldung_genau_das()
    {
        var befund = Stillgelegt.Dateiverweisurheber(999);

        Befundpruefung.ErwarteVollstaendigenBefund(befund, "kontributor-stillgelegt");
        Assert.Multiple(() =>
        {
            Assert.That(befund.Meldung, Does.Contain("999"));
            Assert.That(befund.Meldung, Does.Contain("Dateiverweis"));
        });
    }

    // Die Zusicherung, die die vier Schwestern ueberhaupt rechtfertigt: gleicher Code, aber
    // **nachweislich** verschiedene Meldungen. Waeren sie gleich, waere die vierte Schwester
    // ueberfluessig — und stuende als Falschaussage an der falschen Route.
    [Test]
    public void Wenn_die_vier_Schwestern_verglichen_werden_dann_tragen_sie_denselben_Code_und_verschiedene_Meldungen()
    {
        var verantwortlicher = Stillgelegt.Kontributor(999);
        var kommentarurheber = Stillgelegt.Kommentarurheber(999);
        var anhangurheber = Stillgelegt.Anhangurheber(999);
        var dateiverweisurheber = Stillgelegt.Dateiverweisurheber(999);

        Assert.Multiple(() =>
        {
            Assert.That(kommentarurheber.Code, Is.EqualTo(verantwortlicher.Code));
            Assert.That(anhangurheber.Code, Is.EqualTo(verantwortlicher.Code));
            Assert.That(dateiverweisurheber.Code, Is.EqualTo(verantwortlicher.Code));
            Assert.That(
                new[] { verantwortlicher.Meldung, kommentarurheber.Meldung, anhangurheber.Meldung, dateiverweisurheber.Meldung }.Distinct().Count(),
                Is.EqualTo(4),
                "Gleiche Meldungen machten die eigene Schwester ueberfluessig.");
        });
    }

    // 400 und nicht 404: der Kontributor fehlt nicht, er arbeitet nur nicht mehr mit.
    [Test]
    public void Wenn_der_Befund_geprueft_wird_dann_meldet_er_kein_fehlendes_Ding()
    {
        Assert.That(Nichtgefunden.MeldetEinFehlendesDing(Stillgelegt.Dateiverweisurheber(999)), Is.False);
    }
}
