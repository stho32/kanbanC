using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-1 bis US-9 als Rundlauf über die Oberfläche: stoppen, die Plakette verschwinden sehen, neu
// starten. Der hinterlassene Eintrag mit Beginn, Ende und Kontributor wird an der API geprüft und
// nicht im Browser — die Liste, die ihn zeigte, gehört I0026.
[TestFixture]
public class TimerStoppenE2ETests : PageTest
{
    // US-1: aus der stillen Zeile ist ein Knopf geworden, und er nennt weiterhin, seit wann
    // gemessen wird — aber keine mitlaufende Dauer.
    [Test]
    [Category("US-1")]
    public async Task Wenn_fuer_mich_ein_Timer_laeuft_dann_steht_im_Zeitenblock_der_Stoppknopf_mit_der_Startzeit()
    {
        var aufbau = await KarteMitLaufendemTimer();

        await Expect(aufbau.Seite.TimerStoppen).ToBeVisibleAsync();
        await Expect(aufbau.Seite.TimerStoppen).ToContainTextAsync("Stoppen");
        await Expect(aufbau.Seite.ZeitenLaeuft).ToHaveTextAsync(Uhrzeitmuster());
        await Expect(aufbau.Seite.TimerStarten).ToHaveCountAsync(0);
    }

    [Test]
    [Category("US-1")]
    public async Task Wenn_der_Timer_gestoppt_wird_dann_steht_dort_wieder_Timer_starten_und_kein_Stoppknopf_mehr()
    {
        var aufbau = await KarteMitLaufendemTimer();

        await aufbau.Seite.TimerStoppen.ClickAsync();

        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();
        await Expect(aufbau.Seite.TimerStoppen).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.ZeitenLaeuft).ToHaveCountAsync(0);
    }

    // US-1, letzter Teil: weder Summenzeile noch Einträgeliste — die gehören I0026.
    [Test]
    [Category("US-1")]
    public async Task Wenn_der_erste_Eintrag_abgeschlossen_ist_dann_zeigt_der_Zeitenblock_weder_Summe_noch_Liste()
    {
        var aufbau = await KarteMitLaufendemTimer();

        await aufbau.Seite.TimerStoppen.ClickAsync();
        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();

        await Expect(aufbau.Seite.Zeitenabschnitt).ToContainTextAsync("Zeiten");
        await Expect(aufbau.Seite.Zeitenabschnitt).Not.ToContainTextAsync("Summe");
        await Expect(aufbau.Seite.Zeitenabschnitt).Not.ToContainTextAsync("Ist");
        await Expect(aufbau.Seite.Zeitenabschnitt).Not.ToContainTextAsync("Soll");
    }

    // US-2: die Plakette auf der Karte in der Bahn ist fort und bleibt es nach dem Reload — sie
    // fällt von selbst heraus, weil der Zeitenleser auf „kein Ende" filtert.
    [Test]
    [Category("US-2")]
    public async Task Wenn_der_Timer_gestoppt_wurde_dann_traegt_die_Karte_in_der_Bahn_keine_Plakette_mehr()
    {
        var aufbau = await KarteMitLaufendemTimer();
        await aufbau.Seite.TimerStoppen.ClickAsync();
        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);

        await Expect(board.Laufplaketten).ToHaveCountAsync(0);

        await board.LadeNeu();

        await Expect(board.Laufplaketten).ToHaveCountAsync(0);
        await Expect(board.Karten).ToHaveCountAsync(2);
    }

    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Kartenseite_nach_dem_Stopp_neu_geladen_wird_dann_steht_dort_weiterhin_Timer_starten()
    {
        var aufbau = await KarteMitLaufendemTimer();
        await aufbau.Seite.TimerStoppen.ClickAsync();
        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();
        await Expect(aufbau.Seite.TimerStoppen).ToHaveCountAsync(0);
    }

    // US-3: der hinterlassene Eintrag, an der API gelesen — Beginn, Ende und Kontributor.
    [Test]
    [Category("US-3")]
    public async Task Wenn_der_Timer_ueber_die_Oberflaeche_gestoppt_wurde_dann_traegt_die_API_den_Eintrag_mit_Beginn_Ende_und_Kontributor()
    {
        var aufbau = await KarteMitLaufendemTimer();

        await aufbau.Seite.TimerStoppen.ClickAsync();
        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();

        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var detail = await webApi.LadeKartendetail(aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(detail.Zeiteintraege[0].Ende, Is.Not.Null);
            Assert.That(detail.Zeiteintraege[0].Ende, Is.GreaterThanOrEqualTo(detail.Zeiteintraege[0].Beginn));
            Assert.That(detail.Zeiteintraege[0].Kontributor.KontributorId, Is.EqualTo(aufbau.Stefan.KontributorId));
        });
    }

    // US-7: der Weg „stoppen, später weiterarbeiten" — die sichtbare Probe darauf, dass der
    // partielle UNIQUE-Index das Paar mit gesetztem Ende wieder freigibt.
    [Test]
    [Category("US-7")]
    public async Task Wenn_nach_dem_Stopp_erneut_gestartet_wird_dann_laeuft_ein_zweiter_Timer_neben_dem_abgeschlossenen_Eintrag()
    {
        var aufbau = await KarteMitLaufendemTimer();
        await aufbau.Seite.TimerStoppen.ClickAsync();
        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();

        await aufbau.Seite.TimerStarten.ClickAsync();

        await Expect(aufbau.Seite.TimerStoppen).ToBeVisibleAsync();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var detail = await webApi.LadeKartendetail(aufbau.ErsteKarteId);
        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(detail.Zeiteintraege.Count(eintrag => eintrag.Ende is null), Is.EqualTo(1));
            Assert.That(detail.Zeiteintraege.Count(eintrag => eintrag.Ende is not null), Is.EqualTo(1));
            Assert.That(detail.Zeiteintraege[0].ZeiteintragId, Is.Not.EqualTo(detail.Zeiteintraege[1].ZeiteintragId));
        });
    }

    // US-4: fremde Timer sind in der Oberfläche nicht beendbar — an der fremden Plakette hängt
    // keine Handlung; über die API geht der Stopp trotzdem.
    [Test]
    [Category("US-4")]
    public async Task Wenn_ein_fremder_Timer_laeuft_dann_traegt_die_Oberflaeche_keine_Stopphandlung_und_die_API_beendet_ihn_doch()
    {
        var aufbau = await KarteMitLaufendemTimer();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var fremder = await webApi.StarteZeitmessung(aufbau.ZweiteKarteId, aufbau.Nina.KontributorId);

        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await board.Oeffne(aufbau.BoardId);
        await Expect(board.FremdeLaufplaketten).ToHaveCountAsync(1);
        await Expect(board.KarteMitTitel("Kartenform zeichnen")).Not.ToContainTextAsync("Stoppen");

        await aufbau.Seite.Oeffne(aufbau.ZweiteKarteId);
        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();
        await Expect(aufbau.Seite.TimerStoppen).ToHaveCountAsync(0);

        var beendeter = await webApi.BeendeZeitmessung(aufbau.ZweiteKarteId, fremder.ZeiteintragId);

        Assert.That(beendeter.Ende, Is.Not.Null);
        Assert.That(beendeter.Kontributor.KontributorId, Is.EqualTo(aufbau.Nina.KontributorId));
    }

    // US-9: fällt die WebApi aus, erscheint eine lesbare Meldung statt einer Ausnahmeseite — und
    // der Knopf zeigt weiterhin „Stoppen", denn der Timer läuft ja noch.
    [Test]
    [Category("US-9")]
    public async Task Wenn_die_WebApi_beim_Stoppen_ausfaellt_dann_erscheint_eine_lesbare_Meldung_und_der_Stoppknopf_bleibt()
    {
        var aufbau = await KarteMitLaufendemTimer();
        Testumgebung.Aktuelle.HalteWebApiAn();

        await aufbau.Seite.TimerStoppen.ClickAsync();

        await Expect(aufbau.Seite.BlattFehlermeldung).ToBeVisibleAsync();
        await Expect(aufbau.Seite.BlattFehlermeldung).ToContainTextAsync("Die WebApi ist nicht erreichbar.");
        await Expect(aufbau.Seite.TimerStoppen).ToBeVisibleAsync();
    }

    private async Task<Aufbau> KarteMitLaufendemTimer()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var ersteKarte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var zweiteKarte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Kartenform zeichnen");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var nina = await webApi.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(ersteKarte.KarteId);
        await WaehleIdentitaet(seite, stefan);
        await seite.TimerStarten.ClickAsync();
        await Expect(seite.TimerStoppen).ToBeVisibleAsync();
        return new Aufbau(seite, board.BoardId, ersteKarte.KarteId, zweiteKarte.KarteId, stefan, nina);
    }

    // Gewaehlt wird über die Kopfzeile, den Weg des Menschen.
    private async Task WaehleIdentitaet(KartendetailSeite seite, Kontributor kontributor)
    {
        var rahmen = new Rahmen(Page);
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(kontributor.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync(kontributor.Name);
        await seite.KehreZumBlattZurueck();
    }

    // „läuft seit 08:04" — die Startzeit, nicht die verstrichene Dauer.
    private static System.Text.RegularExpressions.Regex Uhrzeitmuster()
    {
        return new System.Text.RegularExpressions.Regex(@"^läuft seit \d{2}:\d{2}$");
    }

    private sealed record Aufbau(KartendetailSeite Seite, long BoardId, long ErsteKarteId, long ZweiteKarteId, Kontributor Stefan, Kontributor Nina);
}
