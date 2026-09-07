using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Die Gestaltungszusagen der Laufzeitplakette und ihres Popovers, geprüft an der Datei. Was sie im
// Browser tun, belegt die E2E-Strecke; was sie **nicht** tun, lässt sich dort nicht zeigen: dass
// kein Gestaltungswert als Literal in ihren Regeln steht, dass die Plakette neben dem
// Identitätsplatz und nicht im Ausgabefeld der offenen Seite sitzt und dass es im Popover keinen
// Startknopf gibt.
// stil-check: C03 die Ablage ist hier der Prüfgegenstand
public class LaufzeitplaketteTests
{
    private static string Kopfzeile()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Layout", "Kopfzeile.razor"));
    }

    private static string Kopfzeilenstilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Layout", "Kopfzeile.razor.css"));
    }

    private static string Popover()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Layout", "Laufzeitpopover.razor"));
    }

    private static string Popoverstilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Layout", "Laufzeitpopover.razor.css"));
    }

    [Test]
    public void Wenn_die_Kopfzeile_gelesen_wird_dann_traegt_sie_Plakette_und_Popover_mit_ihrem_Titel()
    {
        var kopfzeile = Kopfzeile();

        Assert.Multiple(() =>
        {
            Assert.That(kopfzeile, Does.Contain("id=\"laufzeitzaehler\""));
            Assert.That(kopfzeile, Does.Contain("id=\"laufzeitpopover\""));
            Assert.That(kopfzeile, Does.Contain("id=\"laufzeit-auffangflaeche\""));
            Assert.That(kopfzeile, Does.Contain(">Läuft gerade …<"));
        });
    }

    // Dieselbe Mechanik wie die Identitätswahl: ein Knopf mit aria-haspopup und aria-expanded.
    [Test]
    public void Wenn_die_Plakette_gelesen_wird_dann_traegt_sie_dieselbe_Popovermechanik_wie_die_Identitaetswahl()
    {
        var plakette = Plakettenknopf(Kopfzeile());

        Assert.Multiple(() =>
        {
            Assert.That(plakette, Does.Contain("aria-haspopup=\"true\""));
            Assert.That(plakette, Does.Contain("aria-expanded=\"@Laufzeitstand()\""));
            Assert.That(plakette, Does.Contain("@onclick=\"SchalteLaufzeitliste\""));
        });
    }

    // Die Plakette gehört **neben** den Identitätsplatz und nicht in das Ausgabefeld
    // kopfzeile-bedienung: das füllt die offene Seite, diese Plakette gilt seitenübergreifend.
    [Test]
    public void Wenn_die_Kopfzeile_gelesen_wird_dann_steht_die_Plakette_zwischen_dem_Ausgabefeld_und_dem_Identitaetsplatz()
    {
        var kopfzeile = Kopfzeile();

        var stelleDesAusgabefelds = kopfzeile.IndexOf("SectionName=\"kopfzeile-bedienung\"", StringComparison.Ordinal);
        var stelleDerPlakette = kopfzeile.IndexOf("id=\"laufzeitzaehler\"", StringComparison.Ordinal);
        var stelleDesIdentitaetsplatzes = kopfzeile.IndexOf("class=\"identitaetsplatz\"", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(stelleDesAusgabefelds, Is.GreaterThan(-1));
            Assert.That(stelleDerPlakette, Is.GreaterThan(stelleDesAusgabefelds));
            Assert.That(stelleDerPlakette, Is.LessThan(stelleDesIdentitaetsplatzes));
        });
    }

    // Die Plakette zählt und nennt keine Dauer: ihre Beschriftung kommt aus dem Laufzaehler, und
    // keine Dauerform steht daneben.
    [Test]
    public void Wenn_die_Plakette_gelesen_wird_dann_zeigt_sie_die_Beschriftung_des_Laufzaehlers_und_keine_Dauer()
    {
        var plakette = Plakettenknopf(Kopfzeile());

        Assert.Multiple(() =>
        {
            Assert.That(plakette, Does.Contain("@_laufzaehler.Beschriftung"));
            Assert.That(plakette, Does.Contain("title=\"@_laufzaehler.Titel\""));
            Assert.That(plakette, Does.Not.Contain("Dauerform"));
        });
    }

    // Der leere Fall ist Abwesenheit und kein Satz: die Plakette hängt an derselben Bedingung, mit
    // der der Laufzaehler null wird.
    [Test]
    public void Wenn_die_Kopfzeile_gelesen_wird_dann_haengt_die_Plakette_am_Laufzaehler_und_traegt_keinen_Leerzustandssatz()
    {
        var kopfzeile = Kopfzeile();

        Assert.Multiple(() =>
        {
            Assert.That(kopfzeile, Does.Contain("@if (_laufzaehler is not null)"));
            Assert.That(kopfzeile, Does.Not.Contain("0 laufen"));
        });
    }

    // Höchstens eines der beiden Popover ist offen: ein Zustand mit drei Werten statt zweier
    // Wahrheitswerte, die den verbotenen Zustand zuließen.
    [Test]
    public void Wenn_die_Kopfzeile_gelesen_wird_dann_fuehrt_sie_einen_Popoverzustand_statt_zweier_Wahrheitswerte()
    {
        var kopfzeile = Kopfzeile();

        Assert.Multiple(() =>
        {
            Assert.That(kopfzeile, Does.Contain("private Kopfzeilenpopover _offenesPopover"));
            Assert.That(kopfzeile, Does.Not.Contain("_popoverIstOffen"));
        });
    }

    // Je Zeile: Initialenkreis, Kartennummer und Titel als Verweis auf die Kartenseite, die
    // Startzeit und das Stoppquadrat; darunter Board und Kontributor.
    [Test]
    public void Wenn_das_Popover_gelesen_wird_dann_traegt_jede_Zeile_Kuerzel_Kartenverweis_Startzeit_und_Stoppquadrat()
    {
        var popover = Popover();

        Assert.Multiple(() =>
        {
            Assert.That(popover, Does.Contain("class=\"kuerzel"));
            Assert.That(popover, Does.Contain("href=\"karten/@messung.Karte.KarteId\""));
            Assert.That(popover, Does.Contain("seit @Zeitpunktform.AlsTageszeit(messung.Zeiteintrag.Beginn)"));
            Assert.That(popover, Does.Contain("class=\"laufzeitstopp\""));
            Assert.That(popover, Does.Contain("class=\"laufzeitmeta\""));
        });
    }

    // Gestoppt wird hier, gestartet nicht: ein Start braucht eine Karte, und ohne sie müsste er
    // eine erfinden.
    [Test]
    public void Wenn_das_Popover_gelesen_wird_dann_gibt_es_darin_keinen_Startknopf()
    {
        var popover = Popover();

        Assert.Multiple(() =>
        {
            Assert.That(popover, Does.Not.Contain("StarteZeitmessung"));
            Assert.That(popover, Does.Not.Contain("Timer starten"));
        });
    }

    // Eine Zeile trägt keine Dauer — auf ihr wäre sie eindeutig, aber ab der ersten Sekunde ebenso
    // falsch wie in der Plakette.
    [Test]
    public void Wenn_das_Popover_gelesen_wird_dann_traegt_keine_Zeile_eine_Dauer()
    {
        var popover = Popover();

        Assert.Multiple(() =>
        {
            Assert.That(popover, Does.Not.Contain("Dauerform"));
            Assert.That(popover, Does.Not.Contain("Zeitbilanz"));
        });
    }

    // Alle Gestaltungswerte kommen aus gestaltung.css: kein Farb-, Abstands- oder Radius-Literal in
    // der Stilvorlage des Popovers.
    [Test]
    public void Wenn_die_Stilvorlage_des_Popovers_gelesen_wird_dann_traegt_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        var stil = Popoverstilvorlage();

        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(stil, @"#[0-9a-fA-F]{3,8}\b"), Is.Empty, "Farben gehören ins Token-Sheet.");
            Assert.That(Regex.Matches(stil, @"(?:padding|margin|gap|border-radius)[^;]*\d+px"), Is.Empty, "Abstände und Radien kommen aus dem Token-Sheet.");
        });
    }

    // Nur die Regeln der Plakette — die übrige Kopfzeilenvorlage ist älter als diese Zusage und
    // wird hier nicht mitgeprüft.
    [Test]
    public void Wenn_die_Regeln_der_Plakette_gelesen_werden_dann_tragen_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        var regeln = Plakettenregeln(Kopfzeilenstilvorlage());

        Assert.Multiple(() =>
        {
            Assert.That(regeln, Is.Not.Empty, "Ohne Regeln prüfte der Test nichts.");
            Assert.That(Regex.Matches(regeln, @"#[0-9a-fA-F]{3,8}\b"), Is.Empty, "Farben gehören ins Token-Sheet.");
            Assert.That(Regex.Matches(regeln, @"(?:padding|margin|gap|border-radius)[^;]*\d+px"), Is.Empty, "Abstände und Radien kommen aus dem Token-Sheet.");
        });
    }

    // Eigen und fremd unterscheiden sich über die Füllung: gefüllt im Akzentton, ruhig ohne.
    [Test]
    public void Wenn_die_Stilvorlage_gelesen_wird_dann_ist_die_eigene_Plakette_gefuellt_und_die_fremde_ruhig()
    {
        var stil = Kopfzeilenstilvorlage();

        Assert.Multiple(() =>
        {
            Assert.That(stil, Does.Contain(".kopfzeile-laufzeit-eigen"));
            Assert.That(stil, Does.Contain(".kopfzeile-laufzeit-fremd"));
            Assert.That(Plakettenregel(stil, ".kopfzeile-laufzeit-eigen"), Does.Contain("background: var(--color-accent)"));
            Assert.That(Plakettenregel(stil, ".kopfzeile-laufzeit-fremd"), Does.Contain("background: transparent"));
        });
    }

    private static string Plakettenknopf(string kopfzeile)
    {
        var anfang = kopfzeile.IndexOf("id=\"laufzeitzaehler\"", StringComparison.Ordinal);
        Assert.That(anfang, Is.GreaterThan(-1), "Die Plakette fehlt in der Kopfzeile.");
        var ende = kopfzeile.IndexOf("</button>", anfang, StringComparison.Ordinal);
        return kopfzeile[anfang..ende];
    }

    private static string Plakettenregeln(string stil)
    {
        var regeln = Regex.Matches(stil, @"\.(?:kopfzeile-laufzeit|laufzeit)[a-z-]*[^{}]*\{[^}]*\}");
        return string.Join(Environment.NewLine, regeln.Select(treffer => treffer.Value));
    }

    private static string Plakettenregel(string stil, string selektor)
    {
        var anfang = stil.IndexOf(selektor + " {", StringComparison.Ordinal);
        Assert.That(anfang, Is.GreaterThan(-1), $"Die Regel {selektor} fehlt.");
        var ende = stil.IndexOf('}', anfang);
        return stil[anfang..ende];
    }
}
