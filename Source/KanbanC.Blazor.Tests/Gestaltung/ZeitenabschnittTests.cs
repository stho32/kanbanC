using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Die Gestaltungszusagen des Zeitenblocks und seines Formulars, geprüft an der Datei — wie die
// übrigen Gestaltungsprüfungen. Was sie im Browser tun, belegt die E2E-Strecke; was sie **nicht**
// tun, lässt sich dort nicht zeigen: dass kein Gestaltungswert als Literal in ihren Stilvorlagen
// steht und dass dasselbe Formular an beiden Orten steht statt zweimal geschrieben zu sein.
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

    private static string Formular()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Karten", "Zeiteintragsformular.razor"));
    }

    private static string Formularstilvorlage()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Karten", "Zeiteintragsformular.razor.css"));
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

    // Seit I0025 trägt der Block beides: den Nachtrag am Fuß und den Stift an jeder Zeile.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_der_Zeitenblock_Nachtragsknopf_und_Stift()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("id=\"zeiten-nachtragen-oeffnen\""));
            Assert.That(seite, Does.Contain("Zeit nachtragen"));
            Assert.That(seite, Does.Contain("class=\"zeiteneintragstift\""));
        });
    }

    // Der Nachtrag steht **hinter** der Einträgeliste und außerhalb ihres Leerzustands: auf einer
    // Karte ohne gemessene Zeit ist er der einzige Weg hinein.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_steht_der_Nachtrag_hinter_der_Eintraegeliste()
    {
        var seite = Kartenseite();

        var stelleDerListe = seite.IndexOf("id=\"zeiteneintragsliste\"", StringComparison.Ordinal);
        var stelleDesLeerstands = seite.IndexOf("id=\"zeiten-leerstand\"", StringComparison.Ordinal);
        var stelleDesNachtrags = seite.IndexOf("id=\"zeitennachtragsbereich\"", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(stelleDesNachtrags, Is.GreaterThan(stelleDerListe));
            Assert.That(stelleDesNachtrags, Is.GreaterThan(stelleDesLeerstands));
        });
    }

    // **Dasselbe Formular an zwei Orten** und nicht zweimal geschrieben: die Kartenseite setzt
    // die Komponente genau zweimal ein, einmal mit Tag und pflichtigem Ende, einmal ohne beides.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_steht_dasselbe_Formular_an_beiden_Orten()
    {
        var seite = Kartenseite();

        Assert.That(Regex.Matches(seite, "<Zeiteintragsformular"), Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("Kennung=\"zeitennachtrag\""));
            Assert.That(seite, Does.Contain("Kennung=\"zeitenaenderung\""));
            Assert.That(seite, Does.Contain("Bestaetigungsbeschriftung=\"Nachtragen\""));
            Assert.That(seite, Does.Contain("Bestaetigungsbeschriftung=\"Sichern\""));
        });
    }

    // Nur das Änderungsformular trägt das Tagesfeld nicht, das pflichtige Ende nicht und dafür
    // das Löschen: ein bestehender Eintrag ändert seine Uhrzeiten, nicht seinen Tag.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_nur_das_Aenderungsformular_das_Loeschen_und_keinen_Tag()
    {
        var seite = Kartenseite();

        var nachtrag = seite[seite.IndexOf("Kennung=\"zeitennachtrag\"", StringComparison.Ordinal)..];
        var aenderung = seite[seite.IndexOf("Kennung=\"zeitenaenderung\"", StringComparison.Ordinal)..];

        Assert.Multiple(() =>
        {
            Assert.That(nachtrag[..nachtrag.IndexOf("/>", StringComparison.Ordinal)], Does.Contain("MitTag=\"true\"").And.Contain("EndeIstPflicht=\"true\"").And.Contain("MitLoeschen=\"false\""));
            Assert.That(aenderung[..aenderung.IndexOf("/>", StringComparison.Ordinal)], Does.Contain("MitTag=\"false\"").And.Contain("EndeIstPflicht=\"false\"").And.Contain("MitLoeschen=\"true\""));
        });
    }

    // Die Gründeliste steht **im Formular**, über den Eingaben — Form .meldung.meldung-abweisung
    // wie die zurückgewiesene Kartenanlage, und nicht in der Meldungsstelle am Kopf der Seite:
    // dort stünde sie außerhalb des Blicks, in dem gerade getippt wird.
    [Test]
    public void Wenn_das_Formular_gelesen_wird_dann_steht_die_Gruendeliste_ueber_den_Eingaben()
    {
        var formular = Formular();

        var stelleDerMeldung = formular.IndexOf("meldung meldung-abweisung", StringComparison.Ordinal);
        var stelleDerEingaben = formular.IndexOf("zeitenformularzeile", StringComparison.Ordinal);

        Assert.That(stelleDerMeldung, Is.GreaterThan(-1));
        Assert.Multiple(() =>
        {
            Assert.That(stelleDerMeldung, Is.LessThan(stelleDerEingaben));
            Assert.That(formular, Does.Contain("id=\"@Feldkennung(\"zurueckweisung\")\""));
            Assert.That(formular, Does.Contain("id=\"@Feldkennung(\"ergibt\")\""));
        });
    }

    // Alle Gestaltungswerte des Formulars kommen aus gestaltung.css.
    [Test]
    public void Wenn_die_Stilvorlage_des_Formulars_gelesen_wird_dann_traegt_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        var stil = Formularstilvorlage();

        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(stil, @"#[0-9a-fA-F]{3,8}\b"), Is.Empty, "Farben gehören ins Token-Sheet.");
            Assert.That(Regex.Matches(stil, @"(?:padding|margin|gap|border-radius)[^;]*\d+px"), Is.Empty, "Abstände und Radien kommen aus dem Token-Sheet.");
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
