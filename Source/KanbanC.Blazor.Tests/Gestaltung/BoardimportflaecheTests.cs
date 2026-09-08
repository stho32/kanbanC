using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Diese Prüfungen lesen den Quelltextbaum, weil ihr Gegenstand die Ablage selbst ist: dass die
// Gestaltungswerte der neuen Fläche aus dem Token-Sheet kommen — und dass der Einstieg im
// Seitenkopf steht und nicht im ⋯-Menü einer Kachel.
// stil-check: C03 Dateisystem ist hier der Prüfgegenstand, nicht eine Laufzeitabhängigkeit
[TestFixture]
public class BoardimportflaecheTests
{
    // stil-check: C03 die Ablage ist der Prüfgegenstand, nicht eine Laufzeitabhängigkeit
    [Test]
    public void Wenn_die_Stilvorlage_der_Boardliste_gelesen_wird_dann_traegt_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        var stilvorlage = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Boards.razor.css"));
        var regeln = stilvorlage[stilvorlage.IndexOf(".importflaeche", StringComparison.Ordinal)..];

        Assert.That(regeln, Does.Contain("var(--space-"), "Ohne Gestaltungswerte aus dem Token-Sheet prüfte der Test nichts.");
        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(regeln, @"#[0-9a-fA-F]{3,8}\b"), Is.Empty, "Farben gehören ins Token-Sheet.");
            Assert.That(Regex.Matches(regeln, @"\brgba?\("), Is.Empty, "Farben gehören ins Token-Sheet.");
            Assert.That(Regex.Matches(regeln, @"(?:color|background)\s*:\s*(?!var\(|color-mix\(|transparent|inherit|none)[A-Za-z#]"), Is.Empty, "Farben gehören ins Token-Sheet.");
            Assert.That(Regex.Matches(regeln, @"(?:padding|margin|gap|border-radius)[^;]*\d+(?:px|rem|em)"), Is.Empty, "Abstände und Radien kommen aus dem Token-Sheet.");
        });
    }

    // **Der Knopf steht im Seitenkopf neben „+ Board anlegen"**: das ⋯-Menü handelt an einem
    // vorhandenen Board, der Import erzeugt eines.
    [Test]
    public void Wenn_die_Boardliste_gelesen_wird_dann_steht_der_Importknopf_im_Seitenkopf_neben_dem_Anlegeknopf()
    {
        var seite = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Boards.razor"));
        var seitenkopf = Seitenkopf(seite);

        Assert.Multiple(() =>
        {
            Assert.That(seitenkopf, Does.Contain("board-anlegen-oeffnen"));
            Assert.That(seitenkopf, Does.Contain("board-importieren-oeffnen"));
            Assert.That(seitenkopf, Does.Contain("Board importieren"));
        });
    }

    // Der Ablauf sitzt auf `/boards`: **kein eigener Schirm und keine eigene Route.**
    [Test]
    public void Wenn_die_Boardliste_gelesen_wird_dann_traegt_sie_genau_eine_Seitenadresse()
    {
        var seite = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Boards.razor"));

        var adressen = Regex.Matches(seite, @"@page\s+""[^""]+""");

        Assert.Multiple(() =>
        {
            Assert.That(adressen, Has.Count.EqualTo(1));
            Assert.That(adressen[0].Value, Does.Contain("/boards"));
        });
    }

    // Die Kachel führt weiterhin ihre drei Menüpunkte — der Import ist keiner davon. Der Test
    // prüft **beide Seiten**: die Kachel kennt ihn nicht, und der Seitenkopf kennt ihn.
    [Test]
    public void Wenn_das_Kachelmenue_gelesen_wird_dann_steht_der_Import_nicht_darin_sondern_im_Seitenkopf()
    {
        var kachel = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Boards", "Boardkachel.razor"));
        var seitenkopf = Seitenkopf(File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Boards.razor")));

        Assert.Multiple(() =>
        {
            Assert.That(kachel, Does.Not.Contain("importieren").IgnoreCase);
            Assert.That(kachel, Does.Contain("exportieren").IgnoreCase, "Ohne die drei vorhandenen Menüpunkte prüfte der Test nichts.");
            Assert.That(seitenkopf, Does.Contain("Board importieren"));
        });
    }

    // **Die Kernregel des Projekts**: die Oberfläche spricht ausschließlich über HTTP mit der API.
    // Eine Projektreferenz auf `KanbanC.BL` machte eine Fähigkeit baubar, die kein Endpunkt hat —
    // und höhlte die Zusage „was die Oberfläche kann, kann die API" still aus.
    [Test]
    public void Wenn_die_Projektdatei_der_Oberflaeche_gelesen_wird_dann_traegt_sie_keine_Referenz_auf_die_Fachlogik()
    {
        var projektdatei = File.ReadAllText(Quelltextbaum.BlazorDatei("KanbanC.Blazor.csproj"));

        Assert.Multiple(() =>
        {
            Assert.That(projektdatei, Does.Contain("KanbanC.Contracts"), "Ohne die Contracts-Referenz prüfte der Test nichts.");
            Assert.That(projektdatei, Does.Not.Contain("KanbanC.BL"));
        });
    }

    // Die Ablegefläche ist **während des Laufs gesperrt** (`R00024`) — eine zweite Ablage während
    // des ersten Laufs verlöre den ersten still. Und ein Ausfall der WebApi endet in einer
    // lesbaren Meldung statt auf einer Ausnahmeseite.
    [Test]
    public void Wenn_die_Boardliste_gelesen_wird_dann_sperrt_sie_die_Ablegeflaeche_und_faengt_den_Ausfall_der_WebApi_ab()
    {
        var seite = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Boards.razor"));

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("Boardablegeflaeche.IstGesperrt"));
            Assert.That(seite, Does.Contain("disabled=\"@AblegeflaecheIstGesperrt\""));
            Assert.That(seite, Does.Contain("WebApiAufruf.MitAusfallmeldung"));
        });
    }

    private static string Seitenkopf(string seite)
    {
        var anfang = seite.IndexOf("<div class=\"seitenkopf\">", StringComparison.Ordinal);
        var ende = seite.IndexOf("</div>", anfang, StringComparison.Ordinal);
        return seite[anfang..ende];
    }
}
