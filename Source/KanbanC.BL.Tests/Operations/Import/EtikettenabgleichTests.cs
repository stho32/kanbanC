using KanbanC.BL.Operations.Import;

namespace KanbanC.BL.Tests.Operations.Import;

// Der Import zieht nur nach, was aus der Datei stammen kann. Was ein Mensch gesetzt hat, bleibt.
public class EtikettenabgleichTests
{
    private static readonly IReadOnlySet<string> Dateietiketten = new HashSet<string>(["Boards führen", "WBS-Import"], StringComparer.Ordinal);

    [Test]
    public void Wenn_die_Datei_ein_neues_Etikett_fuehrt_dann_ist_es_anzulegen()
    {
        var ergebnis = Etikettenabgleich.Gleiche(["Boards führen", "WBS-Import"], ["Boards führen"], Dateietiketten);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Anzulegen, Is.EqualTo(new[] { "WBS-Import" }));
            Assert.That(ergebnis.Unveraendert, Is.EqualTo(new[] { "Boards führen" }));
            Assert.That(ergebnis.ZuEntfernen, Is.Empty);
        });
    }

    [Test]
    public void Wenn_die_Datei_ein_Etikett_nicht_mehr_fuehrt_dann_ist_es_zu_entfernen()
    {
        var ergebnis = Etikettenabgleich.Gleiche(["Boards führen"], ["Boards führen", "WBS-Import"], Dateietiketten);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.ZuEntfernen, Is.EqualTo(new[] { "WBS-Import" }));
            Assert.That(ergebnis.Fremd, Is.Empty);
        });
    }

    // **Die Klammer um die ganze Nachzieherei**: ein Etikett, das die Datei gar nicht erzeugen
    // kann, hat ein Mensch gesetzt und bleibt stehen.
    [Test]
    public void Wenn_ein_Etikett_nicht_aus_der_Datei_stammen_kann_dann_ist_es_fremd_und_bleibt()
    {
        var ergebnis = Etikettenabgleich.Gleiche(["Boards führen"], ["Boards führen", "dringend"], Dateietiketten);

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Fremd, Is.EqualTo(new[] { "dringend" }));
            Assert.That(ergebnis.ZuEntfernen, Is.Empty);
            Assert.That(ergebnis.Anzulegen, Is.Empty);
        });
    }
}
