using KanbanC.Blazor.Services;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Services;

// Was die Pfadkopie **nicht** tut, lässt sich im Browser nicht zeigen: dass sie ohne eigene
// `.js`-Datei auskommt. Der E2E-Lauf belegt, dass der Pfad in der Zwischenablage landet und dass
// der Rückfall im unsicheren Kontext trägt — er sähe aber nicht, ob dafür unterwegs ein Modul
// geladen wird. Deshalb steht diese Zusage hier, an der Datei.
// stil-check: C03 die Ablage ist hier der Prüfgegenstand
public class PfadkopieTests
{
    private static string Quelltext()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Services", "Pfadkopie.cs"));
    }

    // Die Anwendung liefert überhaupt kein eigenes JavaScript aus. Eine eigene Datei brächte
    // einen zweiten Auslieferungsweg und eine Version, die mit dem C#-Code auseinanderlaufen kann
    // — genau das, was der Identitaetsspeicher seit R00013 vermeidet.
    [Test]
    public void Wenn_das_Auslieferungsverzeichnis_durchgesehen_wird_dann_liegt_darin_keine_eigene_JavaScript_Datei()
    {
        var eigeneSkripte = Directory.GetFiles(Quelltextbaum.BlazorDatei("wwwroot"), "*.js", SearchOption.AllDirectories);

        Assert.That(eigeneSkripte, Is.Empty);
    }

    // Und die Pfadkopie lädt auch keines nach: kein Modulimport, kein Dateiname, keine
    // IJSObjectReference auf ein Modul. Die beiden Objektverweise, die sie hält, kommen aus
    // Browser-Standardbefehlen (`document.getElementById`, `getSelection`) und nicht aus `import`.
    [Test]
    public void Wenn_die_Pfadkopie_gelesen_wird_dann_laedt_sie_kein_JavaScript_Modul_nach()
    {
        var quelltext = Quelltext();

        Assert.Multiple(() =>
        {
            Assert.That(quelltext, Does.Not.Contain("\"import\""));
            Assert.That(quelltext, Does.Not.Match("\"[^\"]*\\.js\""), "Kein Zeichenkettenliteral, das auf eine .js-Datei zeigt.");
            Assert.That(quelltext, Does.Not.Contain("InvokeAsync<IJSObjectReference>(\"import"));
        });
    }

    // Die Bezeichner stehen als Konstanten im Quelltext und sind Browser-Standardbefehle —
    // dieselbe Bauform wie im Identitaetsspeicher.
    [Test]
    public void Wenn_die_Pfadkopie_gelesen_wird_dann_ruft_sie_nur_Browser_Standardbefehle_ueber_Inline_Bezeichner()
    {
        var quelltext = Quelltext();

        Assert.Multiple(() =>
        {
            Assert.That(quelltext, Does.Contain("\"navigator.clipboard.writeText\""));
            Assert.That(quelltext, Does.Contain("\"document.getElementById\""));
            Assert.That(quelltext, Does.Contain("\"getSelection\""));
            Assert.That(quelltext, Does.Contain("\"removeAllRanges\""));
            Assert.That(quelltext, Does.Contain("\"selectAllChildren\""));
        });
    }

    // Der Ausfall wird gefangen wie im Identitaetsspeicher — die Kartenseite darf an einem
    // fehlenden Browserbefehl nicht reißen, und das ist im LAN über `http://` der Normalfall.
    // Ohne diese Zusicherung bliebe unbemerkt, wenn jemand einen der drei Ausnahmetypen
    // herausnimmt.
    [Test]
    public void Wenn_die_Pfadkopie_gelesen_wird_dann_faengt_sie_dieselben_drei_Browserausfaelle_wie_der_Identitaetsspeicher()
    {
        var quelltext = Quelltext();

        Assert.Multiple(() =>
        {
            Assert.That(quelltext, Does.Contain("JSException"));
            Assert.That(quelltext, Does.Contain("JSDisconnectedException"));
            Assert.That(quelltext, Does.Contain("InvalidOperationException"));
        });
    }

    // **Der Rückfall ist der Normalfall und keine Zugabe**: fehlt `navigator.clipboard`, wird der
    // Text in seiner Zeile markiert — eine Ausnahme wäre an dieser Stelle eine Ausnahmeseite.
    [Test]
    public async Task Wenn_die_Zwischenablage_fehlt_dann_wird_der_Text_markiert_statt_geworfen()
    {
        var browser = new TestBrowser("navigator.clipboard.writeText");
        var kopie = new Pfadkopie(browser);

        var ergebnis = await kopie.Kopiere("31 Karten angelegt", "import-bericht");

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis, Is.EqualTo(Kopierergebnis.AlsTextMarkiert));
            Assert.That(browser.AufgerufeneBefehle, Does.Contain("selectAllChildren"));
        });
    }

    // Fällt auch der Rückfall aus, bleibt der Text sichtbar stehen und lässt sich von Hand
    // markieren — die Seite bleibt in jeder anderen Hinsicht bedienbar.
    [Test]
    public async Task Wenn_auch_der_Rueckfall_ausfaellt_dann_kommt_das_dritte_Ergebnis_statt_einer_Ausnahme()
    {
        var browser = new TestBrowser("navigator.clipboard.writeText", "document.getElementById");
        var kopie = new Pfadkopie(browser);

        var ergebnis = await kopie.Kopiere("31 Karten angelegt", "import-bericht");

        Assert.That(ergebnis, Is.EqualTo(Kopierergebnis.Gescheitert));
    }

    // Was in die Zwischenablage geht, ist der übergebene Text — nicht der Elementbezeichner.
    [Test]
    public async Task Wenn_die_Zwischenablage_traegt_dann_bekommt_sie_den_uebergebenen_Text()
    {
        var browser = new TestBrowser();
        var kopie = new Pfadkopie(browser);

        var ergebnis = await kopie.Kopiere("31 Karten angelegt", "import-bericht");

        Assert.Multiple(() =>
        {
            Assert.That(ergebnis, Is.EqualTo(Kopierergebnis.InDerZwischenablage));
            Assert.That(browser.ZuletztUebergebeneWerte, Is.EqualTo(new object?[] { "31 Karten angelegt" }));
        });
    }

    // Drei Ausgänge, und der mittlere ist der, für den der Typ überhaupt da ist: „nicht kopiert"
    // ist im unsicheren Kontext kein Fehler, sondern die zweitbeste Zusage.
    [Test]
    public void Wenn_das_Kopierergebnis_durchgesehen_wird_dann_traegt_es_genau_drei_Ausgaenge()
    {
        Assert.That(Enum.GetValues<Kopierergebnis>(), Is.EqualTo(new[]
        {
            Kopierergebnis.InDerZwischenablage,
            Kopierergebnis.AlsTextMarkiert,
            Kopierergebnis.Gescheitert,
        }));
    }
}
