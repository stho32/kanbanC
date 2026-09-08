using System.Text.RegularExpressions;
using KanbanC.Blazor.Tests.TestHelpers;

namespace KanbanC.Blazor.Tests.Gestaltung;

// Diese Prüfungen lesen den Quelltextbaum, weil ihr Gegenstand die Ablage selbst ist: dass der
// Export ein **Verweis** ist und kein Knopf, dass er auch an der archivierten Kachel steht und
// dass seine Gestaltungswerte aus dem Token-Sheet kommen.
// stil-check: C03 Dateisystem ist hier der Prüfgegenstand, nicht eine Laufzeitabhängigkeit
[TestFixture]
public class BoardkachelmenueTests
{
    // Ein Knopf könnte die Datei nicht ausliefern, ohne dass die Bytes durch den Blazor-Kreislauf
    // laufen — der Browser holt sie selbst.
    [Test]
    public void Wenn_der_Menuepunkt_Exportieren_gelesen_wird_dann_ist_er_ein_Verweis_auf_die_WebApi_und_kein_Knopf()
    {
        var menuepunkt = Exportzeile();

        Assert.Multiple(() =>
        {
            Assert.That(menuepunkt, Does.StartWith("<a "));
            Assert.That(menuepunkt, Does.Contain("Exportadresse.Fuer(WebApibasis.Adresse, Board.BoardId)"));
            Assert.That(menuepunkt, Does.Not.Contain("<button"));
            Assert.That(menuepunkt, Does.Not.Contain("WebApiAufruf"));
            Assert.That(menuepunkt, Does.Not.Contain("IJSRuntime"));
        });
    }

    // Der Punkt steht **außerhalb** der Verzweigung, die „Archivieren" nur der Standardansicht
    // gibt: ein abgelegtes Board bleibt ausleitbar.
    [Test]
    public void Wenn_die_Kachel_gelesen_wird_dann_steht_der_Menuepunkt_auch_an_der_archivierten_Kachel()
    {
        var kachel = Kachel();

        var vorDemExport = kachel[..kachel.IndexOf("board-kachel-menuepunkt-exportieren", StringComparison.Ordinal)];
        Assert.Multiple(() =>
        {
            Assert.That(kachel, Does.Contain("board-kachel-menuepunkt-exportieren"));
            Assert.That(vorDemExport, Does.Contain("@if (!IstArchivansicht)"));
            Assert.That(Regex.Matches(vorDemExport, @"@if \(!IstArchivansicht\)"), Has.Count.EqualTo(1));
            Assert.That(vorDemExport.LastIndexOf('}'), Is.GreaterThan(vorDemExport.IndexOf("@if (!IstArchivansicht)", StringComparison.Ordinal)), "Der Export steht noch im Zweig der Standardansicht.");
        });
    }

    // Der Trennstrich und die Abstände der neuen Zeile kommen aus dem Token-Sheet, kein Literal.
    [Test]
    public void Wenn_die_Stilvorlage_der_Menuezeile_gelesen_wird_dann_traegt_sie_kein_Farb_Abstands_oder_Radius_Literal()
    {
        var regel = Exportregel();

        Assert.That(regel, Is.Not.Empty, "Ohne Regeln prüfte der Test nichts.");
        Assert.Multiple(() =>
        {
            Assert.That(Regex.Matches(regel, @"#[0-9a-fA-F]{3,8}\b"), Is.Empty, "Farben gehören ins Token-Sheet.");
            Assert.That(Regex.Matches(regel, @"(?:padding|margin|gap|border-radius)[^;]*\d+px"), Is.Empty, "Abstände und Radien kommen aus dem Token-Sheet.");
            Assert.That(regel, Does.Contain("var(--"), "Die Zeile nimmt keinen einzigen Token.");
        });
    }

    private static string Kachel()
    {
        return File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Boards", "Boardkachel.razor")); // stil-check: C03 die Ablage ist der Prüfgegenstand
    }

    private static string Exportzeile()
    {
        var kachel = Kachel();
        var beginn = kachel.IndexOf("<a class=\"board-kachel-menuepunkt board-kachel-menuepunkt-exportieren\"", StringComparison.Ordinal);
        Assert.That(beginn, Is.GreaterThan(-1), "Die Kachel führt keinen Menüpunkt „Exportieren“.");
        var ende = kachel.IndexOf("</a>", beginn, StringComparison.Ordinal);
        return kachel[beginn..ende];
    }

    private static string Exportregel()
    {
        var stilvorlage = File.ReadAllText(Quelltextbaum.BlazorDatei("Components", "Boards", "Boardkachel.razor.css")); // stil-check: C03 die Ablage ist der Prüfgegenstand
        var beginn = stilvorlage.IndexOf(".board-kachel-menuepunkt-exportieren", StringComparison.Ordinal);
        Assert.That(beginn, Is.GreaterThan(-1), "Die Stilvorlage kennt die Menüzeile nicht.");
        var ende = stilvorlage.IndexOf('}', beginn);
        return stilvorlage[beginn..ende];
    }
}
