using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Die Gestaltungszusagen des Klassenfeldes und der Nummernplakette, geprüft an der Datei — wie
// die übrigen Gestaltungsprüfungen. Was beide im Browser tun, belegt die E2E-Strecke; was sie
// **nicht** tun, lässt sich dort nicht zeigen: dass die Plakette über dem Titelverweis steht und
// nicht in ihm, dass das Klassenfeld seine Liste über die eigene Route holt statt über ein
// siebtes Glied am Kartendetail, und dass kein Gestaltungswert als Literal danebensteht.
// stil-check: C03 die Ablage ist hier der Prüfgegenstand
[TestFixture]
public class KlassenfeldTests
{
    private static string Kartenseite()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Kartendetail.razor"));
    }

    private static string Kartenseitenstil()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Pages", "Kartendetail.razor.css"));
    }

    private static string Kartenkomponente()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Karten", "Karte.razor"));
    }

    private static string Kartenstil()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Karten", "Karte.razor.css"));
    }

    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_traegt_das_Klassenfeld_seine_Kennungen_und_die_Beschriftung_Klasse()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain(">Klasse</label>"));
            Assert.That(seite, Does.Contain("id=\"klasse-feld\""));
            Assert.That(seite, Does.Contain("id=\"klasse-hinweis\""));
            Assert.That(seite, Does.Contain(">ohne Klasse</option>"));
            Assert.That(seite, Does.Not.Contain(">Kartenklasse</label>"));
        });
    }

    // Wortlaut und Form wie im Klassenbereich des Layout-Modus: ein Satz statt eines leeren
    // Auswahlfeldes.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_zeigt_ein_Board_ohne_Klasse_denselben_Satz_wie_der_Layout_Modus()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("id=\"keine-klassen\""));
            Assert.That(seite, Does.Contain("Dieses Board hat keine Klasse."));
            Assert.That(seite, Does.Contain("_kartenklassen.Count == 0"));
        });
    }

    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_nennt_die_Hinweiszeile_die_naechste_Nummer()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("Vergibt beim Speichern"));
            Assert.That(seite, Does.Contain("Kartennummer.Aus(ErsteKartenklasse.Praefix, ErsteKartenklasse.Zaehlerstand + 1)"));

            // Die Zeile nennt die Klasse, die sie meint — sonst wäre die Nummer auf einem Board
            // mit mehreren Klassen ein Versprechen, das die Wahl daneben nicht hält.
            Assert.That(seite, Does.Contain("die nächste Nummer der Klasse @ErsteKartenklasse.Name"));
        });
    }

    // Die Klassenliste kommt über die Route aus R00022 — das Kartendetail wächst nicht um eine
    // siebte Liste.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_holt_sie_die_Klassenliste_ueber_die_eigene_Route_des_Boards()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("@inject KartenklassenApiKlient KartenklassenKlient"));
            Assert.That(seite, Does.Contain("KartenklassenKlient.LadeKartenklassen(_detail!.Board)"));
        });
    }

    // Der zweite Aufruf hat einen zweiten Fehlerpfad: die Zuordnung geht über dieselbe
    // Ausfallmeldung wie die übrigen Schreibwege der Seite.
    [Test]
    public void Wenn_die_Kartenseite_gelesen_wird_dann_laeuft_die_Zuordnung_ueber_die_Ausfallmeldung()
    {
        var seite = Kartenseite();

        Assert.Multiple(() =>
        {
            Assert.That(seite, Does.Contain("WebApiAufruf.MitAusfallmeldung(() => OrdneKartenklasseZu("));
            Assert.That(seite, Does.Contain("KartenKlient.OrdneKartenklasseZu(KarteId, anfrage)"));
        });
    }

    // Die Plakette steht **über** dem Titelverweis und nicht in ihm: die beiden Ziehproben
    // prüfen genau das, und im Browser wäre der Unterschied erst zu sehen, wenn eine von ihnen
    // fällt.
    [Test]
    public void Wenn_die_Kartenkomponente_gelesen_wird_dann_steht_die_Plakette_vor_dem_Titelverweis_und_nicht_in_ihm()
    {
        var komponente = Kartenkomponente();
        var stelleDerPlakette = komponente.IndexOf("karte-nummer", StringComparison.Ordinal);
        var stelleDesTitelverweises = komponente.IndexOf("class=\"karte-titel\"", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(stelleDerPlakette, Is.GreaterThan(0));
            Assert.That(stelleDerPlakette, Is.LessThan(stelleDesTitelverweises));
            Assert.That(komponente, Does.Contain("class=\"tag tag-neutral karte-nummer\""));
        });
    }

    // Eine Karte ohne Klasse zeigt nur ihren Titel; die Stelle kostet keine Zeile, weil gar
    // nichts gerendert wird.
    [Test]
    public void Wenn_die_Kartenkomponente_gelesen_wird_dann_entfaellt_die_Plakette_ohne_Nummer_ganz()
    {
        Assert.That(Kartenkomponente(), Does.Contain("@if (Kartendaten.Kartennummer is not null)"));
    }

    [Test]
    public void Wenn_die_Stilvorlagen_gelesen_werden_dann_kommen_die_Werte_der_neuen_Stellen_aus_dem_Token_Sheet()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Kartenstil(), Does.Contain(".karte-nummer {\n    align-self: flex-start;\n}"));
            Assert.That(Kartenseitenstil(), Does.Contain("color: var(--color-text);"));
            Assert.That(Kartenseitenstil(), Does.Contain("color-mix(in srgb, var(--color-text) 55%, transparent)"));
        });
    }
}
