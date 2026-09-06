using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Die Gestaltungszusagen des Zeitenblocks, geprüft an der Datei — wie die übrigen
// Gestaltungsprüfungen. Was er im Browser tut, belegt die E2E-Strecke; was er **nicht** tut,
// lässt sich dort nicht zeigen: dass kein Gestaltungswert als Literal in seiner Stilvorlage steht
// und dass weder ein Stift noch ein Nachtragsfeld mitgekommen sind, die I0025 gehören.
// stil-check: C03 die Ablage ist hier der Prüfgegenstand
public class ZeitenabschnittTests
{
    private static string Kartenseite()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Kartendetail.razor"));
    }

    private static string Stilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Kartendetail.razor.css"));
    }

    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_der_Zeitenblock_Bilanz_Liste_und_Leerzustand()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("id=\"zeitenabschnitt\""));
            Assert.That(seite, Does.Contain("id=\"zeiten-ist\""));
            Assert.That(seite, Does.Contain("id=\"zeitensummen\""));
            Assert.That(seite, Does.Contain("id=\"zeitenzahl\""));
            Assert.That(seite, Does.Contain("id=\"zeiteneintragsliste\""));
            Assert.That(seite, Does.Contain("id=\"zeiten-leerstand\""));
            Assert.That(seite, Does.Contain(">Noch keine Zeit erfasst.<"));
            Assert.That(seite, Does.Contain(">Einträge<"));
        });
    }

    // Die Reihenfolge des Artboards: erst die Handlung, dann die Bilanz, dann die Belege.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_steht_die_Bilanz_ueber_der_Eintraegeliste()
    {
        var seite = Kartenseite();

        var stelleDerHandlung = seite.IndexOf("id=\"zeiten-ist\"", StringComparison.Ordinal);
        var stelleDerBilanz = seite.IndexOf("id=\"zeitensummen\"", StringComparison.Ordinal);
        var stelleDerListe = seite.IndexOf("id=\"zeiteneintragsliste\"", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(stelleDerBilanz, Is.GreaterThan(stelleDerHandlung));
            Assert.That(stelleDerListe, Is.GreaterThan(stelleDerBilanz));
        });
    }

    // Gerechnet wird an genau einer Stelle: **ein** Aufruf der Zeitbilanz trägt Summenzeilen und
    // Ist-Summe. Ein zweiter Aufruf wäre eine zweite Rechnung derselben Zahl.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_steht_die_Zeitbilanz_genau_einmal_darin()
    {
        Assert.That(Regex.Matches(Kartenseite(), @"Zeitbilanz\.Fuer\("), Has.Count.EqualTo(1));
    }

    // Der Stift zum Ändern und das Nachtragsfeld des Artboards sind bewusst nicht mitgekommen:
    // sie gehören I0025, und eine Handlung, die nichts tut, wäre schlechter als keine. Dass auch
    // die gezeichnete Vorgabezeit fehlt, prüft die E2E-Strecke am gerenderten Block — hier stünde
    // das Wort schon in der Begründung daneben.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_der_Zeitenblock_weder_Nachtragsfeld_noch_Stift()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Not.Contain("Zeit nachtragen"));
            Assert.That(seite, Does.Not.Contain("zeiteneintrag-aendern"));
        });
    }

    // Drei Merkmale, von denen keines nur Farbe ist: die Akzentkante links, das Wort „läuft" (es
    // entsteht in der Zeitpunktform) und das Stoppquadrat als einzige Handlung der Zeile.
    [Test]
    public void Wenn_die_Stilvorlage_gelesen_wird_dann_traegt_die_laufende_Zeile_ihre_Akzentkante()
    {
        var stil = Stilvorlage();

        Assert.Multiple(() =>
        {
            Assert.That(stil, Does.Contain(".zeiteneintrag-laeuft"));
            Assert.That(stil, Does.Contain("border-left: 3px solid var(--color-accent)"));
            Assert.That(Kartenseite(), Does.Contain("class=\"zeiteneintragstopp\""));
        });
    }

    // Alle Gestaltungswerte des Blocks kommen aus gestaltung.css: kein Farbliteral in den Regeln
    // des Zeitenblocks, keine Abstände und Radien in Pixeln.
    [Test]
    public void Wenn_die_Regeln_des_Zeitenblocks_gelesen_werden_dann_tragen_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        var regeln = Zeitenregeln(Stilvorlage());

        Assert.Multiple(() =>
        {
            Assert.That(regeln, Is.Not.Empty, "Ohne Regeln prüfte der Test nichts.");
            Assert.That(Regex.Matches(regeln, @"#[0-9a-fA-F]{3,8}\b"), Is.Empty, "Farben gehören ins Token-Sheet.");
            Assert.That(Regex.Matches(regeln, @"(?:padding|margin|gap|border-radius)[^;]*\d+px"), Is.Empty, "Abstände und Radien kommen aus dem Token-Sheet.");
        });
    }

    // Nur die Regeln, deren Selektor zum Zeitenblock gehört — die übrige Stilvorlage ist älter als
    // diese Zusage und wird hier nicht mitgeprüft.
    private static string Zeitenregeln(string stil)
    {
        var regeln = Regex.Matches(stil, @"\.zeiten[a-z-]*[^{}]*\{[^}]*\}|\.zeiteneintrag[a-z-]*[^{}]*\{[^}]*\}");
        return string.Join(Environment.NewLine, regeln.Select(treffer => treffer.Value));
    }
}
