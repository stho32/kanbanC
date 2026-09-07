using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Die Gestaltungszusagen des Verbindungsstands und des Aufschließbands, geprüft an der Datei — wie
// die übrigen Gestaltungsprüfungen. Was sie im Browser tun, belegt die E2E-Strecke; was sie
// **nicht** tun, lässt sich dort nicht zeigen: dass kein Gestaltungswert als Literal in ihren
// Regeln steht, dass die Zurücknahme über Sättigung und nicht über Deckkraft läuft, dass die
// Kopfzeile keine dauerhafte „live"-Marke trägt und dass sich beide zuhörenden Sichten wieder
// abmelden.
// stil-check: C03 die Ablage ist hier der Prüfgegenstand
public class VerbindungsstandGestaltungTests
{
    private static string Kopfzeile()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Layout", "Kopfzeile.razor"));
    }

    private static string Kopfzeilenstilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Layout", "Kopfzeile.razor.css"));
    }

    private static string Layout()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Layout", "MainLayout.razor"));
    }

    private static string Layoutstilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Layout", "MainLayout.razor.css"));
    }

    private static string Boardseite()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Board.razor"));
    }

    private static string Boardstilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Board.razor.css"));
    }

    private static string Kartenseite()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Kartendetail.razor"));
    }

    private static string Tokensheet()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("wwwroot", "gestaltung.css"));
    }

    // Die Marke sitzt links vom Laufzeitplatz und steht **nur**, wenn die Leitung weg ist.
    [Test]
    public void Wenn_die_Kopfzeile_gelesen_wird_dann_steht_die_Verbindungsmarke_links_vom_Laufzeitplatz()
    {
        var kopfzeile = Kopfzeile();

        var stelleDerMarke = kopfzeile.IndexOf("id=\"verbindungsmarke\"", StringComparison.Ordinal);
        var stelleDesLaufzeitplatzes = kopfzeile.IndexOf("class=\"laufzeitplatz\"", StringComparison.Ordinal);
        Assert.Multiple(() =>
        {
            Assert.That(stelleDerMarke, Is.GreaterThan(-1));
            Assert.That(stelleDerMarke, Is.LessThan(stelleDesLaufzeitplatzes));
            Assert.That(kopfzeile, Does.Contain("@if (_verbindungsmarke is not null)"), "Die Marke steht dauerhaft statt nur bei Verlust.");
            Assert.That(kopfzeile, Does.Contain("role=\"status\""), "Ein Verbindungsverlust ist eine Statusmeldung, kein Alarm.");
        });
    }

    // Es gibt keine dauerhafte Marke „live": der knappste Platz der Anwendung gehört dem, was
    // gerade gilt, und Abwesenheit heißt „es steht".
    [Test]
    public void Wenn_die_Kopfzeile_gelesen_wird_dann_traegt_sie_keine_dauerhafte_Live_Marke()
    {
        var kopfzeile = Kopfzeile();

        Assert.Multiple(() =>
        {
            Assert.That(kopfzeile, Does.Not.Contain(">live<"));
            Assert.That(kopfzeile, Does.Not.Contain("● live"));
        });
    }

    [Test]
    public void Wenn_die_Regeln_der_Verbindungsmarke_gelesen_werden_dann_tragen_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        ErwarteOhneLiterale(Regeln(Kopfzeilenstilvorlage(), @"\.kopfzeile-verbindung"));
    }

    // Die Zurücknahme gilt dem **ganzen Schirm**: eine Klasse am Layout, nicht eine je Komponente.
    [Test]
    public void Wenn_das_Layout_gelesen_wird_dann_traegt_der_Rumpf_die_Klasse_des_getrennten_Schirms()
    {
        var layout = Layout();

        Assert.Multiple(() =>
        {
            Assert.That(layout, Does.Contain("seite schirm-getrennt"));
            Assert.That(layout, Does.Contain("Ereignisverteiler.Verbindungsstandgewechselt += AufVerbindungsstand"));
            Assert.That(layout, Does.Contain("@implements IDisposable"));
            Assert.That(layout, Does.Contain("Ereignisverteiler.Verbindungsstandgewechselt -= AufVerbindungsstand"));
        });
    }

    // **Ruhiger, nicht unlesbar:** die Zurücknahme läuft über Sättigung, Kontrast und Helligkeit
    // und nicht über die Deckkraft — Deckkraft nähme auch dem Text die Lesbarkeit, die der Entwurf
    // ausdrücklich erhalten will.
    [Test]
    public void Wenn_die_Zuruecknahme_gelesen_wird_dann_laeuft_sie_ueber_Saettigung_und_nicht_ueber_Deckkraft()
    {
        var regel = Regeln(Layoutstilvorlage(), @"\.schirm-getrennt");
        var token = Tokensheet();

        Assert.Multiple(() =>
        {
            Assert.That(regel, Does.Contain("filter: var(--filter-zurueckgenommen)"));
            Assert.That(regel, Does.Not.Contain("opacity"), "Deckkraft nimmt auch dem Text die Lesbarkeit.");
            Assert.That(token, Does.Contain("--filter-zurueckgenommen:"), "Der Wert gehört ins Token-Sheet.");
            Assert.That(Wert(token, "--filter-zurueckgenommen"), Does.Contain("saturate("));
            Assert.That(Wert(token, "--filter-zurueckgenommen"), Does.Not.Contain("opacity("));
        });
    }

    // Das Band steht **über den Bahnen** und nicht in der Kopfzeile: die Kopfzeile steht auf jeder
    // Seite, das Band gehört dem Board.
    [Test]
    public void Wenn_die_Boardseite_gelesen_wird_dann_steht_das_Band_ueber_den_Bahnen()
    {
        var seite = Boardseite();

        var stelleDesBandes = seite.IndexOf("id=\"aufschliessband\"", StringComparison.Ordinal);
        var stelleDerBahnen = seite.IndexOf("<Spaltenbahnen", StringComparison.Ordinal);
        Assert.Multiple(() =>
        {
            Assert.That(stelleDesBandes, Is.GreaterThan(-1));
            Assert.That(stelleDesBandes, Is.LessThan(stelleDerBahnen));
            Assert.That(seite, Does.Contain("id=\"aufschliessband-schliessen\""));
            Assert.That(seite, Does.Contain(">schließen<"));
        });
    }

    [Test]
    public void Wenn_die_Kopfzeile_gelesen_wird_dann_traegt_sie_kein_Aufschliessband()
    {
        Assert.That(Kopfzeile(), Does.Not.Contain("aufschliessband"));
    }

    [Test]
    public void Wenn_die_Regeln_des_Bandes_gelesen_werden_dann_tragen_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        ErwarteOhneLiterale(Regeln(Boardstilvorlage(), @"\.aufschliessband"));
    }

    // Die Kartenseite zeigt eine Karte, nicht einen Bestand: dort gibt es weder Band noch Zahl.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_sie_weder_Band_noch_Zahl()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Not.Contain("aufschliessband"));
            Assert.That(seite, Does.Not.Contain("Aufschliessband"));
            Assert.That(seite, Does.Not.Contain("Standvergleich"));
        });
    }

    // Ein Singleton, an dem sich Kreisläufe anmelden, hält tote Kreisläufe fest, wenn niemand
    // abmeldet — auch für das zweite Ereignis.
    [Test]
    public void Wenn_die_zuhoerenden_Sichten_gelesen_werden_dann_melden_sie_sich_auch_vom_Verbindungsstand_ab()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Boardseite(), Does.Contain("Ereignisverteiler.Verbindungsstandgewechselt -= NimmVerbindungsstandAn"));
            Assert.That(Kartenseite(), Does.Contain("Ereignisverteiler.Verbindungsstandgewechselt -= NimmVerbindungsstandAn"));
            Assert.That(Kopfzeile(), Does.Contain("Ereignisverteiler.Verbindungsstandgewechselt -= AufVerbindungsstand"));
        });
    }

    // Die Schwelle steht an derselben Sorte Stelle wie die Markenstandzeit — ein Testlauf muss sie
    // senken können, statt gegen ein Literal im Renderzweig anzulaufen.
    [Test]
    public void Wenn_die_Startdatei_gelesen_wird_dann_kommt_die_Schwelle_aus_der_Konfiguration()
    {
        var start = File.ReadAllText(Quelltextbaum.BlazorDatei("Program.cs"));
        var einstellungen = File.ReadAllText(Quelltextbaum.BlazorDatei("appsettings.json"));

        Assert.Multiple(() =>
        {
            Assert.That(start, Does.Contain("Aufschliessschwelle.Aus(builder.Configuration[\"Oberflaeche:AufschliessschwelleInAenderungen\"])"));
            Assert.That(einstellungen, Does.Contain("\"AufschliessschwelleInAenderungen\": \"\""), "Der Schlüssel bleibt leer, damit der Betrieb die Vorgabe nimmt.");
        });
    }

    private static string Wert(string stil, string variable)
    {
        var treffer = Regex.Match(stil, Regex.Escape(variable) + @":([^;]*);");
        Assert.That(treffer.Success, Is.True, $"Die Variable {variable} fehlt.");
        return treffer.Groups[1].Value;
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

    private static string Regeln(string stil, string selektormuster)
    {
        var treffer = Regex.Matches(stil, selektormuster + @"[a-z:-]*[^{}]*\{[^}]*\}");
        return string.Join(Environment.NewLine, treffer.Select(eintrag => eintrag.Value));
    }
}
