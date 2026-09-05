using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-4: die Wahl selbst — wer darinsteht, wer nicht, und was „niemand" bedeutet.
[TestFixture]
public class VerantwortlicherE2ETests : PageTest
{
    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_Karte_niemanden_traegt_dann_zeigt_das_Feld_niemand()
    {
        var aufbau = await KarteUndKontributoren();

        await Expect(aufbau.Seite.Verantwortlich).ToContainTextAsync("niemand");
    }

    // Das Rechenbeispiel der Anforderung: drei Waehlbare, nicht einer und nicht vier.
    [Test]
    [Category("US-4")]
    public async Task Wenn_die_Wahl_geoeffnet_wird_dann_stehen_drei_Waehlbare_ein_Suchfeld_und_niemand_darin()
    {
        var aufbau = await KarteUndKontributoren();

        await aufbau.Seite.OeffneVerantwortlichenwahl();

        await Expect(aufbau.Seite.Verantwortlichensuche).ToBeVisibleAsync();
        await Expect(aufbau.Seite.Verantwortlichenzeilen).ToHaveCountAsync(3);
        await Expect(aufbau.Seite.VerantwortlichenzeileVon(aufbau.Maria.KontributorId)).ToBeVisibleAsync();
        await Expect(aufbau.Seite.VerantwortlichenzeileVon(aufbau.Jan.KontributorId)).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Niemand).ToBeVisibleAsync();
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_im_Suchfeld_getippt_wird_dann_bleibt_nur_der_Treffer_stehen()
    {
        var aufbau = await KarteUndKontributoren();
        await aufbau.Seite.OeffneVerantwortlichenwahl();

        await aufbau.Seite.Verantwortlichensuche.FillAsync("lenz");

        await Expect(aufbau.Seite.Verantwortlichenzeilen).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.VerantwortlichenzeileVon(aufbau.Maria.KontributorId)).ToBeVisibleAsync();
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_ein_Agent_gewaehlt_wird_dann_zeigt_die_Karte_ihn_mit_Art_Plakette_und_behaelt_ihn_nach_einem_Reload()
    {
        var aufbau = await KarteUndKontributoren();
        await aufbau.Seite.OeffneVerantwortlichenwahl();

        await aufbau.Seite.VerantwortlichenzeileVon(aufbau.Agent.KontributorId).ClickAsync();

        await Expect(aufbau.Seite.Verantwortlichenname).ToHaveTextAsync("Claude-Agent");
        await Expect(aufbau.Seite.Verantwortlichenart).ToHaveTextAsync("Agent");

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Verantwortlichenname).ToHaveTextAsync("Claude-Agent");
    }

    [Test]
    [Category("US-4")]
    public async Task Wenn_niemand_gewaehlt_wird_dann_hat_die_Karte_danach_keinen_Verantwortlichen_mehr()
    {
        var aufbau = await KarteUndKontributoren();
        await aufbau.Seite.OeffneVerantwortlichenwahl();
        await aufbau.Seite.VerantwortlichenzeileVon(aufbau.Agent.KontributorId).ClickAsync();
        await Expect(aufbau.Seite.Verantwortlichenname).ToHaveTextAsync("Claude-Agent");

        await aufbau.Seite.OeffneVerantwortlichenwahl();
        await aufbau.Seite.Niemand.ClickAsync();

        await Expect(aufbau.Seite.Verantwortlich).ToContainTextAsync("niemand");

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Verantwortlich).ToContainTextAsync("niemand");
        await Expect(aufbau.Seite.Verantwortlichenname).ToHaveCountAsync(0);
    }

    private async Task<Aufbau> KarteUndKontributoren()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "WBS-Import");
        await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var agent = await webApi.LegeKontributorAn("Claude-Agent", Kontributorart.Agent);
        var maria = await webApi.LegeKontributorAn("Maria Lenz", Kontributorart.Abgebildet);
        var jan = await webApi.LegeKontributorAn("Jan R.", Kontributorart.Mensch);
        await webApi.SetzeStilllegung(jan.KontributorId, istStillgelegt: true);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        return new Aufbau(seite, karte.KarteId, agent, maria, jan);
    }

    private sealed record Aufbau(KartendetailSeite Seite, long KarteId, Kontributor Agent, Kontributor Maria, Kontributor Jan);
}
