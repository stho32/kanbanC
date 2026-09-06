using KanbanC.Blazor.Tests.TestHelpers;
using KanbanC.Contracts.Karten;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Die Gestaltungszusagen des Anhangabschnitts, geprüft an der Datei — wie die übrigen
// Gestaltungsprüfungen. Was er im Browser tut, belegt die E2E-Strecke; was er **nicht** tut,
// lässt sich dort nicht zeigen: dass kein Gestaltungswert als Literal darin steht und dass der
// Verweis nicht auf eine Blazor-Route zeigt.
// stil-check: C03 die Ablage ist hier der Prüfgegenstand
public class AnhangabschnittTests
{
    private static string Kartenseite()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Kartendetail.razor"));
    }

    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_sie_den_Abschnitt_Anhaenge_mit_seinen_Bedienelementen()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("id=\"anhangabschnitt\""));
            Assert.That(seite, Does.Contain(">Anhänge<"));
            Assert.That(seite, Does.Contain("id=\"anhangliste\""));
            Assert.That(seite, Does.Contain("id=\"anhang-leerstand\""));
            Assert.That(seite, Does.Contain("id=\"anhang-ablegeflaeche\""));
            Assert.That(seite, Does.Contain("Datei hierher ziehen oder wählen"));
            Assert.That(seite, Does.Contain("id=\"anhang-hinweis\""));
        });
    }

    // Der Abschnitt steht hinter „Kommentare" und ist die **linke** Haelfte einer zweispaltigen
    // Sektion; rechts stehen die Dateiverweise. Die Pruefung auf die Abwesenheit von
    // „verweisplatz" zeigt auf eine Form, die es nicht mehr geben darf: ein leerer Platzhalter
    // neben einem gebauten Abschnitt waere eine dritte Haelfte.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_steht_der_Abschnitt_links_in_einer_zweispaltigen_Sektion()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite.IndexOf("id=\"anhangabschnitt\"", StringComparison.Ordinal), Is.GreaterThan(seite.IndexOf("id=\"kommentarabschnitt\"", StringComparison.Ordinal)));
            Assert.That(seite, Does.Contain("id=\"anhaengeUndVerweise\""));
            Assert.That(seite.IndexOf("id=\"anhangabschnitt\"", StringComparison.Ordinal), Is.LessThan(seite.IndexOf("id=\"dateiverweisabschnitt\"", StringComparison.Ordinal)));
            Assert.That(seite, Does.Not.Contain("id=\"verweisplatz\""), "Neben dem gebauten Abschnitt darf kein leerer Platzhalter stehen.");
        });
    }

    // Erste bewusste Abweichung vom Artboard: ein `×` am Zeilenende. Ohne Bedienelement waere die
    // Entfernen-Route in der Oberflaeche unerreichbar.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_jede_Anhangzeile_ein_Kreuz_zum_Entfernen()
    {
        Assert.That(Kartenseite(), Does.Contain("class=\"anhang-entfernen\""));
    }

    // Zweite bewusste Abweichung: Urheber und Zeitpunkt stehen im title der Zeile und nicht als
    // zweite Textzeile — so bleibt die gezeichnete einzeilige Form.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_die_Anhangzeile_Urheber_und_Zeitpunkt_im_title()
    {
        Assert.That(Kartenseite(), Does.Contain("title=\"@Anhangmeta(anhang)\""));
    }

    // Der Verweis zeigt direkt auf die WebApi und nicht auf eine Blazor-Route: die Adresse kommt
    // aus der oeffentlichen Basisadresse, nicht aus dem Navigationsstamm der Oberflaeche.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_zeigt_der_Download_Verweis_auf_die_WebApi_Adresse()
    {
        var seite = Kartenseite();

        Assert.That(seite, Does.Contain("Anhangadresse.Fuer(Anhangbasis.Adresse, KarteId, anhang.AnhangId)"));
        Assert.That(seite, Does.Not.Contain("href=\"/karten/@KarteId/anhaenge"));
    }

    // Die Obergrenze kommt auch am Dateiwaehler aus der einen Konstante: OpenReadStream bricht
    // ohne Angabe schon bei 512 KB ab.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_oeffnet_sie_den_Strom_mit_der_Anhangsgrenze_und_ohne_zweite_Zahl()
    {
        var seite = Kartenseite();

        Assert.That(seite, Does.Contain("OpenReadStream(Anhangsgrenze.HoechsteDateigroesse)"));
        Assert.That(seite, Does.Not.Contain(Anhangsgrenze.HoechsteDateigroesse.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    // Alle Gestaltungswerte kommen aus dem Token-Sheet: kein Farbliteral in der Komponenten-CSS.
    // Die Groessenwerte der Zeichnung stehen als Zahl da, wie in den uebrigen Abschnitten auch.
    [Test]
    public void Wenn_die_Stilvorlage_des_Anhangabschnitts_gelesen_wird_dann_kommen_ihre_Farben_und_Abstaende_aus_dem_Token_Sheet()
    {
        var stil = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Kartendetail.razor.css"));

        Assert.Multiple(() =>
        {
            Assert.That(stil, Does.Contain(".ablegeflaeche"));
            Assert.That(stil, Does.Contain("background: var(--color-surface)"));
            Assert.That(stil, Does.Contain("color: var(--color-accent)"));
            Assert.That(stil, Does.Contain("gap: var(--space-6)"));
        });
    }
}
