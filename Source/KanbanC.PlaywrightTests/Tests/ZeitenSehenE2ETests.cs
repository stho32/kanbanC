using System.Text.RegularExpressions;
using KanbanC.Contracts.Kontributoren;
using KanbanC.Contracts.Zeiten;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-1 bis US-8 als Rundlauf über die Oberfläche: der Leerzustand, die Einträgeliste, die Summen
// je Kontributor, die laufende Zeile ohne Dauer und der Stopp am fremden Timer.
// Die Zeiträume entstehen über Start und Stopp — ein Nachtrag mit gewählten Uhrzeiten gehört
// I0025 und fehlt hier; die Rechnung selbst belegen die Unit Tests der Zeitbilanz.
[TestFixture]
public class ZeitenSehenE2ETests : PageTest
{
    // US-5: eine Karte, an der noch niemand gemessen hat, sagt es in einem Satz — keine Nullen.
    [Test]
    [Category("US-5")]
    public async Task Wenn_an_einer_Karte_noch_niemand_gemessen_hat_dann_steht_dort_ein_Satz_und_keine_Nullen()
    {
        var aufbau = await FrischeKarte();

        await Expect(aufbau.Seite.ZeitenLeerstand).ToHaveTextAsync("Noch keine Zeit erfasst.");
        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeitensummen).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeitenzahl).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.ZeitenIst).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();
    }

    // US-5, zweiter Teil: mit dem Start verschwindet der Satz und die laufende Zeile erscheint —
    // ohne Summenzeile, weil noch nichts abgeschlossen ist.
    [Test]
    [Category("US-5")]
    public async Task Wenn_der_erste_Timer_startet_dann_erscheint_die_laufende_Zeile_ohne_Summenzeile()
    {
        var aufbau = await FrischeKarte();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);

        await aufbau.Seite.TimerStarten.ClickAsync();

        await Expect(aufbau.Seite.TimerStoppen).ToBeVisibleAsync();
        await Expect(aufbau.Seite.ZeitenLeerstand).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Zeitensummen).ToHaveCountAsync(0);
    }

    // US-3 und US-4: die laufende Zeile trägt „läuft" an der Stelle des Endes, keine Dauer, die
    // Akzentkante und das Stoppquadrat — drei Merkmale, von denen keines nur Farbe ist.
    [Test]
    [Category("US-4")]
    public async Task Wenn_ein_Eintrag_laeuft_dann_traegt_seine_Zeile_das_Wort_laeuft_die_Kante_und_ein_Stoppquadrat_aber_keine_Dauer()
    {
        var aufbau = await FrischeKarte();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);
        await aufbau.Seite.TimerStarten.ClickAsync();
        await Expect(aufbau.Seite.TimerStoppen).ToBeVisibleAsync();

        await Expect(aufbau.Seite.Zeiteintragsspannen).ToHaveTextAsync(LaufendeSpanne());
        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.LaufendeZeiteintraege).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Stoppquadrate).ToHaveCountAsync(1);
    }

    // US-1 und US-2: nach dem Stopp steht eine abgeschlossene Zeile mit Dauer, darüber die
    // Summenzeile ihres Kontributors, und daneben die Ist-Summe — ohne „von h:mm Soll".
    [Test]
    [Category("US-2")]
    public async Task Wenn_ein_Eintrag_abgeschlossen_ist_dann_traegt_seine_Zeile_eine_Dauer_und_es_erscheinen_Summenzeile_und_Ist_Summe()
    {
        var aufbau = await FrischeKarte();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);
        await aufbau.Seite.TimerStarten.ClickAsync();
        await Expect(aufbau.Seite.TimerStoppen).ToBeVisibleAsync();

        await aufbau.Seite.TimerStoppen.ClickAsync();
        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();

        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveTextAsync(Dauermuster());
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Stefan.KontributorId)).ToContainTextAsync("Stefan");
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Stefan.KontributorId)).ToContainTextAsync(Dauerteil());
        await Expect(aufbau.Seite.ZeitenIst).ToHaveTextAsync(IstSumme());
        await Expect(aufbau.Seite.Zeitenabschnitt).Not.ToContainTextAsync("Soll");
    }

    // US-4: eine abgeschlossene Zeile trägt überhaupt keine Handlung — kein Stift, kein Löschen,
    // kein Stoppquadrat. Ändern und Nachtragen gehören I0025.
    [Test]
    [Category("US-4")]
    public async Task Wenn_eine_Zeile_abgeschlossen_ist_dann_traegt_sie_keine_Handlung()
    {
        var aufbau = await FrischeKarte();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);
        await aufbau.Seite.TimerStarten.ClickAsync();
        await Expect(aufbau.Seite.TimerStoppen).ToBeVisibleAsync();
        await aufbau.Seite.TimerStoppen.ClickAsync();
        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();

        await Expect(aufbau.Seite.Stoppquadrate).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.LaufendeZeiteintraege).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(1);
    }

    // US-1: die Liste steht flach und chronologisch, neueste zuerst, und ist **nicht** nach
    // Kontributor gruppiert — Stefans beide Einträge stehen nicht beieinander.
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Karte_mehrere_Eintraege_traegt_dann_steht_der_neueste_oben_und_die_Liste_ist_nicht_gruppiert()
    {
        var aufbau = await FrischeKarte();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aeltester = await MessungVonBisJetzt(webApi, aufbau.KarteId, aufbau.Stefan.KontributorId);
        var mittlerer = await MessungVonBisJetzt(webApi, aufbau.KarteId, aufbau.Nina.KontributorId);
        var laufender = await webApi.StarteZeitmessung(aufbau.KarteId, aufbau.Stefan.KontributorId);

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Zeitenzahl).ToHaveTextAsync("3");
        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(3);
        await Expect(aufbau.Seite.Zeiteintraege.Nth(0)).ToHaveAttributeAsync("data-zeiteintrag", laufender.ZeiteintragId.ToString());
        await Expect(aufbau.Seite.Zeiteintraege.Nth(1)).ToHaveAttributeAsync("data-zeiteintrag", mittlerer.ZeiteintragId.ToString());
        await Expect(aufbau.Seite.Zeiteintraege.Nth(2)).ToHaveAttributeAsync("data-zeiteintrag", aeltester.ZeiteintragId.ToString());
    }

    // US-2: zwei Kontributoren, zwei Summenzeilen — und die laufende Messung zählt nicht mit.
    [Test]
    [Category("US-2")]
    public async Task Wenn_zwei_Kontributoren_gemessen_haben_dann_traegt_jeder_seine_eigene_Summenzeile()
    {
        var aufbau = await FrischeKarte();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await MessungVonBisJetzt(webApi, aufbau.KarteId, aufbau.Stefan.KontributorId);
        await MessungVonBisJetzt(webApi, aufbau.KarteId, aufbau.Nina.KontributorId);
        await webApi.StarteZeitmessung(aufbau.KarteId, aufbau.Claude.KontributorId);

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Zeitensummen).ToHaveCountAsync(2);
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Stefan.KontributorId)).ToContainTextAsync("Stefan");
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Nina.KontributorId)).ToContainTextAsync("Nina Barth");
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Claude.KontributorId)).ToHaveCountAsync(0);
    }

    // US-3, die auffälligste Entscheidung des Slice am gerenderten Block: wer abgeschlossene
    // **und** laufende Zeit hat, trägt beides getrennt — „0:00 · 1 läuft". Addiert wird die
    // laufende nicht, verschwiegen aber auch nicht.
    [Test]
    [Category("US-3")]
    public async Task Wenn_ein_Kontributor_zugleich_misst_dann_nennt_seine_Summenzeile_die_laufende_Messung_getrennt()
    {
        var aufbau = await FrischeKarte();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await MessungVonBisJetzt(webApi, aufbau.KarteId, aufbau.Stefan.KontributorId);
        var laufender = await webApi.StarteZeitmessung(aufbau.KarteId, aufbau.Stefan.KontributorId);
        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Zeitensummen).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Stefan.KontributorId)).ToContainTextAsync(LaufendeMessungInDerSumme());

        await aufbau.Seite.Stoppquadrat(laufender.ZeiteintragId).ClickAsync();

        await Expect(aufbau.Seite.LaufendeZeiteintraege).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Stefan.KontributorId)).Not.ToContainTextAsync("läuft");
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Stefan.KontributorId)).ToContainTextAsync(Dauerteil());
    }

    // US-6: die eingelöste Schuld aus I0024 — das Stoppquadrat steht auch am fremden Timer, und
    // der Eintrag behält dabei seinen Kontributor.
    [Test]
    [Category("US-6")]
    public async Task Wenn_ein_fremder_Timer_laeuft_dann_beendet_das_Stoppquadrat_ihn_und_seine_Summenzeile_erscheint()
    {
        var aufbau = await FrischeKarte();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var fremder = await webApi.StarteZeitmessung(aufbau.KarteId, aufbau.Claude.KontributorId);
        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.TimerStoppen).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Stoppquadrat(fremder.ZeiteintragId)).ToBeVisibleAsync();

        await aufbau.Seite.Stoppquadrat(fremder.ZeiteintragId).ClickAsync();

        await Expect(aufbau.Seite.LaufendeZeiteintraege).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeiteintragsdauern).ToHaveCountAsync(1);
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Claude.KontributorId)).ToContainTextAsync("Claude");
        await Expect(aufbau.Seite.ZeitenIst).ToContainTextAsync("Ist");

        var detail = await webApi.LadeKartendetail(aufbau.KarteId);
        Assert.That(detail.Zeiteintraege, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(detail.Zeiteintraege[0].Ende, Is.Not.Null);
            Assert.That(detail.Zeiteintraege[0].Kontributor.KontributorId, Is.EqualTo(aufbau.Claude.KontributorId), "Gestoppt wurde der Timer, nicht die Urheberschaft.");
        });
    }

    // US-6, letzter Teil: beenden darf jeder — auch wer keine Identität gewählt hat.
    [Test]
    [Category("US-6")]
    public async Task Wenn_keine_Identitaet_gewaehlt_ist_dann_traegt_die_laufende_Zeile_trotzdem_ihr_Stoppquadrat()
    {
        var aufbau = await FrischeKarte();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var fremder = await webApi.StarteZeitmessung(aufbau.KarteId, aufbau.Claude.KontributorId);
        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();
        await aufbau.Seite.Stoppquadrat(fremder.ZeiteintragId).ClickAsync();

        await Expect(aufbau.Seite.LaufendeZeiteintraege).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Claude.KontributorId)).ToBeVisibleAsync();
    }

    // US-7: mein eigener Timer lässt sich oben am Knopf und unten an seiner Zeile beenden — beide
    // Wege zeigen denselben Eintrag, also endet auch derselbe.
    [Test]
    [Category("US-7")]
    public async Task Wenn_mein_eigener_Timer_laeuft_dann_beendet_ihn_auch_das_Stoppquadrat_an_seiner_Zeile()
    {
        var aufbau = await FrischeKarte();
        await WaehleIdentitaet(aufbau.Seite, aufbau.Stefan);
        await aufbau.Seite.TimerStarten.ClickAsync();
        await Expect(aufbau.Seite.TimerStoppen).ToBeVisibleAsync();
        await Expect(aufbau.Seite.Stoppquadrate).ToHaveCountAsync(1);

        await aufbau.Seite.Stoppquadrate.ClickAsync();

        await Expect(aufbau.Seite.TimerStarten).ToBeVisibleAsync();
        await Expect(aufbau.Seite.TimerStoppen).ToHaveCountAsync(0);
        await Expect(aufbau.Seite.Zeitensumme(aufbau.Stefan.KontributorId)).ToBeVisibleAsync();
    }

    // US-1 und US-6: jeder Stand überlebt den Reload — die Liste kommt aus der Karte, nicht aus
    // dem Sitzungszustand des Browsers.
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Seite_neu_geladen_wird_dann_stehen_dieselben_Zeilen_in_derselben_Reihenfolge()
    {
        var aufbau = await FrischeKarte();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var aelterer = await MessungVonBisJetzt(webApi, aufbau.KarteId, aufbau.Stefan.KontributorId);
        var neuerer = await MessungVonBisJetzt(webApi, aufbau.KarteId, aufbau.Nina.KontributorId);

        await aufbau.Seite.LadeNeu();
        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(2);

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Zeiteintraege).ToHaveCountAsync(2);
        await Expect(aufbau.Seite.Zeiteintraege.Nth(0)).ToHaveAttributeAsync("data-zeiteintrag", neuerer.ZeiteintragId.ToString());
        await Expect(aufbau.Seite.Zeiteintraege.Nth(1)).ToHaveAttributeAsync("data-zeiteintrag", aelterer.ZeiteintragId.ToString());
        await Expect(aufbau.Seite.Zeitensummen).ToHaveCountAsync(2);
    }

    // US-1: jede Zeile trägt den Initialenkreis ihres Kontributors — dasselbe Kürzel und dieselbe
    // Artfarbe wie in der Kommentarliste, aus derselben Ableitung.
    [Test]
    [Category("US-1")]
    public async Task Wenn_eine_Zeile_erscheint_dann_traegt_sie_den_Initialenkreis_ihres_Kontributors()
    {
        var aufbau = await FrischeKarte();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await MessungVonBisJetzt(webApi, aufbau.KarteId, aufbau.Nina.KontributorId);

        await aufbau.Seite.LadeNeu();

        await Expect(aufbau.Seite.Zeiteintragskuerzel).ToHaveTextAsync("NB");
        await Expect(aufbau.Seite.Zeiteintragskuerzel).ToHaveClassAsync(new Regex("kuerzel-mensch"));
    }

    private async Task<Aufbau> FrischeKarte()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var board = await webApi.LegeBoardAn("Entwicklung");
        var karte = await webApi.LegeKarteAn(board.BoardId, board.Spalten[0].SpalteId, "Migration schreiben");
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var nina = await webApi.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);
        var claude = await webApi.LegeKontributorAn("Claude", Kontributorart.Agent);

        var seite = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await seite.Oeffne(karte.KarteId);
        return new Aufbau(seite, karte.KarteId, stefan, nina, claude);
    }

    // Ein abgeschlossener Eintrag über die API: Start und Stopp liegen dicht beieinander, die
    // Dauer ist deshalb „0:00". Geprüft wird hier die Verdrahtung — die Rechnung belegen die Unit
    // Tests der Zeitbilanz, und einen Nachtrag mit gewählten Uhrzeiten gibt es erst mit I0025.
    private static async Task<Zeiteintrag> MessungVonBisJetzt(WebApiKlient webApi, long karteId, long kontributorId)
    {
        var gestartet = await webApi.StarteZeitmessung(karteId, kontributorId);
        return await webApi.BeendeZeitmessung(karteId, gestartet.ZeiteintragId);
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

    // „heute 09:12 – läuft": Tagesbezug, Beginn und das Wort statt eines Endes.
    private static Regex LaufendeSpanne()
    {
        return new Regex(@"^heute \d{2}:\d{2} – läuft$");
    }

    private static Regex Dauermuster()
    {
        return new Regex(@"^\d+:\d{2}$");
    }

    private static Regex Dauerteil()
    {
        return new Regex(@"\d+:\d{2}");
    }

    // „Ist 1:22": das Wort und der Wert, den die Zeitbilanz liefert — im Lauf steht dort 0:00,
    // weil Start und Stopp dicht beieinanderliegen.
    private static Regex IstSumme()
    {
        return new Regex(@"^Ist \d+:\d{2}$");
    }

    // „0:00 · 1 läuft": die laufende Messung ist genannt, aber nicht addiert.
    private static Regex LaufendeMessungInDerSumme()
    {
        return new Regex(@"\d+:\d{2} · 1 läuft");
    }

    private sealed record Aufbau(KartendetailSeite Seite, long KarteId, Kontributor Stefan, Kontributor Nina, Kontributor Claude);
}
