using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Diese Prüfungen lesen den Quelltextbaum, weil ihr Gegenstand die Ablage selbst ist: dass die
// Gestaltungswerte des neuen Schirms aus dem Token-Sheet kommen — und dass die Sollzeit nirgends
// als Feld auftaucht, in das jemand schreiben könnte.
// stil-check: C03 Dateisystem ist hier der Prüfgegenstand, nicht eine Laufzeitabhängigkeit
[TestFixture]
public class AuswertungsflaecheTests
{
    [Test]
    public void Wenn_die_Stilvorlage_der_Auswertungsflaeche_gelesen_wird_dann_traegt_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        ErwarteOhneLiterale(Auswertungsstilvorlage());
    }

    [Test]
    public void Wenn_die_Stilvorlage_der_Tabelle_gelesen_wird_dann_traegt_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        ErwarteOhneLiterale(Tabellenstilvorlage());
    }

    // Kein CSS-Framework: was mit R00005 herausgegangen ist, kommt mit einem neuen Schirm nicht
    // zurück.
    [Test]
    public void Wenn_der_Schirm_gelesen_wird_dann_traegt_er_keine_Bootstrap_Klasse()
    {
        var schirm = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Auswertungen.razor")); // stil-check: C03 die Ablage ist der Prüfgegenstand

        Assert.Multiple(() =>
        {
            Assert.That(schirm, Does.Not.Contain("form-control"));
            Assert.That(schirm, Does.Not.Contain("btn-primary"));
            Assert.That(schirm, Does.Not.Contain("row"));
        });
    }

    // **Die Sollzeit ist nicht von Hand änderbar.** Die Vision schließt Planen im Board aus; der
    // einzige Erzeuger ist der Import. Ein Feld an der Karte oder am Kartendetail wäre die eine
    // Stelle, an der sich das still ändern ließe — deshalb steht die Zusage hier als Prüfung.
    // Geprüft wird das Markup **ohne seine Kommentare**: dass es keine Sollzeit gibt, steht seit
    // I0026 als Begründung darin und ist kein Feld.
    [Test]
    public void Wenn_die_Karte_gelesen_wird_dann_zeigt_sie_kein_Sollzeitfeld()
    {
        var karte = OhneKommentare(File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Karten", "Karte.razor")));

        Assert.That(karte, Does.Not.Contain("Sollzeit").IgnoreCase);
        Assert.That(karte, Does.Not.Contain("Sollband").IgnoreCase);
    }

    [Test]
    public void Wenn_das_Kartendetail_gelesen_wird_dann_zeigt_es_kein_Sollzeitfeld()
    {
        var kartendetail = OhneKommentare(File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Kartendetail.razor")));

        Assert.That(kartendetail, Does.Not.Contain("Sollzeit").IgnoreCase);
        Assert.That(kartendetail, Does.Not.Contain("Sollband").IgnoreCase);
    }

    // Der Vertrag, den beide Schirme binden, trägt die Sollzeit ebenfalls nicht: sie wohnt in der
    // Auswertung und nicht an der Karte.
    [Test]
    public void Wenn_die_Kartenvertraege_gelesen_werden_dann_tragen_sie_kein_Sollzeitfeld()
    {
        var felder = typeof(Karte).GetProperties().Concat(typeof(Kartendetail).GetProperties());

        Assert.That(felder.Select(feld => feld.Name), Has.None.Contains("Soll").IgnoreCase);
    }

    private static string Auswertungsstilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Auswertungen.razor.css"));
    }

    private static string Tabellenstilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Auswertungen", "SollIstTabelle.razor.css"));
    }

    // Razor-Kommentare und C#-Zeilenkommentare heraus: der Prüfgegenstand ist, was der Schirm
    // zeigt, nicht was er über sich schreibt.
    private static string OhneKommentare(string markup)
    {
        var ohneRazorkommentare = Regex.Replace(markup, @"@\*[\s\S]*?\*@", string.Empty);
        return Regex.Replace(ohneRazorkommentare, @"(?m)^\s*//.*$", string.Empty);
    }

    private static void ErwarteOhneLiterale(string regeln)
    {
        Assert.That(regeln, Is.Not.Empty, "Ohne Regeln prüfte der Test nichts.");
        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(regeln, @"#[0-9a-fA-F]{3,8}\b"), Is.Empty, "Farben gehören ins Token-Sheet.");
            Assert.That(Regex.Matches(regeln, @"(?:padding|margin|gap|border-radius)[^;]*\d+px"), Is.Empty, "Abstände und Radien kommen aus dem Token-Sheet.");
        });
    }
}
