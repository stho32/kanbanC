using System.Text.RegularExpressions;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// US-12 über die ganze Strecke: der **zweite** Abbruch, zwischen Browser und Anwendung. Der Server
// merkt davon nichts und kann nichts mehr zeichnen — sichtbar ist allein der Trennungsdialog.
// **Der erste Test überhaupt auf ReconnectModal.razor.**
// Der Abbruch wird auf der Browserseite erzeugt und nicht durch Anhalten des Blazor-Prozesses: nur
// so bleibt der Kreislauf auf dem Server am Leben. Wie er erzeugt wird, ist gemessen und nicht
// angenommen — siehe TrennungsdialogProbeE2ETests: `SetOfflineAsync` allein reißt eine schon offene
// Verbindung nicht, es braucht beides.
[TestFixture]
public class TrennungsdialogE2ETests : PageTest
{
    // Bis die Vorlage ihren Dialog zeigt, vergeht ihr erster Wiederverbindungsversuch.
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

    [Test]
    [Category("US-12")]
    public async Task Wenn_der_Kreislauf_zum_Browser_abreisst_dann_spricht_der_Trennungsdialog_deutsch()
    {
        var trennungsdialog = await TrenneDenBrowserVonDerAnwendung();

        await Expect(trennungsdialog).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachTrennung });
        await Expect(trennungsdialog).ToContainTextAsync("Die Verbindung wird wiederhergestellt");
        await Expect(trennungsdialog).Not.ToContainTextAsync("Rejoining the server");
        await Expect(trennungsdialog).Not.ToContainTextAsync("Retry");
        await Context.SetOfflineAsync(false);
    }

    // Die eine Zeile, die das Artboard über die Vorlage hinaus zeichnet: seit wann der Stand alt
    // ist. Sie **entsteht im Browser** — der Server ist in diesem Fall weg und kann sie nicht
    // rendern.
    [Test]
    [Category("US-12")]
    public async Task Wenn_der_Trennungsdialog_steht_dann_nennt_er_den_Zeitpunkt_seit_dem_der_Schirm_alt_ist()
    {
        var trennungsdialog = await TrenneDenBrowserVonDerAnwendung();
        await Expect(trennungsdialog).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachTrennung });

        var alterszeile = Page.Locator("#verbindungsalter");

        await Expect(alterszeile).ToHaveTextAsync(new Regex(@"^Was du siehst, ist der Stand von \d\d:\d\d\.$"));
        await Context.SetOfflineAsync(false);
    }

    // **Kein Band und keine Kopfzeilenmarke:** das ist der andere Abbruch. Auf einem abgerissenen
    // Kreislauf erreichte eine gerenderte Marke den Browser ohnehin nie.
    [Test]
    [Category("US-12")]
    public async Task Wenn_der_Kreislauf_zum_Browser_abreisst_dann_erscheint_weder_Band_noch_Kopfzeilenmarke()
    {
        var trennungsdialog = await TrenneDenBrowserVonDerAnwendung();
        await Expect(trennungsdialog).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachTrennung });

        await Expect(Page.Locator("#verbindungsmarke")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#aufschliessband")).ToHaveCountAsync(0);
        await Context.SetOfflineAsync(false);
    }

    // Nach einem kurzen Abriss zeigt die Seite den **aktuellen** Stand: der Kreislauf hat
    // serverseitig weitergelebt, und deshalb gibt es in diesem Fall nichts aufzuschließen.
    [Test]
    [Category("US-12")]
    public async Task Wenn_die_Verbindung_zurueckkommt_dann_schliesst_der_Dialog_und_die_Seite_ist_wieder_bedienbar()
    {
        var trennungsdialog = await TrenneDenBrowserVonDerAnwendung();
        await Expect(trennungsdialog).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = SchrankeNachTrennung });
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);

        await Context.SetOfflineAsync(false);

        await Expect(trennungsdialog).ToBeHiddenAsync(new LocatorAssertionsToBeHiddenOptions { Timeout = SchrankeNachTrennung });
        await liste.OeffneAnlegeformular();
        await Expect(liste.Anlegeformular).ToBeVisibleAsync();
    }

    private async Task<ILocator> TrenneDenBrowserVonDerAnwendung()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        await Context.AddInitScriptAsync(SammleVerbindungen);
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        var trennungsdialog = Page.Locator("#components-reconnect-modal");
        await Assertions.Expect(trennungsdialog).ToBeHiddenAsync();

        // Beides zusammen: das Schließen reißt die offene Verbindung, das Offline verhindert, dass
        // sie sich sofort wieder aufnimmt und der Dialog vor der ersten Erwartung verschwindet.
        await Context.SetOfflineAsync(true);
        await Page.EvaluateAsync(ReisseVerbindungen);
        return trennungsdialog;
    }
}
