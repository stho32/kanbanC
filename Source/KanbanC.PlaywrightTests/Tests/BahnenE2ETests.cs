using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
[Category("US-6")]
public class BahnenE2ETests : PageTest
{
    // Das Haeckchen, an dem die Abschlussspalte in der Bahn erkennbar ist.
    private const string Haeckchen = "\u2713";

    [Test]
    public async Task Wenn_ein_Board_geoeffnet_wird_dann_traegt_jede_Bahn_eine_Kopfzeile_mit_ihrer_Bezeichnung()
    {
        var seite = await BoardMitStandardspalten();

        await Expect(seite.Bahnenkoepfe).ToHaveCountAsync(3);
        await Expect(seite.Spaltenbezeichnungen).ToHaveTextAsync(["Zu erledigen", "In Arbeit", "Erledigt"]);
    }

    [Test]
    public async Task Wenn_eine_Bahn_die_Abschlussspalte_ist_dann_traegt_sie_ein_Haeckchen_und_nennt_ihre_Anzeigegrenze()
    {
        var seite = await BoardMitStandardspalten();

        await Expect(seite.Abschlusshaken).ToHaveCountAsync(1);
        await Expect(seite.Abschlusshaken).ToHaveTextAsync([Haeckchen]);
        await Expect(seite.Abschlussvermerke).ToHaveTextAsync(["Abschlussspalte, Anzeigegrenze 20"]);
        await Expect(seite.SpaltenbahnAnStelle(2)).ToContainTextAsync("Erledigt");
    }

    [Test]
    public async Task Wenn_ein_Board_geoeffnet_wird_dann_stehen_die_Stellen_fuer_Kartenzahl_und_neue_Karte_leer_bereit()
    {
        var seite = await BoardMitStandardspalten();

        await Expect(seite.Kartenzahlstellen).ToHaveCountAsync(3);
        await Expect(seite.Kartenstellen).ToHaveCountAsync(3);
        await Expect(seite.Kartenzahlstellen.Nth(0)).ToBeEmptyAsync();
        await Expect(seite.Kartenstellen.Nth(0)).ToBeEmptyAsync();
    }

    [Test]
    public async Task Wenn_ueber_den_ersten_Bahnenkopf_geschaut_wird_dann_steht_darueber_nur_die_Board_Kopfzeile()
    {
        var seite = await BoardMitStandardspalten();

        var bahnenFolgenDirektAufDieKopfzeile = await Page.EvaluateAsync<bool>(
            """
            () => {
                const kopf = document.getElementById('board-kopf');
                const naechstes = kopf.nextElementSibling;
                return naechstes !== null && naechstes.id === 'spaltenbahnen';
            }
            """);

        Assert.That(bahnenFolgenDirektAufDieKopfzeile, Is.True);
    }

    [Test]
    public async Task Wenn_die_Bahnen_breiter_sind_als_das_Fenster_dann_scrollen_sie_waagerecht_und_die_Seite_nicht()
    {
        await Page.SetViewportSizeAsync(500, 800);
        var seite = await BoardMitStandardspalten();

        var bahnenSindBreiterAlsIhrKasten = await Page.EvaluateAsync<bool>(
            "() => { const bahnen = document.getElementById('spaltenbahnen'); return bahnen.scrollWidth > bahnen.clientWidth; }");
        await Page.EvaluateAsync("() => { document.getElementById('spaltenbahnen').scrollLeft = 200; }");
        var scrollstandDerBahnen = await Page.EvaluateAsync<int>(
            "() => document.getElementById('spaltenbahnen').scrollLeft");
        var seiteScrolltWaagerecht = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth");

        Assert.That(bahnenSindBreiterAlsIhrKasten, Is.True, "Die Bahnen passen ins Fenster; der Test misst nichts.");
        Assert.That(scrollstandDerBahnen, Is.GreaterThan(0), "Die Bahnen liessen sich nicht waagerecht scrollen.");
        Assert.That(seiteScrolltWaagerecht, Is.False, "Die Seite selbst scrollt waagerecht.");
    }

    [Test]
    public async Task Wenn_der_Layout_Modus_betreten_wird_dann_stehen_die_Bahnen_in_derselben_Anordnung_nebeneinander()
    {
        var seite = await BoardMitStandardspalten();
        var stellenInDerArbeitsansicht = await LinkeKantenDerBahnen();

        await seite.BetreteLayoutModus();

        var stellenImLayoutModus = await LinkeKantenDerBahnen();
        Assert.That(stellenImLayoutModus, Has.Count.EqualTo(stellenInDerArbeitsansicht.Count));
        Assert.That(stellenImLayoutModus, Is.Ordered.Ascending, "Die Bahnen stehen im Layout-Modus nicht mehr nebeneinander.");
        Assert.That(stellenInDerArbeitsansicht, Is.Ordered.Ascending);
    }

    private async Task<IReadOnlyList<double>> LinkeKantenDerBahnen()
    {
        return await Page.EvaluateAsync<double[]>(
            """
            () => Array.from(document.querySelectorAll('#spaltenbahnen .spaltenbahn'))
                       .map(bahn => bahn.getBoundingClientRect().left)
            """);
    }

    private async Task<BoardSeite> BoardMitStandardspalten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("KanbanC — Release 2", "Projekt", "2026-01-05", "2026-09-30");
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        var seite = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(1);
        await Expect(seite.Spaltenbahnen).ToHaveCountAsync(3);
        return seite;
    }
}
