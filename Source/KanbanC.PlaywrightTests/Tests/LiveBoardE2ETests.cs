using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-1 bis US-5 und US-13 über die ganze Strecke: zwei Prozesse, zwei Browserkontexte und der Weg
// des Agenten ohne Browser. **Zwei Kontexte zugleich sind neu in diesem Projekt** — bisher hat kein
// Test zwei Sichten gehalten; die Probe dafür steht in ZweiterBrowserkontextProbeE2ETests.
// Gewartet wird nie eine feste Pause: jede Erwartung ist ein Expect mit Zeitschranke.
[TestFixture]
public class LiveBoardE2ETests : PageTest
{
    // Ein Neustart der WebApi kostet die Ereignisleitung ihre Wiederaufnahmepause; die Zeitschranke
    // liegt deshalb über der Vorgabe von fünf Sekunden.
    private const int SchrankeNachNeustart = 20000;

    // US-1: was ein anderer Browser bewegt, steht ohne Zutun an seiner neuen Stelle.
    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_zweiter_Browser_eine_Karte_bewegt_dann_zieht_das_offene_Board_ohne_Zutun_nach()
    {
        var aufbau = await BoardMitDreiKarten();
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.BereitId))).ToHaveTextAsync(["A", "B", "C"]);
        await using var zweiterKontext = await Browser.NewContextAsync();
        var handelndes = new BoardSeite(await zweiterKontext.NewPageAsync(), Testumgebung.Aktuelle.BlazorAdresse);
        await handelndes.Oeffne(aufbau.BoardId);

        await handelndes.ZieheKarteAufsBahnende(handelndes.KarteMitTitel("C"), handelndes.Spaltenbahn(aufbau.InArbeitId));

        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.InArbeitId))).ToHaveTextAsync(["C"]);
        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.BereitId))).ToHaveTextAsync(["A", "B"]);
        // Kein Platzhalter an der alten Stelle: die Bahn zeigt genau zwei Karten und sonst nichts.
        await Expect(zusehendes.KartenDerBahn(zusehendes.Spaltenbahn(aufbau.BereitId))).ToHaveCountAsync(2);
    }

    // US-2: derselbe Fall über die API, ganz ohne zweiten Browser. Erst beide Fälle zusammen
    // tragen das Fertig-Kriterium „ein Browser **oder die API**".
    [Test]
    [Category("US-2")]
    public async Task Wenn_die_API_ohne_Browser_eine_Karte_bewegt_dann_zieht_das_offene_Board_ohne_Zutun_nach()
    {
        var aufbau = await BoardMitDreiKarten();
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.InArbeitId))).ToHaveCountAsync(0);

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.InArbeitId, 1, aufbau.Claude.KontributorId));

        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.InArbeitId))).ToHaveTextAsync(["C"]);
        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.BereitId))).ToHaveTextAsync(["A", "B"]);
    }

    // US-3: die Karte steht an **der** Stelle, an die der Urheber sie gelegt hat. Rechenbeispiel
    // der Anforderung: „Bereit" trägt A, B, C; D kommt auf Position 1 — danach A, D, B, C.
    [Test]
    [Category("US-3")]
    public async Task Wenn_eine_Karte_auf_Position_1_gelegt_wird_dann_steht_sie_dort_und_nicht_oben_oder_unten()
    {
        var aufbau = await BoardMitDreiKarten();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var karteD = await webApi.LegeKarteAn(aufbau.BoardId, aufbau.InArbeitId, "D");
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.BereitId))).ToHaveTextAsync(["A", "B", "C"]);

        await webApi.VerschiebeKarte(aufbau.BoardId, karteD.KarteId, new Kartenlage(aufbau.BereitId, 2));

        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.BereitId))).ToHaveTextAsync(["A", "D", "B", "C"]);
    }

    // US-4: die Bahnenzahlen ziehen mit — sie sind gerechnet und nicht gespeichert.
    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_Karte_die_Bahn_wechselt_dann_stimmen_die_Bahnenzahlen_ohne_Zutun()
    {
        var aufbau = await BoardMitDreiKarten();
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        await zusehendes.SchalteKartenzahl(true);
        await Expect(zusehendes.Kartenzahlstellen.Nth(0)).ToHaveTextAsync("3");
        await Expect(zusehendes.Kartenzahlstellen.Nth(1)).ToHaveTextAsync("0");

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.InArbeitId, 1));

        await Expect(zusehendes.Kartenzahlstellen.Nth(0)).ToHaveTextAsync("2");
        await Expect(zusehendes.Kartenzahlstellen.Nth(1)).ToHaveTextAsync("1");
    }

    // Ein Ereignis zu einem **anderen** Board lässt das offene Board unberührt. Gemessen statt
    // gehofft: erst die fremde Bewegung, dann eine eigene — kommt die eigene an, war die fremde
    // längst durch den Kanal und hat nichts getan.
    [Test]
    public async Task Wenn_sich_auf_einem_anderen_Board_etwas_bewegt_dann_bleibt_das_offene_Board_unberuehrt()
    {
        var aufbau = await BoardMitDreiKarten();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var fremdesBoard = await webApi.LegeBoardAn("Nebensache");
        var fremdeKarte = await webApi.LegeKarteAn(fremdesBoard.BoardId, fremdesBoard.Spalten[0].SpalteId, "Fremd");
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        await Expect(zusehendes.Karten).ToHaveCountAsync(3);

        await webApi.VerschiebeKarte(fremdesBoard.BoardId, fremdeKarte.KarteId, new Kartenlage(fremdesBoard.Spalten[1].SpalteId, 1));
        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.InArbeitId, 1));

        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.InArbeitId))).ToHaveTextAsync(["C"]);
        await Expect(zusehendes.Kartentitel).ToHaveTextAsync(["A", "B", "C"]);
    }

    // US-13: nach einem Neustart der WebApi nimmt die Leitung sich von selbst wieder auf. Ohne
    // diesen Fall wäre die Zusage nur bis zum ersten Neustart wahr.
    [Test]
    [Category("US-13")]
    public async Task Wenn_die_WebApi_neu_startet_dann_zieht_das_offene_Board_danach_wieder_ohne_Zutun_nach()
    {
        var aufbau = await BoardMitDreiKarten();
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.InArbeitId))).ToHaveCountAsync(0);

        Testumgebung.Aktuelle.HalteWebApiAn();
        await Testumgebung.Aktuelle.StarteWebApiNeu();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.InArbeitId, 1));

        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.InArbeitId)))
            .ToHaveTextAsync(["C"], new LocatorAssertionsToHaveTextOptions { Timeout = SchrankeNachNeustart });
    }

    // US-5: hält jemand eine Karte in der Hand, ordnet sich unter der Maus nichts um — die fremde
    // Bewegung wartet sichtbar und wird beim Loslassen eingespielt.
    [Test]
    [Category("US-5")]
    public async Task Wenn_jemand_eine_Karte_in_der_Hand_haelt_dann_wartet_die_fremde_Bewegung_sichtbar_bis_zum_Loslassen()
    {
        var aufbau = await BoardMitDreiKarten();
        var zusehendes = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await zusehendes.Oeffne(aufbau.BoardId);
        var inArbeit = zusehendes.Spaltenbahn(aufbau.InArbeitId);
        await zusehendes.NimmKarteAuf(zusehendes.KarteMitTitel("A"));
        await Expect(zusehendes.AblageflaecheDerBahn(inArbeit)).ToBeVisibleAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.InArbeitId, 1));

        await Expect(zusehendes.WartendeAenderung).ToHaveTextAsync("1 Änderung wartet");
        await Expect(zusehendes.KartentitelDerBahn(zusehendes.Spaltenbahn(aufbau.BereitId))).ToHaveTextAsync(["A", "B", "C"]);

        await zusehendes.FahreAufFreieFlaeche(inArbeit);
        await Page.Mouse.UpAsync();

        // Danach stehen **beide** Bewegungen in der Bahn. A liegt vorn, weil die Stelle aus der
        // Bahn gerechnet wurde, die der Ziehende vor sich hatte — die zurueckgehaltene Aenderung
        // wird erst beim Loslassen eingespielt und ordnet nichts unter der Maus um.
        await Expect(zusehendes.WartendeAenderung).ToHaveCountAsync(0);
        await Expect(zusehendes.KartentitelDerBahn(inArbeit)).ToHaveTextAsync(["A", "C"]);
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
        return new Aufbau(board.BoardId, bereitId, board.Spalten[1].SpalteId, karteC.KarteId, claude);
    }

    private sealed record Aufbau(long BoardId, long BereitId, long InArbeitId, long KarteC, Kontributor Claude);
}
