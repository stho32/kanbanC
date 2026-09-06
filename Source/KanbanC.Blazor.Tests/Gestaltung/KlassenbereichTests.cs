using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Die Gestaltungszusagen des Klassenbereichs, geprüft an der Datei — wie die übrigen
// Gestaltungsprüfungen. Was er im Browser tut, belegt die E2E-Strecke; was er **nicht** tut,
// lässt sich dort nicht zeigen: dass die Eingabefelder kein value-Attribut tragen, dass kein
// Gestaltungswert als Literal in der Komponenten-CSS steht und dass keine Kennung des Bereichs
// unter die Zählzusagen des Layout-Modus fällt.
// stil-check: C03 die Ablage ist hier der Prüfgegenstand
[TestFixture]
public class KlassenbereichTests
{
    private static string Klassenpflege()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Klassen", "Klassenpflege.razor"));
    }

    private static string Stilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Klassen", "Klassenpflege.razor.css"));
    }

    [Test]
    public void Wenn_die_Komponente_gelesen_wird_dann_traegt_sie_die_Kennungen_des_Bereichs()
    {
        var komponente = Klassenpflege();

        Assert.Multiple(() =>
        {
            Assert.That(komponente, Does.Contain("id=\"klassenpflege\""));
            Assert.That(komponente, Does.Contain("id=\"klassenliste\""));
            Assert.That(komponente, Does.Contain("id=\"keine-klassen\""));
            Assert.That(komponente, Does.Contain("id=\"neue-klasse\""));
            Assert.That(komponente, Does.Contain("id=\"klassen-zurueckweisung\""));
            Assert.That(komponente, Does.Contain("id=\"klassen-fehlermeldung\""));
        });
    }

    // Die Beschriftung heißt „Klassen“ und nicht „Kartenklassen“: im Board steht das Wort neben
    // „Spalten“ und ist dort so eindeutig wie dieses. Der Bezeichner heißt trotzdem überall
    // Kartenklasse.
    [Test]
    public void Wenn_die_Komponente_gelesen_wird_dann_heisst_die_Ueberschrift_Klassen()
    {
        var komponente = Klassenpflege();

        Assert.That(komponente, Does.Contain(">Klassen<"));
        Assert.That(komponente, Does.Not.Contain(">Kartenklassen<"));
    }

    [Test]
    public void Wenn_die_Komponente_gelesen_wird_dann_traegt_die_Anlegezeile_beide_Felder_und_den_Knopf()
    {
        var komponente = Klassenpflege();

        Assert.Multiple(() =>
        {
            Assert.That(komponente, Does.Contain(">Name</label>"));
            Assert.That(komponente, Does.Contain(">Nummernkreis-Präfix</label>"));
            Assert.That(komponente, Does.Contain(">Klasse anlegen</button>"));
        });
    }

    [Test]
    public void Wenn_die_Komponente_gelesen_wird_dann_lautet_der_Leerzustand_wie_bei_den_Spalten()
    {
        Assert.That(Klassenpflege(), Does.Contain("Dieses Board hat keine Klasse."));
    }

    [Test]
    public void Wenn_die_Komponente_gelesen_wird_dann_traegt_die_Zurueckweisung_ihren_Satz_und_die_Befundliste()
    {
        var komponente = Klassenpflege();

        Assert.Multiple(() =>
        {
            Assert.That(komponente, Does.Contain("Die Klasse wurde nicht angelegt:"));
            Assert.That(komponente, Does.Contain("meldung meldung-abweisung"));
            Assert.That(komponente, Does.Contain("@befund.Meldung"));
        });
    }

    // Wie bei Spalte, Teilaufgabe und Kommentar: **kein value-Attribut**. Trüge das Feld den
    // serverseitigen Stand, schriebe jede zurückkommende Renderrunde den Stand von vorhin über
    // das, was inzwischen getippt wurde.
    [Test]
    public void Wenn_die_Komponente_gelesen_wird_dann_tragen_die_Eingabefelder_kein_value_Attribut()
    {
        var komponente = Klassenpflege();

        Assert.That(komponente, Does.Not.Contain("value="));
        Assert.That(komponente, Does.Contain("@bind=\"_neueKlasse.Name\""));
        Assert.That(komponente, Does.Contain("@bind=\"_neueKlasse.Praefix\""));
    }

    // Die Anlegezeile heißt bewusst nicht .spaltenpflege-neu, obwohl sie so aussieht: die
    // Zählzusagen des Layout-Modus zählen unter #spaltenbahnen und auf #neue-spalte, und eine
    // geteilte Kennung machte einen grünen Test rot, ohne dass sich an den Spalten etwas
    // geändert hätte. Die Form wird übernommen, der Name nicht.
    [Test]
    public void Wenn_die_Komponente_gelesen_wird_dann_teilt_sie_keine_Kennung_mit_der_Spaltenpflege()
    {
        var komponente = Klassenpflege();

        Assert.Multiple(() =>
        {
            Assert.That(komponente, Does.Not.Contain("spaltenpflege-neu"));
            Assert.That(komponente, Does.Not.Contain("spaltenbahn"));
            Assert.That(komponente, Does.Not.Contain("neue-spalte"));
        });
    }

    [Test]
    public void Wenn_die_Komponente_gelesen_wird_dann_traegt_die_Zeile_das_Praefix_als_Plakette_und_die_naechste_Nummer()
    {
        var komponente = Klassenpflege();

        Assert.Multiple(() =>
        {
            Assert.That(komponente, Does.Contain("class=\"tag tag-neutral klassenpraefix\""));
            Assert.That(komponente, Does.Contain("vergeben · nächste"));
            Assert.That(komponente, Does.Contain("Kartennummer.Aus(kartenklasse.Praefix, kartenklasse.Zaehlerstand + 1)"));
        });
    }

    // Der Bereich sitzt unter der Spaltenpflege, getrennt durch dieselbe Linie, die das
    // Token-Sheet zieht.
    [Test]
    public void Wenn_die_Komponente_gelesen_wird_dann_trennt_die_Linie_des_Token_Sheets_sie_von_der_Spaltenpflege()
    {
        Assert.That(Klassenpflege(), Does.Contain("<hr class=\"hr\" />"));
    }

    [Test]
    public void Wenn_die_Boardseite_gelesen_wird_dann_steht_der_Bereich_nur_im_Layout_Zweig_unter_der_Spaltenpflege()
    {
        var boardseite = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Board.razor"));
        var stelleDerSpaltenpflege = boardseite.IndexOf("<Spaltenpflege", StringComparison.Ordinal);
        var stelleDerKlassenpflege = boardseite.IndexOf("<Klassenpflege", StringComparison.Ordinal);
        var stelleDerArbeitsansicht = boardseite.IndexOf("<Spaltenbahnen", StringComparison.Ordinal);

        Assert.That(stelleDerKlassenpflege, Is.GreaterThan(stelleDerSpaltenpflege));
        Assert.That(stelleDerKlassenpflege, Is.LessThan(stelleDerArbeitsansicht), "Der Bereich gehört in den Layout-Zweig, nicht in die Arbeitsansicht.");
        Assert.That(Regex.Matches(boardseite, "<Klassenpflege"), Has.Count.EqualTo(1));
    }

    // Alle Gestaltungswerte kommen aus gestaltung.css: kein Farb-, Abstands- oder Radius-Literal
    // in der Komponenten-CSS.
    [Test]
    public void Wenn_die_Stilvorlage_gelesen_wird_dann_traegt_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        var stil = Stilvorlage();

        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(stil, @"#[0-9a-fA-F]{3,8}\b"), Is.Empty, "Farben gehören ins Token-Sheet.");
            Assert.That(Regex.Matches(stil, @"(?:padding|margin|gap|border-radius)[^;]*\d+px"), Is.Empty, "Abstände und Radien kommen aus dem Token-Sheet.");
        });
    }

    [Test]
    public void Wenn_die_Stilvorlage_gelesen_wird_dann_traegt_die_Klassenzeile_die_Flaeche_des_Token_Sheets()
    {
        var stil = Stilvorlage();

        Assert.Multiple(() =>
        {
            Assert.That(stil, Does.Contain("background: var(--color-surface)"));
            Assert.That(stil, Does.Contain("border-radius: var(--radius-xs)"));
        });
    }
}
