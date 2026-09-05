using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-1 bis US-3 als Rundlauf über die Oberfläche: anlegen, abhaken, zurückweisen — und nach jedem
// Schritt der Reload, weil „nach Reload da" die Hälfte des Fertig-Kriteriums ist. US-5 steht am
// Ende als Gegenprobe: auf der Bahn ändert sich nichts.
[TestFixture]
public class KarteGliedernE2ETests : PageTest
{
    // US-1: die Handlung statt der Null — kein „0 von 0", kein Balken.
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Karte_keine_Teilaufgabe_traegt_dann_steht_dort_die_Handlung_und_kein_Fortschritt()
    {
        var aufbau = await KarteOhneTeilaufgaben();

        await Expect(aufbau.Seite.TeilaufgabenLeerstand).ToContainTextAsync("Keine Teilaufgaben");
        await Expect(aufbau.Seite.TeilaufgabenLeerstand).ToContainTextAsync("anlegen");
        await Expect(aufbau.Seite.Teilaufgabenstand).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Teilaufgabenbalken).ToHaveCountAsync(0);
    }

    // Der Abschnitt heisst „Teilaufgaben" und nicht „Subtasks" — die benannte Abweichung vom
    // Artboard, hier als Zusicherung festgehalten.
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Kartenseite_offen_ist_dann_traegt_der_Abschnitt_die_deutsche_Ueberschrift()
    {
        var aufbau = await KarteOhneTeilaufgaben();

        await Expect(aufbau.Seite.Teilaufgabenabschnitt).ToContainTextAsync("Teilaufgaben");
        await Expect(aufbau.Seite.Teilaufgabenabschnitt).Not.ToContainTextAsync("Subtask");
    }

    // Das Szenario von US-1: zwei Zeilen in Anlegereihenfolge, Fortschritt „0 von 2", Feld leer,
    // und nach dem Reload steht alles unveraendert da.
    [Test]
    [Category("US-1")]
    public async Task Wenn_zwei_Teilaufgaben_angelegt_werden_dann_stehen_sie_in_Anlegereihenfolge_und_ueberstehen_den_Reload()
    {
        var aufbau = await KarteOhneTeilaufgaben();

        await aufbau.Seite.LegeTeilaufgabeAn("Lizenztext lesen");

        await Expect(aufbau.Seite.Teilaufgaben).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Teilaufgabenstand).ToHaveTextAsync("0 von 1");
        await Expect(aufbau.Seite.Teilaufgabenfeld).ToHaveValueAsync(string.Empty);

        await aufbau.Seite.LegeTeilaufgabeAn("Rückfrage an den Hersteller");

        await Expect(aufbau.Seite.Teilaufgaben).ToHaveTextAsync(new[] { "Lizenztext lesen", "Rückfrage an den Hersteller" });
        await Expect(aufbau.Seite.Teilaufgabenstand).ToHaveTextAsync("0 von 2");

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Teilaufgaben).ToHaveTextAsync(new[] { "Lizenztext lesen", "Rückfrage an den Hersteller" });
        await Expect(aufbau.Seite.Teilaufgabenstand).ToHaveTextAsync("0 von 2");
    }

    // Das letzte Szenario von US-1: zwei gleich benannte Arbeiten sind zwei Arbeiten.
    [Test]
    [Category("US-1")]
    public async Task Wenn_derselbe_Text_zweimal_angelegt_wird_dann_stehen_zwei_Zeilen_in_der_Liste()
    {
        var aufbau = await KarteOhneTeilaufgaben();

        await aufbau.Seite.LegeTeilaufgabeAn("Nachfassen");
        await Expect(aufbau.Seite.Teilaufgaben).ToHaveCountAsync(1);
        await aufbau.Seite.LegeTeilaufgabeAn("Nachfassen");

        await Expect(aufbau.Seite.Teilaufgaben).ToHaveTextAsync(new[] { "Nachfassen", "Nachfassen" });
    }

    // Nach dem Anlegen bleibt das Feld leer und unter dem Cursor: die zweite Zeile wird getippt,
    // ohne das Feld erneut anzuklicken. Das ist die entschiedene Antwort auf die offene Frage,
    // ob die Eingabezeile stehen bleibt.
    [Test]
    [Category("US-1")]
    public async Task Wenn_zwei_Teilaufgaben_nacheinander_mit_der_Eingabetaste_angelegt_werden_dann_traegt_die_Karte_beide()
    {
        var aufbau = await KarteOhneTeilaufgaben();

        await aufbau.Seite.TippeTeilaufgabe("Lizenztext lesen");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(aufbau.Seite.Teilaufgaben).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Teilaufgabenfeld).ToHaveValueAsync(string.Empty);
        await Expect(aufbau.Seite.Teilaufgabenfeld).ToBeFocusedAsync();

        await Page.Keyboard.TypeAsync("Rückfrage an den Hersteller");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(aufbau.Seite.Teilaufgaben).ToHaveTextAsync(new[] { "Lizenztext lesen", "Rückfrage an den Hersteller" });
    }

    // Auch nach dem Klick auf „+" steht der Cursor wieder im Feld — beide Wege verhalten sich
    // gleich, obwohl der Klick den Fokus zunaechst mitnimmt.
    [Test]
    [Category("US-1")]
    public async Task Wenn_ueber_den_Plusknopf_angelegt_wird_dann_steht_der_Cursor_danach_wieder_im_Feld()
    {
        var aufbau = await KarteOhneTeilaufgaben();

        await aufbau.Seite.LegeTeilaufgabeAn("Lizenztext lesen");

        await Expect(aufbau.Seite.Teilaufgaben).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Teilaufgabenfeld).ToBeFocusedAsync();
    }

    // Das Szenario von US-2 in einem Zug: B abhaken, D dazu, Reload, B zuruecknehmen, alle vier.
    [Test]
    [Category("US-2")]
    public async Task Wenn_einzelne_Teilaufgaben_abgehakt_werden_dann_zieht_der_Fortschritt_mit_und_die_uebrigen_bleiben_offen()
    {
        var aufbau = await KarteMitVierTeilaufgaben();

        await Expect(aufbau.Seite.Teilaufgabenstand).ToHaveTextAsync("0 von 4");
        await Expect(aufbau.Seite.Teilaufgabenbalken).ToHaveAttributeAsync("aria-valuenow", "0");

        await aufbau.Seite.Teilaufgabenkaestchen("B").ClickAsync();

        await Expect(aufbau.Seite.Teilaufgabenstand).ToHaveTextAsync("1 von 4");
        await Expect(aufbau.Seite.Teilaufgabenbalken).ToHaveAttributeAsync("aria-valuenow", "25");
        await Expect(aufbau.Seite.AbgehakteTeilaufgaben).ToHaveTextAsync(new[] { "B" });
        await Expect(aufbau.Seite.Teilaufgaben).ToHaveTextAsync(new[] { "A", "B", "C", "D" });

        await aufbau.Seite.Teilaufgabenkaestchen("D").ClickAsync();

        await Expect(aufbau.Seite.Teilaufgabenstand).ToHaveTextAsync("2 von 4");
        await Expect(aufbau.Seite.Teilaufgabenbalken).ToHaveAttributeAsync("aria-valuenow", "50");

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.AbgehakteTeilaufgaben).ToHaveTextAsync(new[] { "B", "D" });

        await aufbau.Seite.Teilaufgabenkaestchen("B").ClickAsync();

        await Expect(aufbau.Seite.Teilaufgabenstand).ToHaveTextAsync("1 von 4");
        await Expect(aufbau.Seite.AbgehakteTeilaufgaben).ToHaveTextAsync(new[] { "D" });

        await aufbau.Seite.Teilaufgabenkaestchen("A").ClickAsync();
        await aufbau.Seite.Teilaufgabenkaestchen("B").ClickAsync();
        await aufbau.Seite.Teilaufgabenkaestchen("C").ClickAsync();

        await Expect(aufbau.Seite.Teilaufgabenstand).ToHaveTextAsync("4 von 4");
        await Expect(aufbau.Seite.Teilaufgabenbalken).ToHaveAttributeAsync("aria-valuenow", "100");
    }

    // Das Akzeptanzkriterium woertlich: geprueft wird der berechnete Stil und nicht die Klasse,
    // die ihn setzt — eine Klasse ohne Wirkung waere gruen, ohne dass etwas durchgestrichen ist.
    [Test]
    [Category("US-2")]
    public async Task Wenn_eine_Zeile_abgehakt_ist_dann_ist_ihr_Text_durchgestrichen_und_der_der_offenen_nicht()
    {
        var aufbau = await KarteMitVierTeilaufgaben();

        await aufbau.Seite.Teilaufgabenkaestchen("B").ClickAsync();
        await Expect(aufbau.Seite.AbgehakteTeilaufgaben).ToHaveCountAsync(1);

        var abgehakte = await Durchstreichung(aufbau.Seite.Teilaufgabe("B"));
        var offene = await Durchstreichung(aufbau.Seite.Teilaufgabe("A"));

        Assert.Multiple(() =>
        {
            Assert.That(abgehakte, Does.Contain("line-through"));
            Assert.That(offene, Does.Not.Contain("line-through"));
        });
    }

    // Der Abschnitt steht hinter „Beschreibung" in der linken Spalte, wie im Artboard.
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Kartenseite_offen_ist_dann_steht_der_Abschnitt_hinter_der_Beschreibung()
    {
        var aufbau = await KarteOhneTeilaufgaben();

        // AllTextContentsAsync und nicht AllInnerTextsAsync: geprueft wird die Reihenfolge im
        // Dokument, nicht die Schreibweise — die Ueberschriften stehen im Markup gemischt und
        // erscheinen nur durch text-transform in Grossbuchstaben.
        var abschnitte = await Page.Locator(".karteninhalt .blattabschnitt .blattueberschrift").AllTextContentsAsync();

        Assert.That(abschnitte, Is.EqualTo(new[] { "Beschreibung", "Teilaufgaben" }));
    }

    private async Task<string> Durchstreichung(ILocator zeile)
    {
        return await zeile.Locator(".teilaufgabentext")
            .EvaluateAsync<string>("element => getComputedStyle(element).textDecorationLine");
    }

    // Die entschiedene Antwort auf die zweite offene Frage: das Kaestchen ist ein Knopf und laesst
    // sich deshalb ohne Zeiger erreichen und schalten.
    [Test]
    [Category("US-2")]
    public async Task Wenn_das_Kaestchen_ueber_die_Tastatur_geschaltet_wird_dann_haakt_es_dieselbe_Zeile_ab()
    {
        var aufbau = await KarteMitVierTeilaufgaben();

        await aufbau.Seite.Teilaufgabenkaestchen("C").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(aufbau.Seite.AbgehakteTeilaufgaben).ToHaveTextAsync(new[] { "C" });
        await Expect(aufbau.Seite.Teilaufgabenstand).ToHaveTextAsync("1 von 4");
    }

    // US-3: eine leere Zeile bringt eine lesbare Meldung und legt nichts an.
    [Test]
    [Category("US-3")]
    public async Task Wenn_das_Feld_leer_bleibt_dann_erscheint_eine_lesbare_Meldung_und_die_Liste_bleibt_wie_sie_war()
    {
        var aufbau = await KarteMitZweiTeilaufgaben();

        await aufbau.Seite.TeilaufgabeHinzufuegen.ClickAsync();

        await Expect(aufbau.Seite.BlattZurueckweisung).ToContainTextAsync("darf nicht leer sein");
        await Expect(aufbau.Seite.Teilaufgaben).ToHaveCountAsync(2);
    }

    [Test]
    [Category("US-3")]
    public async Task Wenn_nur_Leerzeichen_eingegeben_werden_dann_erscheint_dieselbe_Meldung_und_die_Liste_bleibt_bei_zwei_Zeilen()
    {
        var aufbau = await KarteMitZweiTeilaufgaben();

        await aufbau.Seite.TippeTeilaufgabe("   ");
        await aufbau.Seite.TeilaufgabeHinzufuegen.ClickAsync();

        await Expect(aufbau.Seite.BlattZurueckweisung).ToContainTextAsync("darf nicht leer sein");
        await Expect(aufbau.Seite.Teilaufgaben).ToHaveCountAsync(2);
    }

    // Das Rechenbeispiel von US-3: die Randleerzeichen fallen weg, der Text im Uebrigen nicht —
    // und von den zurueckgewiesenen Eingaben ist nach dem Reload nichts geblieben.
    [Test]
    [Category("US-3")]
    public async Task Wenn_ein_Text_mit_Randleerzeichen_angelegt_wird_dann_steht_er_ohne_sie_in_der_Liste()
    {
        var aufbau = await KarteMitZweiTeilaufgaben();
        await aufbau.Seite.TeilaufgabeHinzufuegen.ClickAsync();
        await Expect(aufbau.Seite.BlattZurueckweisung).ToBeVisibleAsync();

        await aufbau.Seite.LegeTeilaufgabeAn("  Kaffee holen  ");

        await Expect(aufbau.Seite.Teilaufgaben).ToHaveTextAsync(new[] { "A", "B", "Kaffee holen" });

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Teilaufgaben).ToHaveTextAsync(new[] { "A", "B", "Kaffee holen" });
    }

    // US-5 als Gegenprobe: die Gliederung aendert an der Kartenform auf der Bahn nichts — kein
    // Zaehler „2/4", und die Kartenzahl im Bahnenkopf zaehlt unveraendert richtig.
    [Test]
    [Category("US-5")]
    public async Task Wenn_eine_Karte_Teilaufgaben_traegt_dann_zeigt_die_Bahn_dieselbe_Kartenform_wie_zuvor()
    {
        var aufbau = await KarteMitVierTeilaufgaben();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var detail = await webApi.LegeTeilaufgabeAn(aufbau.KarteId, "Fuenfter Schritt");
        await webApi.SetzeAbhakung(aufbau.KarteId, detail.Teilaufgaben[0].TeilaufgabeId, abgehakt: true);

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).ToBeVisibleAsync();
        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).Not.ToContainTextAsync("/5");
        await Expect(board.KarteMitTitel("Playwright-Lizenz klären")).Not.ToContainTextAsync("Fuenfter Schritt");
        await Expect(board.Karten).ToHaveCountAsync(1);
    }

    private async Task<Aufbau> KarteOhneTeilaufgaben()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        return new Aufbau(seite, board.BoardId, karte.KarteId);
    }

    private async Task<Aufbau> KarteMitZweiTeilaufgaben()
    {
        return await KarteMitTeilaufgaben("A", "B");
    }

    private async Task<Aufbau> KarteMitVierTeilaufgaben()
    {
        return await KarteMitTeilaufgaben("A", "B", "C", "D");
    }

    // Angelegt wird ueber die API und nicht ueber die Oberflaeche: das Arrange soll den Zustand
    // herstellen und nicht schon den Weg pruefen, um den es im Test geht.
    private async Task<Aufbau> KarteMitTeilaufgaben(params string[] texte)
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Playwright-Lizenz klären");
        foreach (var text in texte)
        {
            await webApi.LegeTeilaufgabeAn(karte.KarteId, text);
        }

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        await Expect(seite.Teilaufgaben).ToHaveCountAsync(texte.Length);
        return new Aufbau(seite, board.BoardId, karte.KarteId);
    }

    private sealed record Aufbau(KartendetailSeite Seite, long BoardId, long KarteId);
}
