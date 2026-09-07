using System.Text.RegularExpressions;
using KanbanC.Contracts.Boards;
using KanbanC.Contracts.Kontributoren;
using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-1 bis US-11 als Rundlauf über die Oberfläche. Der Aufbau geht über **zwei Boards**, weil
// „alle gerade laufenden Timer" sonst nicht geprüft wäre: Board.LaufendeZeiteintraege kennt immer
// nur den Ausschnitt des offenen Boards.
[TestFixture]
public class LaufendeTimerE2ETests : PageTest
{
    // US-8: läuft nichts, steht in der Kopfzeile nichts — kein Nullwert und keine Attrappe.
    [Test]
    [Category("US-8")]
    public async Task Wenn_kein_Timer_laeuft_dann_steht_in_der_Kopfzeile_keine_Plakette()
    {
        var aufbau = await ZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();

        await Expect(rahmen.Laufzeitzaehler).ToHaveCountAsync(0);
        await Expect(rahmen.Kopfzeile).Not.ToContainTextAsync("laufen");
    }

    // US-1 und US-3: ein eigener Timer, die Plakette zählt und ist gefüllt.
    [Test]
    [Category("US-1")]
    public async Task Wenn_ein_eigener_Timer_laeuft_dann_zeigt_die_Plakette_ein_laeuft_und_ist_gefuellt()
    {
        var aufbau = await ZweiBoards();
        var rahmen = new Rahmen(Page);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        await aufbau.Boards.Oeffne();
        await WaehleIdentitaet(rahmen, aufbau.Stefan);

        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("1 läuft");
        await Expect(rahmen.Laufzeitzaehler).ToHaveClassAsync(new Regex("kopfzeile-laufzeit-eigen"));
    }

    // US-1: **über alle Boards** — der Kern dieses Slice. Der zweite Timer liegt auf einem anderen
    // Board und zählt trotzdem mit, auch wenn gerade das erste Board offen steht.
    [Test]
    [Category("US-1")]
    public async Task Wenn_auf_zwei_Boards_ein_Timer_laeuft_dann_zaehlt_die_Plakette_beide()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Board.Oeffne(aufbau.ErstesBoard.BoardId);

        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("2 laufen");
        await Expect(rahmen.Laufzeitzaehler).ToHaveClassAsync(new Regex("kopfzeile-laufzeit-eigen"));
    }

    // US-1: dieselbe Plakette mit demselben Stand auf jeder Seite der Anwendung.
    [Test]
    [Category("US-1")]
    public async Task Wenn_die_Seite_wechselt_dann_steht_ueberall_dieselbe_Plakette()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);

        await aufbau.Boards.Oeffne();
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("2 laufen");

        await aufbau.Board.Oeffne(aufbau.ZweitesBoard.BoardId);
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("2 laufen");

        await aufbau.Kontributoren.Oeffne();
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("2 laufen");

        await aufbau.Kartendetail.Oeffne(aufbau.ZweiteKarteId);
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("2 laufen");
    }

    // US-2: die Plakette nennt weder Dauer noch Startzeit; ihr Hinweistext nennt beide Timer
    // einzeln.
    [Test]
    [Category("US-2")]
    public async Task Wenn_die_Plakette_gelesen_wird_dann_traegt_sie_keine_Dauer_und_ihr_Titel_nennt_alle_einzeln()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();

        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("2 laufen");
        await Expect(rahmen.Laufzeitzaehler).ToHaveAttributeAsync("title", Titelmuster());
    }

    // US-3: ein dritter Kontributor macht aus meinen Timern fremde — die Zahl bleibt, die Füllung
    // geht.
    [Test]
    [Category("US-3")]
    public async Task Wenn_die_Identitaet_auf_einen_dritten_Kontributor_wechselt_dann_wird_die_Plakette_ruhig_und_die_Zahl_bleibt()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();
        await Expect(rahmen.Laufzeitzaehler).ToHaveClassAsync(new Regex("kopfzeile-laufzeit-eigen"));

        await WaehleIdentitaet(rahmen, aufbau.Nina);

        await Expect(rahmen.Laufzeitzaehler).ToHaveClassAsync(new Regex("kopfzeile-laufzeit-fremd"));
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("2 laufen");
    }

    // US-4: das Popover zeigt je Eintrag eine Zeile mit Kartennummer, Titel, Startzeit, Board und
    // Kontributor — und keine Dauer.
    [Test]
    [Category("US-4")]
    public async Task Wenn_die_Plakette_angeklickt_wird_dann_zeigt_das_Popover_je_Timer_eine_Zeile_mit_Karte_Board_und_Kontributor()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();

        await rahmen.OeffneLaufzeitliste();

        await Expect(rahmen.Laufzeitpopover).ToContainTextAsync("Läuft gerade …");
        await Expect(rahmen.Laufzeitzeilen).ToHaveCountAsync(2);
        await Expect(rahmen.LaufzeitKartenverweis(aufbau.EigenerZeiteintragId)).ToHaveTextAsync("WBS-01 Timer starten und stoppen");
        await Expect(rahmen.LaufzeitKartenverweis(aufbau.FremderZeiteintragId)).ToHaveTextAsync("WBS-Import: Markdown-Baum");
        await Expect(rahmen.Laufzeitbeginne).ToHaveTextAsync([Beginnmuster(), Beginnmuster()]);
        await Expect(rahmen.Laufzeitherkuenfte).ToHaveTextAsync(["KanbanC — Release 2 · für mich", "Beschaffung · Claude"]);
    }

    // US-5: meine Zeile steht oben, obwohl der fremde Timer länger läuft — die Gegenprobe der
    // Vorrangregel.
    [Test]
    [Category("US-5")]
    public async Task Wenn_ein_fremder_Timer_laenger_laeuft_dann_steht_meine_Zeile_trotzdem_oben()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();

        await rahmen.OeffneLaufzeitliste();

        await Expect(rahmen.Laufzeitkuerzel).ToHaveTextAsync(["ST", "CL"]);
    }

    // US-6: aus der Zeile führt der Weg auf die Kartenseite, und das Popover schließt mit dem
    // Seitenwechsel.
    [Test]
    [Category("US-6")]
    public async Task Wenn_eine_Zeile_angeklickt_wird_dann_oeffnet_sich_ihre_Karte_und_das_Popover_schliesst()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();
        await rahmen.OeffneLaufzeitliste();

        await rahmen.LaufzeitKartenverweis(aufbau.FremderZeiteintragId).ClickAsync();

        await aufbau.Kartendetail.ErwarteGeoeffnet();
        await Expect(Page).ToHaveURLAsync(new Regex($"/karten/{aufbau.ZweiteKarteId}$"));
        await Expect(rahmen.Laufzeitpopover).ToHaveCountAsync(0);
    }

    // US-7: das Stoppquadrat an der **fremden** Zeile beendet genau diesen Eintrag; die Plakette
    // zählt danach herunter.
    [Test]
    [Category("US-7")]
    public async Task Wenn_das_Stoppquadrat_einer_fremden_Zeile_gedrueckt_wird_dann_faellt_sie_heraus_und_die_Plakette_zaehlt_herunter()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();
        await rahmen.OeffneLaufzeitliste();

        await rahmen.LaufzeitStoppquadrat(aufbau.FremderZeiteintragId).ClickAsync();

        await Expect(rahmen.Laufzeitzeilen).ToHaveCountAsync(1);
        await Expect(rahmen.Laufzeitzeile(aufbau.FremderZeiteintragId)).ToHaveCountAsync(0);
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("1 läuft");
    }

    // US-7 und US-8: mit der letzten Zeile verschwindet die Plakette, und der Stand überlebt den
    // Reload.
    [Test]
    [Category("US-7")]
    public async Task Wenn_auch_die_letzte_Zeile_gestoppt_wird_dann_verschwinden_Popover_und_Plakette_und_bleiben_nach_dem_Reload_fort()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();
        await rahmen.OeffneLaufzeitliste();

        await rahmen.LaufzeitStoppquadrat(aufbau.FremderZeiteintragId).ClickAsync();
        await Expect(rahmen.Laufzeitzeilen).ToHaveCountAsync(1);
        await rahmen.LaufzeitStoppquadrat(aufbau.EigenerZeiteintragId).ClickAsync();

        await Expect(rahmen.Laufzeitpopover).ToHaveCountAsync(0);
        await Expect(rahmen.Laufzeitzaehler).ToHaveCountAsync(0);

        await Page.ReloadAsync();
        await Expect(rahmen.Laufzeitzaehler).ToHaveCountAsync(0);
    }

    // US-9: eine archivierte Karte steht in keiner Bahn mehr — das Popover ist dann der einzige
    // Ort, an dem ihr laufender Timer sichtbar und beendbar ist.
    [Test]
    [Category("US-9")]
    public async Task Wenn_die_Karte_archiviert_wird_dann_bleibt_ihre_Zeile_gekennzeichnet_stehen_und_laesst_sich_beenden()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.SchalteKartenarchivierung(aufbau.ZweitesBoard.BoardId, aufbau.ZweiteKarteId, istArchiviert: true);
        await aufbau.Boards.Oeffne();

        await rahmen.OeffneLaufzeitliste();

        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("2 laufen");
        await Expect(rahmen.Laufzeitzeile(aufbau.FremderZeiteintragId)).ToContainTextAsync("archiviert");

        await rahmen.LaufzeitStoppquadrat(aufbau.FremderZeiteintragId).ClickAsync();
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("1 läuft");
    }

    // US-9: ein nach dem Start stillgelegter Kontributor behält seine Zeile — sie trägt den
    // Zusatz und bleibt stoppbar.
    [Test]
    [Category("US-9")]
    public async Task Wenn_der_Zeitmesser_stillgelegt_wird_dann_bleibt_seine_Zeile_gekennzeichnet_stehen()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.SetzeStilllegung(aufbau.Claude.KontributorId, istStillgelegt: true);
        await aufbau.Boards.Oeffne();

        await rahmen.OeffneLaufzeitliste();

        await Expect(rahmen.Laufzeitzeile(aufbau.FremderZeiteintragId)).ToContainTextAsync("stillgelegt");
        await Expect(rahmen.Laufzeitzeilen).ToHaveCountAsync(2);
    }

    // US-10: der eigene Start auf der Kartenseite hebt die Zahl **ohne Reload und ohne
    // Seitenwechsel** — das ist der Laufzeitmelder.
    [Test]
    [Category("US-10")]
    public async Task Wenn_ich_auf_der_Kartenseite_starte_dann_erscheint_die_Plakette_ohne_Reload()
    {
        var aufbau = await ZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Kartendetail.Oeffne(aufbau.ErsteKarteId);
        await WaehleIdentitaet(rahmen, aufbau.Stefan);
        await aufbau.Kartendetail.Oeffne(aufbau.ErsteKarteId);
        await Expect(rahmen.Laufzeitzaehler).ToHaveCountAsync(0);

        await aufbau.Kartendetail.TimerStarten.ClickAsync();

        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("1 läuft");

        await aufbau.Kartendetail.TimerStoppen.ClickAsync();

        await Expect(rahmen.Laufzeitzaehler).ToHaveCountAsync(0);
    }

    // US-4: höchstens eines der beiden Popover ist offen — das Öffnen des einen schließt das
    // andere.
    [Test]
    [Category("US-4")]
    public async Task Wenn_das_eine_Popover_geoeffnet_wird_dann_schliesst_das_andere()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();

        await rahmen.OeffneLaufzeitliste();
        await Expect(rahmen.Identitaetspopover).ToHaveCountAsync(0);

        await rahmen.OeffneIdentitaetswahl();
        await Expect(rahmen.Laufzeitpopover).ToHaveCountAsync(0);

        await rahmen.OeffneLaufzeitliste();
        await Expect(rahmen.Identitaetspopover).ToHaveCountAsync(0);
    }

    // US-4: ein zweiter Klick, ein Klick daneben und Escape schließen die Liste — dieselbe
    // Mechanik wie die Identitätswahl.
    [Test]
    [Category("US-4")]
    public async Task Wenn_neben_die_Liste_geklickt_oder_Escape_gedrueckt_wird_dann_schliesst_sie()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();

        await rahmen.OeffneLaufzeitliste();
        await rahmen.Laufzeitzaehler.ClickAsync();
        await Expect(rahmen.Laufzeitpopover).ToHaveCountAsync(0);

        await rahmen.OeffneLaufzeitliste();
        await Page.Locator("#laufzeit-auffangflaeche").ClickAsync();
        await Expect(rahmen.Laufzeitpopover).ToHaveCountAsync(0);

        await rahmen.OeffneLaufzeitliste();
        await rahmen.Laufzeitpopover.PressAsync("Escape");
        await Expect(rahmen.Laufzeitpopover).ToHaveCountAsync(0);
    }

    // US-7: es gibt in der Liste keinen Knopf, mit dem sich ein Timer starten ließe — ein Start
    // braucht eine Karte.
    [Test]
    [Category("US-7")]
    public async Task Wenn_die_Liste_offen_steht_dann_gibt_es_darin_keinen_Startknopf()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();

        await rahmen.OeffneLaufzeitliste();

        await Expect(rahmen.Laufzeitpopover.Locator("button")).ToHaveCountAsync(2);
        await Expect(rahmen.Laufzeitpopover.Locator(".laufzeitstopp")).ToHaveCountAsync(2);
        await Expect(rahmen.Laufzeitpopover.Locator("#timer-starten")).ToHaveCountAsync(0);
    }

    // US-13: die Identitätswahl behält Knopf, Popover und Verhalten — die neue Plakette daneben
    // ändert daran nichts.
    [Test]
    [Category("US-13")]
    public async Task Wenn_die_Plakette_dasteht_dann_verhaelt_sich_die_Identitaetswahl_unveraendert()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();

        await rahmen.OeffneIdentitaetswahl();

        await Expect(rahmen.IdentitaetWaehlbareZeilen).ToHaveCountAsync(2);
        await rahmen.IdentitaetWaehlbareZeile(aufbau.Nina.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync("Nina Barth");
    }

    // US-6 und US-9: der Sprung führt auch aus einer Zeile, deren Karte archiviert ist — die
    // Kartenseite ist dann der einzige Weg dorthin.
    [Test]
    [Category("US-9")]
    public async Task Wenn_die_Karte_archiviert_ist_dann_fuehrt_ihre_Zeile_trotzdem_auf_die_Kartenseite()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.SchalteKartenarchivierung(aufbau.ZweitesBoard.BoardId, aufbau.ZweiteKarteId, istArchiviert: true);
        await aufbau.Boards.Oeffne();
        await rahmen.OeffneLaufzeitliste();

        await rahmen.LaufzeitKartenverweis(aufbau.FremderZeiteintragId).ClickAsync();

        await aufbau.Kartendetail.ErwarteGeoeffnet();
        await Expect(Page).ToHaveURLAsync(new Regex($"/karten/{aufbau.ZweiteKarteId}$"));
    }

    // US-7: beenden darf jeder — die Stoppquadrate stehen auch da, wenn niemand gewählt ist. Die
    // Plakette ist dann ruhig, weil es ohne „mich" keinen eigenen Timer gibt.
    [Test]
    [Category("US-7")]
    public async Task Wenn_keine_Identitaet_gewaehlt_ist_dann_traegt_jede_Zeile_trotzdem_ihr_Stoppquadrat()
    {
        var aufbau = await ZweiBoards();
        var rahmen = new Rahmen(Page);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.StarteZeitmessung(aufbau.ZweiteKarteId, aufbau.Claude.KontributorId);
        var eigener = await webApi.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        await aufbau.Boards.Oeffne();

        await Expect(rahmen.Laufzeitzaehler).ToHaveClassAsync(new Regex("kopfzeile-laufzeit-fremd"));
        await rahmen.OeffneLaufzeitliste();

        await Expect(rahmen.Laufzeitpopover.Locator(".laufzeitstopp")).ToHaveCountAsync(2);
        await Expect(rahmen.Laufzeitherkuenfte).ToHaveTextAsync(["Beschaffung · Claude", "KanbanC — Release 2 · Stefan"]);

        await rahmen.LaufzeitStoppquadrat(eigener.ZeiteintragId).ClickAsync();
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("1 läuft");
    }

    // US-10 und US-11: die benannte Lücke und ihr Gegenstück. Startet jemand anders, bleibt die
    // Zahl stehen — bis das Aufklappen sie holt.
    [Test]
    [Category("US-11")]
    public async Task Wenn_jemand_anders_startet_dann_bleibt_die_Zahl_stehen_und_das_Aufklappen_holt_sie()
    {
        var aufbau = await ZweiBoards();
        var rahmen = new Rahmen(Page);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        await webApi.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        await aufbau.Boards.Oeffne();
        await WaehleIdentitaet(rahmen, aufbau.Stefan);
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("1 läuft");

        await webApi.StarteZeitmessung(aufbau.ZweiteKarteId, aufbau.Claude.KontributorId);

        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("1 läuft");

        await rahmen.OeffneLaufzeitliste();

        await Expect(rahmen.Laufzeitzeilen).ToHaveCountAsync(2);
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("2 laufen");
    }

    // US-7: derselbe Eintrag, zwei Orte — nach dem Stopp aus dem Popover steht auf der Kartenseite
    // kein laufender Timer mehr. Es entsteht kein zweiter Weg, nur ein zweiter Ort.
    [Test]
    [Category("US-7")]
    public async Task Wenn_aus_dem_Popover_gestoppt_wird_dann_zeigt_die_Kartenseite_denselben_Zustand()
    {
        var aufbau = await ZweiTimerAufZweiBoards();
        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();
        await rahmen.OeffneLaufzeitliste();

        await rahmen.LaufzeitStoppquadrat(aufbau.EigenerZeiteintragId).ClickAsync();
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("1 läuft");

        await aufbau.Kartendetail.Oeffne(aufbau.ErsteKarteId);

        await Expect(aufbau.Kartendetail.TimerStarten).ToBeVisibleAsync();
        await Expect(aufbau.Kartendetail.TimerStoppen).ToHaveCountAsync(0);
    }

    // US-10: eine Änderung an einem laufenden Eintrag ist für die Kopfzeile derselbe Anlass wie
    // ein Stopp — wird die laufende Zeile auf der Kartenseite gelöscht, verschwindet die Plakette
    // ohne Reload.
    [Test]
    [Category("US-10")]
    public async Task Wenn_ich_die_laufende_Zeile_auf_der_Kartenseite_loesche_dann_verschwindet_die_Plakette_ohne_Reload()
    {
        var aufbau = await ZweiBoards();
        var rahmen = new Rahmen(Page);
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var laufender = await webApi.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);
        await aufbau.Kartendetail.Oeffne(aufbau.ErsteKarteId);
        await WaehleIdentitaet(rahmen, aufbau.Stefan);
        await aufbau.Kartendetail.Oeffne(aufbau.ErsteKarteId);
        await Expect(rahmen.Laufzeitzaehler).ToHaveTextAsync("1 läuft");

        await aufbau.Kartendetail.Zeiteintragsstift(laufender.ZeiteintragId).ClickAsync();
        await Expect(aufbau.Kartendetail.Aenderungsformular.Rahmen).ToBeVisibleAsync();
        await aufbau.Kartendetail.Aenderungsformular.Loeschen.ClickAsync();

        await Expect(rahmen.Laufzeitzaehler).ToHaveCountAsync(0);
    }

    // Der Aufbau: zwei Boards, drei Kontributoren, eine Kartenklasse am ersten Board — mehr
    // braucht keiner dieser Läufe.
    private async Task<Aufbau> ZweiBoards()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var erstesBoard = await webApi.LegeBoardAn("KanbanC — Release 2");
        var zweitesBoard = await webApi.LegeBoardAn("Beschaffung");
        var ersteKarte = await webApi.LegeKarteAn(erstesBoard.BoardId, erstesBoard.Spalten[0].SpalteId, "Timer starten und stoppen");
        var zweiteKarte = await webApi.LegeKarteAn(zweitesBoard.BoardId, zweitesBoard.Spalten[0].SpalteId, "WBS-Import: Markdown-Baum");
        var kartenklasse = await webApi.LegeKartenklasseAn(erstesBoard.BoardId, "Arbeitspaket", "WBS-");
        await webApi.OrdneKartenklasseZu(ersteKarte.KarteId, kartenklasse.KartenklasseId);
        var stefan = await webApi.LegeKontributorAn("Stefan", Kontributorart.Mensch);
        var nina = await webApi.LegeKontributorAn("Nina Barth", Kontributorart.Mensch);
        var claude = await webApi.LegeKontributorAn("Claude", Kontributorart.Agent);

        var boards = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var board = new BoardSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var kontributoren = new KontributorenSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        var kartendetail = new KartendetailSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        return new Aufbau(boards, board, kontributoren, kartendetail, erstesBoard, zweitesBoard, ersteKarte.KarteId, zweiteKarte.KarteId, stefan, nina, claude, 0, 0);
    }

    // Zwei laufende Timer auf zwei verschiedenen Boards, mit Stefan als gewählter Identität: die
    // Lage, gegen die dieser Slice gebaut ist.
    private async Task<Aufbau> ZweiTimerAufZweiBoards()
    {
        var aufbau = await ZweiBoards();
        using var webApi = new WebApiKlient(Testumgebung.Aktuelle.WebApiAdresse);
        var fremder = await webApi.StarteZeitmessung(aufbau.ZweiteKarteId, aufbau.Claude.KontributorId);
        var eigener = await webApi.StarteZeitmessung(aufbau.ErsteKarteId, aufbau.Stefan.KontributorId);

        var rahmen = new Rahmen(Page);
        await aufbau.Boards.Oeffne();
        await WaehleIdentitaet(rahmen, aufbau.Stefan);
        return aufbau with { EigenerZeiteintragId = eigener.ZeiteintragId, FremderZeiteintragId = fremder.ZeiteintragId };
    }

    // Gewählt wird über die Kopfzeile, den Weg des Menschen.
    private async Task WaehleIdentitaet(Rahmen rahmen, Kontributor kontributor)
    {
        await rahmen.OeffneIdentitaetswahl();
        await rahmen.IdentitaetWaehlbareZeile(kontributor.KontributorId).ClickAsync();
        await Expect(rahmen.Identitaetsplatz).ToHaveTextAsync(kontributor.Name);
    }

    // „seit 08:04": das Wort und eine Tageszeit, keine Dauer.
    private static Regex Beginnmuster()
    {
        return new Regex(@"^seit \d{2}:\d{2}$");
    }

    // „Stefan seit 08:04 · Claude seit 09:12": alle einzeln, mit Trenner, ohne Dauer.
    private static Regex Titelmuster()
    {
        return new Regex(@"^Claude seit \d{2}:\d{2} · Stefan seit \d{2}:\d{2}$");
    }

    private sealed record Aufbau(
        BoardsSeite Boards,
        BoardSeite Board,
        KontributorenSeite Kontributoren,
        KartendetailSeite Kartendetail,
        Board ErstesBoard,
        Board ZweitesBoard,
        long ErsteKarteId,
        long ZweiteKarteId,
        Kontributor Stefan,
        Kontributor Nina,
        Kontributor Claude,
        long EigenerZeiteintragId,
        long FremderZeiteintragId);
}
