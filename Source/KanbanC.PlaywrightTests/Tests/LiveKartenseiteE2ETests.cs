using KanbanC.Contracts.Karten;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-9 und US-10: die zweite offene Sicht, die das Fertig-Kriterium meint. Der zweite Fall ist der
// teure und der wichtige — „eine Änderung, die mir unter der Hand den Text austauscht, wäre
// schlimmer als gar keine Live-Aktualisierung".
[TestFixture]
public class LiveKartenseiteE2ETests : PageTest
{
    // US-9: die Kopfzeile nennt ohne Zutun die neue Spalte und trägt dieselbe Marke wie die Karte
    // am Board.
    [Test]
    [Category("US-9")]
    public async Task Wenn_die_geoeffnete_Karte_bewegt_wird_dann_nennt_die_Kopfzeile_ohne_Zutun_die_neue_Spalte()
    {
        var aufbau = await KarteInDerErstenBahn();
        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(aufbau.KarteC);
        await Expect(seite.Spalte).ToHaveTextAsync("Spalte Zu erledigen");
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);

        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.InArbeitId, 1, aufbau.Claude.KontributorId));

        await Expect(seite.Spalte).ToHaveTextAsync("Spalte In Arbeit");
        await Expect(seite.Einflugmarke).ToContainTextAsync("Claude-Agent · über die API ·");
    }

    // Ein Ereignis zu einer **anderen** Karte lässt die offene Kartenseite unberührt. Gemessen
    // statt gehofft: erst die fremde Karte, dann die eigene — kommt die eigene an, war die fremde
    // längst durch den Kanal.
    [Test]
    [Category("US-9")]
    public async Task Wenn_eine_andere_Karte_bewegt_wird_dann_bleibt_die_offene_Kartenseite_unberuehrt()
    {
        var aufbau = await KarteInDerErstenBahn();
        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(aufbau.KarteC);
        await Expect(seite.Spalte).ToHaveTextAsync("Spalte Zu erledigen");
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);

        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteA, new Kartenlage(aufbau.InArbeitId, 1));
        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.InArbeitId, 1));

        await Expect(seite.Spalte).ToHaveTextAsync("Spalte In Arbeit");
        await Expect(seite.Einflugmarke).ToHaveCountAsync(1);
    }

    // US-10: mit offenem Beschreibungsfeld und ungesendetem Text bleibt der Text stehen, die
    // Meldung wartet sichtbar — und es gibt **kein Angebot**: eine Bewegung fasst kein Feld an.
    [Test]
    [Category("US-10")]
    public async Task Wenn_ein_Feld_offen_steht_dann_bleibt_der_ungesendete_Text_stehen_und_die_Meldung_wartet()
    {
        var aufbau = await KarteInDerErstenBahn();
        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(aufbau.KarteC);
        await seite.BeschreibungHinzufuegen.ClickAsync();
        await seite.Beschreibungsfeld.FillAsync("Drei Sätze, noch nicht abgeschickt.");
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);

        await webApi.VerschiebeKarte(aufbau.BoardId, aufbau.KarteC, new Kartenlage(aufbau.InArbeitId, 1, aufbau.Claude.KontributorId));

        await Expect(seite.WartendeAenderung).ToHaveTextAsync("1 Änderung wartet");
        await Expect(seite.Beschreibungsfeld).ToHaveValueAsync("Drei Sätze, noch nicht abgeschickt.");
        await Expect(seite.Spalte).ToHaveTextAsync("Spalte Zu erledigen");
        await Expect(seite.Einflugmarke).ToHaveCountAsync(0);

        await seite.Beschreibungsfeld.BlurAsync();

        await Expect(seite.WartendeAenderung).ToHaveCountAsync(0);
        await Expect(seite.Spalte).ToHaveTextAsync("Spalte In Arbeit");
        await Expect(seite.Einflugmarke).ToContainTextAsync("Claude-Agent · über die API ·");
        await Expect(seite.Beschreibung).ToHaveTextAsync("Drei Sätze, noch nicht abgeschickt.");
    }

    private static async Task<Aufbau> KarteInDerErstenBahn()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var bereitId = board.Spalten[0].SpalteId;
        var karteA = await webApi.LegeKarteAn(board.BoardId, bereitId, "A");
        var karteC = await webApi.LegeKarteAn(board.BoardId, bereitId, "C");
        var claude = await webApi.LegeKontributorAn("Claude-Agent", Kontributorart.Agent);
        return new Aufbau(board.BoardId, board.Spalten[1].SpalteId, karteA.KarteId, karteC.KarteId, claude);
    }

    private sealed record Aufbau(long BoardId, long InArbeitId, long KarteA, long KarteC, Kontributor Claude);
}
