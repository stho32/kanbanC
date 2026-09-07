using KanbanC.BL.Integrations.Import;
using KanbanC.BL.Tests.TestHelpers;

namespace KanbanC.BL.Tests.Integrations.Import;

public class WbsleserTests
{
    [Test]
    public void Wenn_eine_Datei_ohne_application_gelesen_wird_dann_nennt_die_Zurueckweisung_Datei_Angabe_und_Weg()
    {
        var text = string.Join('\n', "---", "titel: Protokoll", "---", string.Empty, "| ID | Ebene | Eltern | Name | Status | a | b | c | d | e | f | g |", "|---|---|---|---|---|---|---|---|---|---|---|---|", "| A0001 | Application | — | Probe | gelb | | | | | | | |");

        var ergebnis = Wbsleser.Lies(text, "Protokoll.md");

        Assert.That(ergebnis.IstErfolg, Is.False);
        var befund = ergebnis.Befunde[0];
        Assert.Multiple(() =>
        {
            Assert.That(befund.Code, Is.EqualTo("wbs-datei-unlesbar"));
            Assert.That(befund.Meldung, Does.Contain("Protokoll.md"));
            Assert.That(befund.Meldung, Does.Contain("application:"));
            Assert.That(befund.Kompensation, Does.Contain("/planung anlegen"));
        });
    }

    [Test]
    public void Wenn_eine_Datei_ohne_Knotentabelle_gelesen_wird_dann_nennt_die_Zurueckweisung_die_fehlenden_Spalten()
    {
        var text = string.Join('\n', "---", "application: Probe", "---", string.Empty, "# Nur Text");

        var ergebnis = Wbsleser.Lies(text, "notizen.md");

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("notizen.md"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("ID, Ebene, Eltern, Name, Status"));
        });
    }

    [Test]
    public void Wenn_weder_Kopf_noch_Tabelle_dastehen_dann_nennt_die_Meldung_beides()
    {
        var ergebnis = Wbsleser.Lies("Nur Prosa.", "brief.md");

        Assert.That(ergebnis.IstErfolg, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Frontmatter-Block"));
            Assert.That(ergebnis.Befunde[0].Meldung, Does.Contain("Knotentabelle"));
        });
    }

    // Die Knotentabelle wird an ihrer Kopfzeile erkannt und nicht an ihrer Stelle: eine WBS-Datei
    // trägt auch andere Tabellen, und die Stelle der einen ist keine Zusage.
    [Test]
    public void Wenn_vor_der_Knotentabelle_eine_andere_Tabelle_steht_dann_wird_die_richtige_gelesen()
    {
        var text = string.Join(
            '\n',
            "---",
            "application: Probe",
            "---",
            "| Feature | Bubbles |",
            "|---|---|",
            "| F0051 | 7 |",
            string.Empty,
            "| ID | Ebene | Eltern | Name | Status | Fertig-Kriterium | Eingabe → Ausgabe | Aufwand | Ausbaustufe | Braucht | Requirement | Notiz |",
            "|---|---|---|---|---|---|---|---|---|---|---|---|",
            "| A0001 | Application | — | Probe | gelb | | | | | | | |");

        var ergebnis = Wbsleser.Lies(text, "probe.md");

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Baum.KnotenAnzahl, Is.EqualTo(1));
    }

    [Test]
    public void Wenn_die_echte_Datei_als_Strom_gelesen_wird_dann_entsteht_derselbe_Baum_wie_aus_dem_Text()
    {
        using var strom = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(EingefroreneWbsdatei.Text()));

        var ergebnis = Wbsleser.Lies(strom, EingefroreneWbsdatei.Dateiname);

        Assert.That(ergebnis.IstErfolg, Is.True);
        Assert.That(ergebnis.Wert.Baum.KnotenAnzahl, Is.EqualTo(EingefroreneWbsdatei.Knotenzeilen));
    }
}
