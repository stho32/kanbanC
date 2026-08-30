using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Diese Prüfungen lesen den Quelltextbaum, weil ihr Gegenstand die Ablage selbst ist:
// welche Datei existiert, welches Stylesheet geladen wird, welche Klasse noch im Markup steht.
// stil-check: C03 Dateisystem ist hier der Prüfgegenstand, nicht eine Laufzeitabhängigkeit
[TestFixture]
public class GestaltungsfundamentTests
{
    private static readonly string[] VerboteneKlassen =
    [
        "btn-primary", "btn-outline-secondary", "form-control", "form-select",
        "form-label", "alert", "row", "d-flex", "navbar", "text-danger",
    ];

    private static readonly string[] VerbotenePraefixe = ["col-md-", "mb-", "px-"];

    private static readonly string[] ErwarteteSchriftdateien =
    [
        "caprasimo-latin.woff2", "caprasimo-latin-ext.woff2",
        "figtree-latin.woff2", "figtree-latin-ext.woff2",
    ];

    [Test]
    public void Wenn_das_Token_Sheet_gesucht_wird_dann_liegt_es_als_wwwroot_gestaltung_css_in_der_Anwendung()
    {
        var pfad = Quelltextbaum.BlazorDatei("wwwroot", "gestaltung.css");

        var istVorhanden = File.Exists(pfad); // stil-check: C03 die Ablage ist der Prüfgegenstand

        Assert.That(istVorhanden, Is.True, $"{pfad} fehlt.");
    }

    [Test]
    public void Wenn_App_razor_gelesen_wird_dann_laedt_es_gestaltung_css_und_kein_Bootstrap()
    {
        var app = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "App.razor"));

        Assert.That(app, Does.Contain("gestaltung.css"));
        Assert.That(app, Does.Not.Contain("bootstrap"));
    }

    [Test]
    public void Wenn_das_Verzeichnis_der_Bootstrap_Bibliothek_gesucht_wird_dann_gibt_es_es_nicht_mehr()
    {
        var pfad = Quelltextbaum.BlazorDatei("wwwroot", "lib", "bootstrap");

        var istVorhanden = Directory.Exists(pfad); // stil-check: C03 die Ablage ist der Prüfgegenstand

        Assert.That(istVorhanden, Is.False, $"{pfad} existiert noch.");
    }

    [Test]
    public void Wenn_das_Token_Sheet_gelesen_wird_dann_traegt_es_alle_Variablen_des_Wireframe_Sheets()
    {
        var wireframeSheet = File.ReadAllText(Path.Combine(Quelltextbaum.Wurzel(), "Dokumentation", "Wireframes", "styles.css"));
        var tokenSheet = File.ReadAllText(Quelltextbaum.BlazorDatei("wwwroot", "gestaltung.css"));
        var erwarteteVariablen = Variablennamen(wireframeSheet);
        var vorhandeneVariablen = Variablennamen(tokenSheet);

        var fehlendeVariablen = erwarteteVariablen.Except(vorhandeneVariablen, StringComparer.Ordinal).ToList();

        Assert.That(erwarteteVariablen, Has.Count.GreaterThan(40), "Das Wireframe-Sheet wurde nicht gelesen.");
        Assert.That(fehlendeVariablen, Is.Empty);
    }

    [Test]
    public void Wenn_das_Token_Sheet_gelesen_wird_dann_traegt_es_dieselben_Klassen_wie_das_Wireframe_Sheet()
    {
        var wireframeSheet = File.ReadAllText(Path.Combine(Quelltextbaum.Wurzel(), "Dokumentation", "Wireframes", "styles.css"));
        var tokenSheet = File.ReadAllText(Quelltextbaum.BlazorDatei("wwwroot", "gestaltung.css"));
        var erwarteteKlassen = Stylesheetklassen(wireframeSheet);
        var vorhandeneKlassen = Stylesheetklassen(tokenSheet);

        var fehlendeKlassen = erwarteteKlassen.Except(vorhandeneKlassen, StringComparer.Ordinal).ToList();

        Assert.That(erwarteteKlassen, Has.Count.GreaterThan(20), "Das Wireframe-Sheet wurde nicht gelesen.");
        Assert.That(fehlendeKlassen, Is.Empty, "Wer die Skizze liest und diese Klasse schreibt, bekommt eine ungestaltete Stelle.");
    }

    [Test]
    public void Wenn_das_Token_Sheet_gelesen_wird_dann_bindet_es_die_Schriften_ueber_font_face_statt_ueber_Google_ein()
    {
        var tokenSheet = File.ReadAllText(Quelltextbaum.BlazorDatei("wwwroot", "gestaltung.css"));

        Assert.That(tokenSheet, Does.Not.Contain("fonts.googleapis.com"));
        Assert.That(tokenSheet, Does.Not.Contain("@import"));
        Assert.That(tokenSheet, Does.Contain("@font-face"));
        Assert.That(tokenSheet, Does.Contain("/fonts/caprasimo-latin.woff2"));
        Assert.That(tokenSheet, Does.Contain("/fonts/figtree-latin.woff2"));
    }

    [Test]
    public void Wenn_die_Schriftdateien_gesucht_werden_dann_liegen_sie_unter_wwwroot_fonts()
    {
        var fehlendeDateien = ErwarteteSchriftdateien.Where(FehltImSchriftverzeichnis).ToList();

        Assert.That(fehlendeDateien, Is.Empty);
    }

    [Test]
    public void Wenn_alle_Stylesheets_neben_dem_Token_Sheet_gelesen_werden_dann_traegt_keines_einen_Farbwert()
    {
        var stylesheets = StylesheetsDerAnwendung();

        var befunde = stylesheets.SelectMany(FarbwerteIn).ToList();

        Assert.That(stylesheets, Is.Not.Empty);
        Assert.That(befunde, Is.Empty);
    }

    [Test]
    public void Wenn_alle_Razor_Dateien_gelesen_werden_dann_traegt_keine_mehr_eine_Bootstrap_Klasse()
    {
        var razorDateien = Directory.GetFiles(Quelltextbaum.BlazorProjekt(), "*.razor", SearchOption.AllDirectories);

        var befunde = razorDateien.SelectMany(BootstrapklassenIn).ToList();

        Assert.That(razorDateien, Is.Not.Empty);
        Assert.That(befunde, Is.Empty);
    }

    private static bool FehltImSchriftverzeichnis(string dateiname)
    {
        // stil-check: C03 die Ablage ist der Prüfgegenstand
        return !File.Exists(Quelltextbaum.BlazorDatei("wwwroot", "fonts", dateiname));
    }

    // Das Token-Sheet ist der eine erlaubte Ort für Farben; die Bauartefakte unter obj/ sind
    // Kopien und kein Quelltext.
    private static IReadOnlyList<string> StylesheetsDerAnwendung()
    {
        var alleStylesheets = Directory.GetFiles(Quelltextbaum.BlazorProjekt(), "*.css", SearchOption.AllDirectories);
        var quelltextStylesheets = alleStylesheets.Where(IstQuelltext);
        return quelltextStylesheets.Where(pfad => Path.GetFileName(pfad) != "gestaltung.css").ToList();
    }

    private static bool IstQuelltext(string pfad)
    {
        var bauverzeichnis = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        return !pfad.Contains(bauverzeichnis, StringComparison.Ordinal);
    }

    // Farben gehören ins Token-Sheet, nicht in eine Komponenten-CSS-Datei: sonst gibt es
    // wieder mehr als einen Ort, an dem die Gestaltung steht.
    private static IReadOnlyList<string> FarbwerteIn(string dateipfad)
    {
        var inhalt = File.ReadAllText(dateipfad);
        var name = Path.GetFileName(dateipfad);
        var treffer = Regex.Matches(inhalt, "#[0-9a-fA-F]{3,8}\\b|\\brgba?\\(|\\bhsla?\\(|:\\s*(white|black|lightyellow|red|blue|green)\\b");
        return treffer.Select(einTreffer => $"{name}: {einTreffer.Value}").ToList();
    }

    private static IReadOnlyList<string> BootstrapklassenIn(string dateipfad)
    {
        var inhalt = File.ReadAllText(dateipfad);
        var name = Path.GetFileName(dateipfad);
        var bootstrapklassen = Klassennamen(inhalt).Where(IstBootstrapklasse);
        return bootstrapklassen.Select(klasse => $"{name}: {klasse}").ToList();
    }

    private static bool IstBootstrapklasse(string klasse)
    {
        var stehtAufDerListe = VerboteneKlassen.Contains(klasse, StringComparer.Ordinal);
        if (stehtAufDerListe)
        {
            return true;
        }

        return VerbotenePraefixe.Any(praefix => klasse.StartsWith(praefix, StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> Klassennamen(string markup)
    {
        var treffer = Regex.Matches(markup, "class=\"([^\"]*)\"");
        var alleKlassen = treffer.SelectMany(einTreffer => Klassenliste(einTreffer.Groups[1].Value));
        return alleKlassen.Distinct(StringComparer.Ordinal).ToList();
    }

    // Razor-Ausdrücke im class-Attribut sind keine Klassennamen.
    private static IEnumerable<string> Klassenliste(string klassenattribut)
    {
        var klassen = klassenattribut.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return klassen.Where(klasse => !klasse.StartsWith('@'));
    }

    private static IReadOnlyList<string> Variablennamen(string stylesheet)
    {
        var treffer = Regex.Matches(stylesheet, "(--[a-z0-9-]+)\\s*:");
        var namen = treffer.Select(einTreffer => einTreffer.Groups[1].Value);
        return namen.Distinct(StringComparer.Ordinal).ToList();
    }

    // Liest die Klassen, die ein Stylesheet definiert — Gegenstueck zu Klassennamen, das
    // die Klassen liest, die ein Markup verwendet.
    private static IReadOnlyList<string> Stylesheetklassen(string stylesheet)
    {
        var treffer = Regex.Matches(stylesheet, "\\.([a-z][a-z0-9-]*)\\s*[,{:]");
        var namen = treffer.Select(einTreffer => einTreffer.Groups[1].Value);
        return namen.Distinct(StringComparer.Ordinal).ToList();
    }
}
