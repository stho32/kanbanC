using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-5 und damit die Einloesung der zweiten Haelfte des Fertig-Kriteriums von I0009:
// „verschwindet aus der Auswahl, bleibt aber an alten Karten sichtbar". Bis zu diesem Slice war
// der Satz nicht pruefbar, weil keine Karte auf einen Kontributor zeigte.
//
// Stillgelegt wird ueber den WebApiKlient, nicht ueber die Kontributorenseite: die Zusicherung
// gilt der Kartenseite, und der Weg ueber einen zweiten Schirm haenge den Test an dessen Markup
// — dass die Stilllegung dort bedienbar ist, prueft KontributorStilllegenE2ETests.
[TestFixture]
public class VerantwortlicherStillgelegtE2ETests : PageTest
{
    [Test]
    [Category("US-5")]
    public async Task Wenn_der_Verantwortliche_stillgelegt_wird_dann_zeigt_die_Karte_ihn_weiterhin_mit_dem_Zusatz_stillgelegt()
    {
        var aufbau = await KarteMitJanAlsVerantwortlichem();

        await StilllegeJan(aufbau.JanId);
        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Verantwortlichenname).ToHaveTextAsync("Jan R.");
        await Expect(aufbau.Seite.StillgelegtVermerk).ToHaveTextAsync("stillgelegt");
        await Expect(aufbau.Seite.Verantwortlichenname).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("verantwortlicher-stillgelegt"));
    }

    [Test]
    [Category("US-5")]
    public async Task Wenn_der_Verantwortliche_stillgelegt_wurde_dann_steht_er_nicht_mehr_zur_Wahl_sondern_im_Abschnitt_darunter()
    {
        var aufbau = await KarteMitJanAlsVerantwortlichem();
        await StilllegeJan(aufbau.JanId);
        await aufbau.Seite.LadeNeu();

        await aufbau.Seite.OeffneVerantwortlichenwahl();

        await Expect(aufbau.Seite.VerantwortlichenzeileVon(aufbau.JanId)).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.StillgelegteZeileVon(aufbau.JanId)).ToBeVisibleAsync();
        await Expect(aufbau.Seite.StillgelegteZeileVon(aufbau.JanId)).ToContainTextAsync("Jan R.");
    }

    // Der Wechsel geht nur nach vorn: ein aktiver Kontributor laesst sich weiterhin waehlen.
    [Test]
    [Category("US-5")]
    public async Task Wenn_nach_der_Stilllegung_jemand_anders_gewaehlt_wird_dann_traegt_die_Karte_ihn()
    {
        var aufbau = await KarteMitJanAlsVerantwortlichem();
        await StilllegeJan(aufbau.JanId);
        await aufbau.Seite.LadeNeu();

        await aufbau.Seite.OeffneVerantwortlichenwahl();
        await aufbau.Seite.VerantwortlichenzeileVon(aufbau.AgentId).ClickAsync();

        await Expect(aufbau.Seite.Verantwortlichenname).ToHaveTextAsync("Claude-Agent");
        await Expect(aufbau.Seite.StillgelegtVermerk).ToHaveCountAsync(0);
    }

    private static async Task StilllegeJan(long janId)
    {
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.SetzeStilllegung(janId, istStillgelegt: true);
    }

    private async Task<Aufbau> KarteMitJanAlsVerantwortlichem()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "WBS-Import");
        var jan = await webApi.LegeKontributorAn("Jan R.", Kontributorart.Mensch);
        var agent = await webApi.LegeKontributorAn("Claude-Agent", Kontributorart.Agent);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        await seite.OeffneVerantwortlichenwahl();
        await seite.VerantwortlichenzeileVon(jan.KontributorId).ClickAsync();
        await Expect(seite.Verantwortlichenname).ToHaveTextAsync("Jan R.");
        return new Aufbau(seite, jan.KontributorId, agent.KontributorId);
    }

    private sealed record Aufbau(KartendetailSeite Seite, long JanId, long AgentId);
}
