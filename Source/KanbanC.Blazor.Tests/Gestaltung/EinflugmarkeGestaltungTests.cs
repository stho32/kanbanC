using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Die Gestaltungszusagen der Einflugmarke und der zurückgehaltenen Meldung, geprüft an der Datei —
// wie die übrigen Gestaltungsprüfungen. Was sie im Browser tun, belegt die E2E-Strecke; was sie
// **nicht** tun, lässt sich dort nicht zeigen: dass kein Gestaltungswert als Literal in ihren
// Regeln steht, dass die Marke **in** der Karte sitzt und nicht daneben, und dass die
// Unterscheidung Mensch/API nicht über die Farbe läuft.
// stil-check: C03 die Ablage ist hier der Prüfgegenstand
public class EinflugmarkeGestaltungTests
{
    private static string Karte()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Karten", "Karte.razor"));
    }

    private static string Kartenstilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Karten", "Karte.razor.css"));
    }

    private static string Spaltenbahnen()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Spalten", "Spaltenbahnen.razor"));
    }

    private static string Bahnenstilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Spalten", "Spaltenbahnen.razor.css"));
    }

    private static string Kartenseite()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Kartendetail.razor"));
    }

    private static string Kartenseitenstilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Kartendetail.razor.css"));
    }

    private static string Startdatei()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Program.cs"));
    }

    // Die Marke steht **in** der Karte und nicht daneben: sie gehört der Karte, nicht der Bahn.
    [Test]
    public void Wenn_die_Karte_gelesen_wird_dann_steht_die_Marke_in_ihr_zwischen_Titel_und_Ablagezonen()
    {
        var karte = Karte();

        var stelleDesTitels = karte.IndexOf("class=\"karte-titel\"", StringComparison.Ordinal);
        var stelleDerMarke = karte.IndexOf("class=\"karte-einflugmarke\"", StringComparison.Ordinal);
        var stelleDesArtikelendes = karte.IndexOf("</article>", StringComparison.Ordinal);
        Assert.Multiple(() =>
        {
            Assert.That(stelleDerMarke, Is.GreaterThan(stelleDesTitels));
            Assert.That(stelleDerMarke, Is.LessThan(stelleDesArtikelendes), "Die Marke steht außerhalb der Karte.");
            Assert.That(karte, Does.Contain("data-karte-einflugmarke=\"@Kartendaten.KarteId\""));
        });
    }

    // Akzentkante und stärkerer Schatten kommen über eine eigene Klasse, nicht über ein Attribut an
    // der Karte: so bleibt die Gestaltung im Stilblatt.
    [Test]
    public void Wenn_die_Stilvorlage_der_Karte_gelesen_wird_dann_traegt_die_eingeflogene_Karte_Kante_und_Schatten_aus_dem_Token_Sheet()
    {
        var stil = Kartenstilvorlage();

        var regel = Regel(stil, ".karte-eingeflogen");
        Assert.Multiple(() =>
        {
            Assert.That(regel, Does.Contain("var(--color-accent)"));
            Assert.That(regel, Does.Contain("var(--shadow-md)"));
        });
    }

    // Die Unterscheidung Mensch/API läuft über **Anwesenheit und Wortlaut**, nie über die Farbe:
    // Olive und Terrakotta tragen in diesem Canvas die Art des Kontributors.
    [Test]
    public void Wenn_die_Regeln_der_Marke_gelesen_werden_dann_gibt_es_keine_zweite_Fassung_fuer_die_API()
    {
        var stil = Kartenstilvorlage();

        Assert.Multiple(() =>
        {
            Assert.That(stil, Does.Not.Contain(".karte-einflugmarke-api"));
            Assert.That(stil, Does.Not.Contain(".karte-einflugmarke-mensch"));
            Assert.That(stil, Does.Not.Contain("var(--color-accent-2)"), "Olive trägt die Art des Kontributors und nicht den Weg.");
        });
    }

    [Test]
    public void Wenn_die_Regeln_der_Marke_gelesen_werden_dann_tragen_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        var regeln = Regeln(Kartenstilvorlage(), @"\.karte-(?:einflugmarke|eingeflogen)");

        ErwarteOhneLiterale(regeln);
    }

    // Die zurückgehaltene Meldung steht über den Bahnen und nicht an einer Karte: sie sagt, dass
    // etwas wartet, und noch nicht, wo.
    [Test]
    public void Wenn_die_Bahnen_gelesen_werden_dann_steht_die_wartende_Meldung_ueber_ihnen()
    {
        var bahnen = Spaltenbahnen();

        var stelleDerMeldung = bahnen.IndexOf("id=\"wartende-aenderung\"", StringComparison.Ordinal);
        var stelleDerBahnen = bahnen.IndexOf("id=\"spaltenbahnen\"", StringComparison.Ordinal);
        Assert.Multiple(() =>
        {
            Assert.That(stelleDerMeldung, Is.GreaterThan(-1));
            Assert.That(stelleDerMeldung, Is.LessThan(stelleDerBahnen));
            Assert.That(bahnen, Does.Contain("role=\"status\""), "Eine wartende Änderung ist eine Statusmeldung, kein Alarm.");
        });
    }

    [Test]
    public void Wenn_die_Regeln_der_wartenden_Meldung_gelesen_werden_dann_tragen_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        ErwarteOhneLiterale(Regeln(Bahnenstilvorlage(), @"\.wartende-aenderung"));
        ErwarteOhneLiterale(Regeln(Kartenseitenstilvorlage(), @"\.wartende-aenderung"));
    }

    // Auf der Kartenseite steht die Marke neben der Spaltenangabe — eine Bewegung ändert genau die.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_steht_die_Marke_neben_der_Spaltenangabe()
    {
        var seite = Kartenseite();

        var stelleDerSpalte = seite.IndexOf("id=\"karte-spalte\"", StringComparison.Ordinal);
        var stelleDerMarke = seite.IndexOf("id=\"karte-einflugmarke\"", StringComparison.Ordinal);
        Assert.Multiple(() =>
        {
            Assert.That(stelleDerMarke, Is.GreaterThan(stelleDerSpalte));
            Assert.That(seite, Does.Contain("id=\"karte-wartende-aenderung\""));
        });
    }

    // Eine Leitung je Prozess und ein Verteiler je Prozess: die Leitung ist ein HostedService und
    // kein Kreislaufdienst, der Verteiler ein Singleton. Ein AddScoped an einer der beiden Stellen
    // machte aus einer Leitung zehn. Geprüft wird hier die **Registrierung**; dass daraus wirklich
    // eine Verbindung wird, belegt EreignisleitungTests am Verhalten.
    [Test]
    public void Wenn_die_Startdatei_gelesen_wird_dann_haelt_die_Anwendung_eine_Leitung_und_einen_Verteiler_je_Prozess()
    {
        var start = Startdatei();

        Assert.Multiple(() =>
        {
            Assert.That(start, Does.Contain("AddSingleton<Ereignisverteiler>()"));
            Assert.That(start, Does.Contain("AddHostedService("));
            Assert.That(start, Does.Not.Contain("AddScoped<Ereignisverteiler>"));
            Assert.That(start, Does.Not.Contain("AddScoped<Ereignisleitung>"));
        });
    }

    // Der Weg wird an **genau einer Stelle** gesetzt: am benannten Klienten. Eine vergessene
    // Aufrufstelle ließe die Marke lügen.
    [Test]
    public void Wenn_die_Startdatei_gelesen_wird_dann_sitzt_der_Wegkopf_am_benannten_Klienten()
    {
        var start = Startdatei();

        Assert.Multiple(() =>
        {
            Assert.That(start, Does.Contain("DefaultRequestHeaders.Add(Wegkopf.Name, Wegkopf.Oberflaechenwert)"));
            Assert.That(Regex.Matches(start, Regex.Escape("Wegkopf.Name")), Has.Count.EqualTo(1), "Der Weg gehört an eine einzige Stelle.");
        });
    }

    // Ein Singleton, an dem sich Kreisläufe anmelden, hält tote Kreisläufe fest, wenn niemand
    // abmeldet: beide zuhörenden Sichten melden sich in Dispose ab.
    [Test]
    public void Wenn_die_zuhoerenden_Sichten_gelesen_werden_dann_melden_sie_sich_beim_Abbau_wieder_ab()
    {
        var board = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Board.razor"));
        var kartenseite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(board, Does.Contain("@implements IDisposable"));
            Assert.That(board, Does.Contain("Ereignisverteiler.Gemeldet -= NimmEreignisAn"));
            Assert.That(kartenseite, Does.Contain("@implements IDisposable"));
            Assert.That(kartenseite, Does.Contain("Ereignisverteiler.Gemeldet -= NimmEreignisAn"));
        });
    }

    // Der Laufzeitmelder bleibt neben dem Verteiler bestehen und wird nicht ersetzt: er ist der Weg
    // für die **eigene** Handlung ohne Umweg über die WebApi.
    [Test]
    public void Wenn_die_Startdatei_gelesen_wird_dann_steht_der_Laufzeitmelder_weiter_als_Kreislaufdienst()
    {
        Assert.That(Startdatei(), Does.Contain("AddScoped<Laufzeitmelder>()"));
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

    private static string Regel(string stil, string selektor)
    {
        var anfang = stil.IndexOf(selektor + " {", StringComparison.Ordinal);
        Assert.That(anfang, Is.GreaterThan(-1), $"Die Regel {selektor} fehlt.");
        var ende = stil.IndexOf('}', anfang);
        return stil[anfang..ende];
    }
}
