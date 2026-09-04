using KanbanC.PlaywrightTests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Probe nach dem Skill dependency-probe: R00013 ist die erste JS-Interop-Nutzung des
// Repositoriums. Drei Entwurfsentscheidungen hängen an dieser Probe — zu welchem Zeitpunkt im
// Lebenszyklus ein Aufruf trägt, wie ein zweiter Tab geöffnet werden muss, damit er den
// sessionStorage nicht erbt, und was ein werfender Speicher mit der Kopfzeile macht.
// Bleibt als Regressionsschutz stehen, wie ZiehenUndAblegenProbeE2ETests.
[TestFixture]
public class SessionStorageProbeE2ETests : PageTest
{
    private const string Probeschluessel = Browserspeicher.Probeschluessel;
    private const string Probewert = "erste Sitzung";

    [Test]
    public async Task Wenn_derselbe_Tab_neu_geladen_wird_dann_steht_der_Wert_im_sessionStorage_noch()
    {
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);
        await Page.EvaluateAsync($"() => sessionStorage.setItem('{Probeschluessel}', '{Probewert}')");

        await Page.ReloadAsync();

        var nachDemNeuladen = await Page.EvaluateAsync<string?>($"() => sessionStorage.getItem('{Probeschluessel}')");
        Assert.That(nachDemNeuladen, Is.EqualTo(Probewert),
            "Überlebt der Wert den Reload nicht, kann die Identitätswahl ihn nicht behalten.");
    }

    [Test]
    public async Task Wenn_ein_zweiter_Tab_ueber_den_Kontext_geoeffnet_wird_dann_beginnt_sein_sessionStorage_leer()
    {
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);
        await Page.EvaluateAsync($"() => sessionStorage.setItem('{Probeschluessel}', '{Probewert}')");

        var zweiterTab = await Context.NewPageAsync();
        await zweiterTab.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);

        var imZweitenTab = await zweiterTab.EvaluateAsync<string?>($"() => sessionStorage.getItem('{Probeschluessel}')");
        var imErstenTab = await Page.EvaluateAsync<string?>($"() => sessionStorage.getItem('{Probeschluessel}')");
        Assert.Multiple(() =>
        {
            Assert.That(imErstenTab, Is.EqualTo(Probewert));
            Assert.That(imZweitenTab, Is.Null, "So geöffnet erbt der zweite Tab den Speicher des ersten — dann belegt er nichts.");
        });
        await zweiterTab.CloseAsync();
    }

    // Fault Injection zur vorigen Probe: ein aus der Seite heraus geöffneter Tab erbt den
    // sessionStorage sehr wohl. Ohne diesen Beleg wäre nicht zu unterscheiden, ob ein leerer
    // zweiter Tab die Zusicherung belegt oder nur die Art des Öffnens.
    [Test]
    public async Task Wenn_ein_Tab_aus_der_Seite_heraus_geoeffnet_wird_dann_erbt_er_den_sessionStorage()
    {
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);
        await Page.EvaluateAsync($"() => sessionStorage.setItem('{Probeschluessel}', '{Probewert}')");

        var geerbterTab = await Page.RunAndWaitForPopupAsync(async () =>
        {
            await Page.EvaluateAsync($"() => window.open('{Testumgebung.Aktuelle.BlazorAdresse}')");
        });
        await geerbterTab.WaitForLoadStateAsync();

        var imGeerbtenTab = await geerbterTab.EvaluateAsync<string?>($"() => sessionStorage.getItem('{Probeschluessel}')");
        Assert.That(imGeerbtenTab, Is.EqualTo(Probewert),
            "Erbt auch dieser Tab nichts, unterscheidet die Probe die beiden Wege nicht.");
        await geerbterTab.CloseAsync();
    }

    // Lebenszyklus: App.razor rendert mit InteractiveServerRenderMode(prerender: false). Die
    // Kopfzeile steht deshalb in der ausgelieferten Seite noch nicht — sie entsteht erst im
    // verbundenen Kreislauf. Damit trägt ein IJSRuntime-Aufruf schon in OnInitializedAsync.
    [Test]
    public async Task Wenn_die_Seite_ohne_Vorabdarstellung_geladen_wird_dann_entsteht_die_Kopfzeile_erst_im_verbundenen_Kreislauf()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();

        var ausgelieferteAntwort = await Context.APIRequest.GetAsync(Testumgebung.Aktuelle.BlazorAdresse + "/boards");
        var ausgeliefertesMarkup = await ausgelieferteAntwort.TextAsync();
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse + "/boards");
        await Expect(Page.Locator("#kopfzeile")).ToBeVisibleAsync();

        var derKreislaufIstVerbunden = await Page.EvaluateAsync<bool>("() => typeof window.Blazor !== 'undefined'");
        Assert.Multiple(() =>
        {
            Assert.That(ausgeliefertesMarkup, Does.Not.Contain("id=\"kopfzeile\""),
                "Wird die Kopfzeile vorab dargestellt, läuft OnInitializedAsync ohne Kreislauf und Interop wirft dort.");
            Assert.That(derKreislaufIstVerbunden, Is.True);
        });
    }

    // Fault Injection: ein gesperrter Browser-Speicher wirft beim Lesen. Die Kopfzeile steht auf
    // jeder Seite — sie muss das aushalten, statt die Anwendung in die Ausnahmeseite zu reißen.
    [Test]
    public async Task Wenn_der_Browser_Speicher_gesperrt_ist_dann_wirft_er_beim_Lesen_und_die_Kopfzeile_steht_trotzdem()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        await Page.AddInitScriptAsync(Browserspeicher.Sperre);

        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse + "/boards");

        var meldungDesGesperrtenSpeichers = await Page.EvaluateAsync<string>(
            $"() => {{ try {{ sessionStorage.getItem('{Probeschluessel}'); return 'kein Fehler'; }} catch (fehler) {{ return fehler.name; }} }}");
        Assert.That(meldungDesGesperrtenSpeichers, Is.EqualTo("SecurityError"),
            "Ohne wirksame Sperre prüft diese Probe nichts.");
        await Expect(Page.Locator("#kopfzeile")).ToBeVisibleAsync();
        await Expect(Page.Locator("#identitaet")).ToBeVisibleAsync();
    }
}
