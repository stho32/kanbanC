using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Die Gestaltungszusagen des Dateiverweisabschnitts, geprüft an der Datei — wie die übrigen
// Gestaltungsprüfungen. Was er im Browser tut, belegt die E2E-Strecke; was er **nicht** tut,
// lässt sich dort nicht zeigen: dass kein Gestaltungswert als Literal darin steht, dass das
// Eingabefeld kein value-Attribut trägt und dass neben ihm kein Knopf steht.
// stil-check: C03 die Ablage ist hier der Prüfgegenstand
public class DateiverweisabschnittTests
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
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_sie_den_Abschnitt_Dateiverweise_mit_seinen_Bedienelementen()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("id=\"dateiverweisabschnitt\""));
            Assert.That(seite, Does.Contain(">Dateiverweise<"));
            Assert.That(seite, Does.Contain("id=\"dateiverweisliste\""));
            Assert.That(seite, Does.Contain("id=\"dateiverweis-leerstand\""));
            Assert.That(seite, Does.Contain("id=\"dateiverweis-eingabe\""));
            Assert.That(seite, Does.Contain("Pfad im Repository eintragen"));
            Assert.That(seite, Does.Contain("id=\"dateiverweis-hinweis\""));
        });
    }

    // Die erste der vier bewussten Abweichungen vom Artboard, und die einzige, die ein Wort
    // betrifft: die Überschrift heißt „Dateiverweise" und nicht „Verweise". C06 verlangt einen
    // Begriff in einer Schreibweise; „Verweis" ist im Code als Hyperlink vergeben, und zwei
    // Bedeutungen desselben Worts stünden auf demselben Schirm nebeneinander.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_heisst_die_Ueberschrift_Dateiverweise_und_nicht_Verweise()
    {
        var seite = Kartenseite();

        Assert.That(seite, Does.Contain(">Dateiverweise<"));
        Assert.That(seite, Does.Not.Contain(">Verweise<"));
    }

    // Der Abschnitt steht **rechts** in der zweispaltigen Sektion, hinter den Anhängen.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_steht_der_Abschnitt_rechts_neben_den_Anhaengen()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("id=\"anhaengeUndVerweise\""));
            Assert.That(
                seite.IndexOf("id=\"dateiverweisabschnitt\"", StringComparison.Ordinal),
                Is.GreaterThan(seite.IndexOf("id=\"anhangabschnitt\"", StringComparison.Ordinal)));
        });
    }

    // Wie bei Teilaufgabe, Etikett und Kommentar: **kein value-Attribut**. Trüge das Feld den
    // serverseitigen Stand, schriebe jede zurückkommende Renderrunde den Stand von vorhin über
    // das, was inzwischen getippt wurde.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_das_Eingabefeld_kein_value_Attribut()
    {
        var eingabefeld = Eingabefeldmarkup();

        Assert.That(eingabefeld, Does.Not.Contain("value="));
    }

    // Abgeschickt wird mit der **Eingabetaste**; das Artboard zeichnet keinen Knopf, und ein
    // zweiter Weg wäre einer mehr als gezeichnet.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_schickt_die_Eingabetaste_ab_und_es_gibt_keinen_Knopf()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("@onkeydown=\"AufDateiverweistaste\""));
            Assert.That(seite, Does.Not.Contain("id=\"dateiverweis-senden\""));
            Assert.That(seite, Does.Not.Contain("id=\"dateiverweis-hinzufuegen\""));
        });
    }

    // Zweite bewusste Abweichung: ein `×` am Zeilenende. Ohne Bedienelement wäre die
    // Entfernen-Route in der Oberfläche unerreichbar — und weil die Anwendung ausdrücklich nicht
    // prüft, ob ein Pfad noch stimmt, kann sie den Verfall nicht selbst bemerken.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_jede_Dateiverweiszeile_ein_Kreuz_zum_Entfernen()
    {
        Assert.That(Kartenseite(), Does.Contain("class=\"dateiverweis-entfernen\""));
    }

    // Dritte bewusste Abweichung: Urheber und Zeitpunkt stehen im title der Zeile und nicht als
    // zweite Textzeile — so bleibt die gezeichnete einzeilige Form. Wörtlich das Vorgehen am
    // Anhang daneben.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_die_Dateiverweiszeile_Urheber_und_Zeitpunkt_im_title()
    {
        Assert.That(Kartenseite(), Does.Contain("title=\"@Dateiverweismeta(dateiverweis)\""));
    }

    // **Der Unterschied zum Anhang ohne Beschriftung:** Schreibmaschinenschrift an oliver Kante
    // hier, Büroklammer und Größenangabe dort. Beides kommt aus dem Token-Sheet — die Olivfarbe
    // als --color-accent-2, die Schrift als --font-mono.
    [Test]
    public void Wenn_die_Stilvorlage_gelesen_wird_dann_traegt_die_Dateiverweiszeile_Schreibmaschinenschrift_an_oliver_Kante()
    {
        var stil = Stilvorlage();

        Assert.Multiple(() =>
        {
            Assert.That(stil, Does.Contain("font-family: var(--font-mono)"));
            Assert.That(stil, Does.Contain("border-left: 3px solid var(--color-accent-2)"));
        });
    }

    // Ein zu langer Pfad wird in der Zeile gekürzt dargestellt und zieht die Spalte nicht auf;
    // gespeichert und kopiert wird der ganze.
    [Test]
    public void Wenn_die_Stilvorlage_gelesen_wird_dann_kuerzt_sie_einen_langen_Pfad_mit_einer_Ellipse()
    {
        var pfadregel = Stilregel(".dateiverweispfad");

        Assert.Multiple(() =>
        {
            Assert.That(pfadregel, Does.Contain("text-overflow: ellipsis"));
            Assert.That(pfadregel, Does.Contain("white-space: nowrap"));
            Assert.That(pfadregel, Does.Contain("overflow: hidden"));
        });
    }

    // Das Token für die Schreibmaschinenschrift steht im Token-Sheet und nicht als Literal in der
    // Komponenten-CSS — Projektregel, hier fuer eine Schriftart statt einer Farbe.
    [Test]
    public void Wenn_das_Token_Sheet_gelesen_wird_dann_traegt_es_die_Schreibmaschinenschrift_als_Token()
    {
        var tokenSheet = File.ReadAllText(Quelltextbaum.BlazorDatei("wwwroot", "gestaltung.css"));

        Assert.That(tokenSheet, Does.Contain("--font-mono:"));
        Assert.That(Stilvorlage(), Does.Not.Contain("ui-monospace"), "Der Schriftstapel gehoert ins Token-Sheet, nicht in die Komponenten-CSS.");
    }

    private static string Eingabefeldmarkup()
    {
        var seite = Kartenseite();
        var beginn = seite.IndexOf("id=\"dateiverweis-eingabe\"", StringComparison.Ordinal);
        Assert.That(beginn, Is.GreaterThan(0), "Ohne das Eingabefeld prueft dieser Test nichts.");
        var ende = seite.IndexOf("/>", beginn, StringComparison.Ordinal);
        return seite[beginn..ende];
    }

    private static string Stilregel(string wahlsatz)
    {
        var stil = Stilvorlage();
        var beginn = stil.IndexOf(wahlsatz + " {", StringComparison.Ordinal);
        Assert.That(beginn, Is.GreaterThan(0), $"Die Regel {wahlsatz} fehlt.");
        var ende = stil.IndexOf('}', beginn);
        return stil[beginn..ende];
    }
}
