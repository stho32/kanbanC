using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Diese Prüfungen lesen den Quelltextbaum, weil ihr Gegenstand die Ablage selbst ist: dass die
// Gestaltungswerte der neuen Fläche aus dem Token-Sheet kommen — und dass sie **nichts bedient**.
// stil-check: C03 Dateisystem ist hier der Prüfgegenstand, nicht eine Laufzeitabhängigkeit
[TestFixture]
public class RohdatenflaecheTests
{
    [Test]
    public void Wenn_die_Stilvorlage_der_Rohdatenflaeche_gelesen_wird_dann_traegt_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        var regeln = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Auswertungen", "Rohdatenflaeche.razor.css"));

        Assert.That(regeln, Is.Not.Empty, "Ohne Regeln prüfte der Test nichts.");
        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(regeln, @"#[0-9a-fA-F]{3,8}\b"), Is.Empty, "Farben gehören ins Token-Sheet.");
            Assert.That(Regex.Matches(regeln, @"(?:padding|margin|gap|border-radius)[^;]*\d+px"), Is.Empty, "Abstände und Radien kommen aus dem Token-Sheet.");
        });
    }

    // **Der Eintrag bedient nichts.** Kein Knopf, kein Download, kein Abruf aus dem Schirm heraus:
    // ein „Rohdaten"-Knopf wäre ein zweiter Export neben dem Zeitexport und eine Fähigkeit, die bei
    // der API anfängt.
    [Test]
    public void Wenn_die_Rohdatenflaeche_gelesen_wird_dann_traegt_sie_weder_Knopf_noch_Verweis_noch_Abruf()
    {
        var flaeche = OhneKommentare(File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Auswertungen", "Rohdatenflaeche.razor")));

        Assert.Multiple(() =>
        {
            Assert.That(flaeche, Does.Not.Contain("<button"));
            Assert.That(flaeche, Does.Not.Contain("<a "));
            Assert.That(flaeche, Does.Not.Contain("@onclick"));
            Assert.That(flaeche, Does.Not.Contain("HttpClient"));
            Assert.That(flaeche, Does.Not.Contain("ApiKlient"));
            Assert.That(flaeche, Does.Not.Contain("IJSRuntime"));
        });
    }

    // Die Fläche nennt **zwei** Pfade mit der Boardnummer darin — und keinen dritten.
    [Test]
    public void Wenn_die_Rohdatenflaeche_gelesen_wird_dann_nennt_sie_genau_die_zwei_Pfade()
    {
        var flaeche = OhneKommentare(File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Auswertungen", "Rohdatenflaeche.razor")));

        Assert.Multiple(() =>
        {
            Assert.That(flaeche, Does.Contain("/api/boards/{BoardId}/karten"));
            Assert.That(flaeche, Does.Contain("/api/boards/{BoardId}/zeiten"));
            Assert.That(flaeche, Does.Not.Contain("verlaeufe"));
        });
    }

    // Der Umschalter führt jede Auswertung des Dialogs als **gebaute**: die Liste der gesperrten
    // Punkte ist leer und bleibt trotzdem stehen — ein künftiger Eintrag wäre wieder ein Eintrag
    // und kein Umbau. Bliebe ein Punkt gesperrt, löge der Schirm über einen gebauten Slice.
    [Test]
    public void Wenn_der_Schirm_gelesen_wird_dann_steht_kein_Punkt_mehr_gesperrt()
    {
        var schirm = OhneKommentare(File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Auswertungen.razor")));

        var gesperrte = Regex.Match(schirm, @"NochNichtGebaut\s*=\s*\[(?<eintraege>[^\]]*)\]");
        Assert.That(gesperrte.Success, Is.True, "Die Liste der gesperrten Auswertungen wurde nicht gefunden.");
        Assert.That(gesperrte.Groups["eintraege"].Value.Trim(), Is.Empty);
    }

    // Der Schirm ruft für diese Wahl nichts ab — es gibt keinen Rohdaten-Klienten, und tote
    // Flexibilität wäre ein Glied ohne Aufrufer.
    [Test]
    public void Wenn_die_Oberflaeche_gelesen_wird_dann_gibt_es_keinen_Rohdaten_Klienten()
    {
        var dienste = Directory.GetFiles(Quelltextbaum.BlazorDatei("Services"), "*.cs");

        Assert.That(dienste.Select(Path.GetFileName), Has.None.Contains("Rohdaten"));
    }

    private static string OhneKommentare(string markup)
    {
        var ohneRazorkommentare = Regex.Replace(markup, @"@\*[\s\S]*?\*@", string.Empty);
        return Regex.Replace(ohneRazorkommentare, @"(?m)^\s*//.*$", string.Empty);
    }
}
