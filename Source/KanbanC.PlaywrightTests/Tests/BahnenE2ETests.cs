using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

[TestFixture]
[Category("US-6")]
public class BahnenE2ETests : PageTest
{
    // Das Häkchen, an dem die Abschlussspalte in der Bahn erkennbar ist.
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
        await Expect(seite.Abschlussvermerke).ToHaveTextAsync(["Grenze 20"]);
        await Expect(seite.SpaltenbahnAnStelle(2)).ToContainTextAsync("Erledigt");
    }

    // Nachgezogen aus R00005: die Kartenzahlstelle bleibt leer, bis I0004 sie füllt; die
    // Kartenstelle im Bahnenfuß trägt seit R00006 das Bedienelement zum Anlegen.
    [Test]
    public async Task Wenn_ein_Board_geoeffnet_wird_dann_bleibt_die_Kartenzahlstelle_leer_und_im_Bahnenfuss_steht_das_Anlegen()
    {
        var seite = await BoardMitStandardspalten();

        await Expect(seite.Kartenzahlstellen).ToHaveCountAsync(3);
        await Expect(seite.Kartenstellen).ToHaveCountAsync(3);
        await Expect(seite.Kartenzahlstellen.Nth(0)).ToBeEmptyAsync();
        await Expect(seite.KarteAnlegenKnoepfe).ToHaveCountAsync(3);
        await Expect(seite.KarteAnlegenKnoepfe.Nth(0)).ToBeVisibleAsync();
    }

    [Test]
    // Die Kopfdaten sind in die Navigationszeile gewandert: ueber dem ersten Bahnenkopf
    // steht seither ueberhaupt nichts mehr, das Board beginnt unmittelbar unter dem Rahmen.
    public async Task Wenn_ueber_den_ersten_Bahnenkopf_geschaut_wird_dann_steht_darueber_nichts_ausser_der_Navigationszeile()
    {
        var seite = await BoardMitStandardspalten();

        var bahnenStehenGanzOben = await Page.EvaluateAsync<bool>(
            """
            () => {
                const bahnen = document.getElementById('spaltenbahnen');
                const inhalt = bahnen.closest('.inhalt') ?? bahnen.parentElement;
                const erstesElement = inhalt.firstElementChild;
                return erstesElement.contains(bahnen);
            }
            """);

        var kopfdatenInDerNavigationszeile = await Page.EvaluateAsync<bool>(
            """
            () => document.getElementById('kopfzeile').contains(document.getElementById('board-name'))
            """);

        Assert.That(bahnenStehenGanzOben, Is.True, "Ueber den Bahnen steht noch ein Element.");
        Assert.That(kopfdatenInDerNavigationszeile, Is.True, "Der Boardname sitzt nicht in der Navigationszeile.");
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
        // Is.Ordered.Ascending besteht auch auf lauter gleichen Werten — ein senkrechter Stapel
        // bliebe damit gruen. Geprueft wird deshalb, dass die Bahnen ueberhaupt verschiedene
        // Stellen haben und im Layout-Modus dieselben behalten.
        Assert.That(stellenInDerArbeitsansicht, Is.Unique, "Die Bahnen stehen schon in der Arbeitsansicht uebereinander; der Test misst nichts.");
        Assert.That(stellenImLayoutModus, Has.Count.EqualTo(stellenInDerArbeitsansicht.Count));
        Assert.That(stellenImLayoutModus, Is.EqualTo(stellenInDerArbeitsansicht).Within(1.0), "Die Bahnen stehen im Layout-Modus nicht mehr an denselben Stellen.");
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
