using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-6, US-7 und US-8: die drei Fassungen der Marke aus dem Artboard und der Fall, den ein Test am
// leichtesten übersieht — **die eigene Bewegung bekommt keine**. Ohne ihn machte jeder Klick eine
// Benachrichtigung über sich selbst.
[TestFixture]
public class EinflugmarkeE2ETests : PageTest
{
    // US-6, Fassung C: ein Agent über die API wird mit Weg genannt.
    [Test]
    [Category("US-6")]
    public async Task Wenn_ein_Agent_ueber_die_API_bewegt_dann_nennt_die_Marke_ihn_und_den_Weg()
    {
        var aufbau = await BoardMitDreiKarten();
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);

        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.InArbeitId, 1, aufbau.Claude.KontributorId));

        var bewegte = zusehendes.KarteMitTitel("C");
        await Expect(zusehendes.EinflugmarkeDerKarte(bewegte)).ToContainTextAsync("Claude-Agent · über die API ·");
        await Expect(zusehendes.EingefloreneKarten).ToHaveCountAsync(1);
    }

    // US-6, Fassung B: ein Mensch an der Oberfläche wird **ohne** Weg genannt — die Oberfläche ist
    // der Normalfall und braucht keine Nennung.
    [Test]
    [Category("US-6")]
    public async Task Wenn_ein_Mensch_in_einem_anderen_Browser_bewegt_dann_nennt_die_Marke_seinen_Namen_ohne_Weg()
    {
        var aufbau = await BoardMitDreiKarten();
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        await using var zweiterKontext = await Browser.NewContextAsync();
        var handelndeSeite = await zweiterKontext.NewPageAsync();
        var handelndes = new BoardSeite(handelndeSeite, Testumgebung.Aktuelle.BlazorAdresse);
        await handelndes.Oeffne(aufbau.BoardId);
        await WaehleIdentitaet(handelndeSeite, handelndes, aufbau.Nina);

        await handelndes.ZieheKarteAufsBahnende(handelndes.KarteMitTitel("C"), handelndes.Spaltenbahn(aufbau.InArbeitId));

        var bewegte = zusehendes.KarteMitTitel("C");
        await Expect(zusehendes.EinflugmarkeDerKarte(bewegte)).ToContainTextAsync("Nina Barth ·");
        await Expect(zusehendes.EinflugmarkeDerKarte(bewegte)).Not.ToContainTextAsync("über die API");
    }

    // US-7: die eigene Bewegung bekommt **keine** Marke — und derselbe Zug trägt im zweiten
    // Browser eine. Erst zusammen ist das eine Aussage: der zweite Browser belegt, dass das
    // Ereignis wirklich durch den Kanal kam, während der erste schweigt.
    [Test]
    [Category("US-7")]
    public async Task Wenn_ich_selbst_ziehe_dann_traegt_meine_Karte_keine_Marke_und_die_des_anderen_Browsers_eine()
    {
        var aufbau = await BoardMitDreiKarten();
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        await using var zweiterKontext = await Browser.NewContextAsync();
        var handelndeSeite = await zweiterKontext.NewPageAsync();
        var handelndes = new BoardSeite(handelndeSeite, Testumgebung.Aktuelle.BlazorAdresse);
        await handelndes.Oeffne(aufbau.BoardId);
        await WaehleIdentitaet(handelndeSeite, handelndes, aufbau.Nina);

        await handelndes.ZieheKarteAufsBahnende(handelndes.KarteMitTitel("C"), handelndes.Spaltenbahn(aufbau.InArbeitId));

        await Expect(zusehendes.Einflugmarken).ToHaveCountAsync(1);
        await Expect(handelndes.Einflugmarken).ToHaveCountAsync(0);
        await Expect(handelndes.EingefloreneKarten).ToHaveCountAsync(0);
    }

    // US-8: jede Marke verschwindet von selbst. Geprüft wird **dass** sie vergeht, nicht ihre
    // Standzeit auf die Sekunde — sie ist eine Größenordnung und für den Lauf gekürzt.
    [Test]
    [Category("US-8")]
    public async Task Wenn_eine_Marke_steht_dann_verschwindet_sie_nach_einer_Weile_von_selbst()
    {
        var aufbau = await BoardMitDreiKarten();
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.InArbeitId, 1, aufbau.Claude.KontributorId));
        await Expect(zusehendes.Einflugmarken).ToHaveCountAsync(1);

        await Expect(zusehendes.Einflugmarken).ToHaveCountAsync(0);

        // Die Karte steht danach wieder ruhig: keine Kante, keine Fußzeile — und sie liegt weiter
        // an ihrer neuen Stelle.
        await Expect(zusehendes.EingefloreneKarten).ToHaveCountAsync(0);
        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.InArbeitId))).ToHaveTextAsync(["C"]);
    }

    // US-6, letzter Teil: ein unbekannter oder fehlender Urheber ergibt eine Marke ohne Namen und
    // keinen Absturz.
    [Test]
    [Category("US-6")]
    public async Task Wenn_die_Bewegung_keinen_Urheber_nennt_dann_traegt_die_Marke_nur_Weg_und_Zeitpunkt()
    {
        var aufbau = await BoardMitDreiKarten();
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);

        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.InArbeitId, 1));

        await Expect(zusehendes.Einflugmarken).ToHaveTextAsync("über die API · gerade eben");
        await Expect(zusehendes.Ausnahmeanzeige).Not.ToBeVisibleAsync();
    }

    // Gewählt wird über die Kopfzeile, den Weg des Menschen: nur so trägt der Zug die Identität
    // dieses Kreislaufs im Rumpf. Danach wird die Seite neu geladen, weil das Board seine Wahl beim
    // Laden liest — dieselbe Regel wie seit R00013 („gilt ab dem nächsten Abruf").
    private static async Task WaehleIdentitaet(IPage seite, BoardSeite board, Kontributor kontributor)
    {
        var rahmen = new Rahmen(seite);
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(kontributor.KontributorId).ClickAsync();
        await Assertions.Expect(rahmen.Identitaetsplatz).ToHaveTextAsync(kontributor.Name);
        await board.LadeNeu();
        await board.ErwarteGeoeffnet();
    }

    private static async Task<Aufbau> BoardMitDreiKarten()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var bereitId = board.Spalten[0].SpalteId;
        await webApi.LegeKarteAn(board.BoardId, bereitId, "A");
        await webApi.LegeKarteAn(board.BoardId, bereitId, "B");
        var karteC = await webApi.LegeKarteAn(board.BoardId, bereitId, "C");
        var claude = await webApi.LegeKontributorAn("Claude-Agent", Kontributorart.Agent);
        var nina = await webApi.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);
        return new Aufbau(board.BoardId, bereitId, board.Spalten[1].SpalteId, karteC.KarteId, claude, nina);
    }

    private sealed record Aufbau(long BoardId, long BereitId, long InArbeitId, long KarteC, Kontributor Claude, Kontributor Nina);
}
