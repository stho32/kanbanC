using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// **Probe vor der Zusage** (Skill dependency-probe): der Trennungsdialog gehört dem Abbruch
// zwischen Browser und Blazor, und der Test muss ihn erzeugen, ohne den Blazor-Prozess anzuhalten
// — hielte er ihn an, stürbe der Kreislauf serverseitig, und die Vorlage zeigte ihren Dialog nicht.
// Offen war, **ob `Context.SetOfflineAsync` eine schon offene WebSocket-Verbindung wirklich reißt
// oder nur neue Verbindungen unterbindet.** Bis das gezeigt ist, ist es eine Annahme und kein
// Messwert; ohne den Beleg bliebe das Abweisen der `_blazor`-Route als Weg.
// Diese Probe misst es und bleibt als Beleg stehen — sie prüft die Mechanik, nicht die Anwendung.
[TestFixture]
public class TrennungsdialogProbeE2ETests : PageTest
{
    // Die Vorlage zeigt den Dialog erst, wenn ihr eigener Wiederverbindungsversuch scheitert; das
    // dauert länger als die Vorgabeschranke.
    private const int SchrankeNachTrennung = 20000;

    private const string SammleVerbindungen = @"
        window.__verbindungen = [];
        const Urspruengliche = WebSocket;
        window.WebSocket = class extends Urspruengliche {
            constructor(...argumente) {
                super(...argumente);
                window.__verbindungen.push(this);
            }
        };";

    private const string ReisseVerbindungen = "window.__verbindungen.forEach(verbindung => verbindung.close())";

    // Die Verweildauer traegt die Aussage, nicht die Erwartung: „nicht sichtbar" gilt schon im
    // Moment des Schaltens und waere ohne Wartezeit sofort erfuellt. Gewartet wird deshalb dieselbe
    // Spanne, in der der Test darunter den Dialog gemessen erscheinen sieht — was in ihr nicht
    // erscheint, erscheint wegen des Offline-Schaltens nicht.
    private static readonly TimeSpan Beobachtungsdauer = TimeSpan.FromMilliseconds(SchrankeNachTrennung);

    [Test]
    public async Task Wenn_der_Browserkontext_offline_geschaltet_wird_dann_reisst_die_offene_Verbindung_nicht()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        var trennungsdialog = Page.Locator("#components-reconnect-modal");
        await Expect(trennungsdialog).ToBeHiddenAsync();

        await Context.SetOfflineAsync(true);
        await Task.Delay(Beobachtungsdauer);

        await Expect(trennungsdialog).ToBeHiddenAsync();
        await Context.SetOfflineAsync(false);
    }

    [Test]
    public async Task Wenn_die_offene_Verbindung_im_Browser_geschlossen_wird_dann_erscheint_der_Trennungsdialog()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        await Context.AddInitScriptAsync(SammleVerbindungen);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        var trennungsdialog = Page.Locator("#components-reconnect-modal");
        await Expect(trennungsdialog).ToBeHiddenAsync();

        await Page.EvaluateAsync(ReisseVerbindungen);

        await Expect(trennungsdialog).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachTrennung });
    }

    // Die Gegenrichtung gehört zur Probe: nähme sich die Verbindung nie wieder auf, wäre der Weg
    // für den Testlauf unbrauchbar — und die Entscheidung „Fall 2 braucht kein Aufschließen"
    // stünde ohne Beleg. Sie **trifft zu**: der Kreislauf lebt serverseitig weiter, und die Seite
    // steht danach unverändert da.
    [Test]
    public async Task Wenn_die_Verbindung_zurueckkommt_dann_schliesst_der_Dialog_und_die_Seite_steht_weiter()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        await Context.AddInitScriptAsync(SammleVerbindungen);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        var trennungsdialog = Page.Locator("#components-reconnect-modal");
        // Erst schließen, dann offline: `close()` ist ein Handshake und braucht das Netz. Auf einem
        // schon offline geschalteten Kontext bleibt die Verbindung in CLOSING stehen und meldet nie
        // ein `close` — Blazor erführe vom Abriss nichts.
        await Page.EvaluateAsync(ReisseVerbindungen);
        await Context.SetOfflineAsync(true);
        await Expect(trennungsdialog).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachTrennung });

        await Context.SetOfflineAsync(false);

        await Expect(trennungsdialog).ToBeHiddenAsync(new LocatorAssertionsToBeHiddenOptions { Timeout = SchrankeNachTrennung });
        await Expect(liste.HinweisKeineBoards).ToBeVisibleAsync();
    }
}
