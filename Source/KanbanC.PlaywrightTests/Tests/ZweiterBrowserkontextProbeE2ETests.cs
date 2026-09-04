using KanbanC.PlaywrightTests.Infrastructure;
using KanbanC.PlaywrightTests.PageObjects;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Probe der Eigenschaften, auf denen der Nachweis „die Einstellung hängt am Board, nicht am
// Browser“ ruht: ein zweiter Browser-Kontext teilt den Browserzustand des ersten nicht, bekommt
// seinen eigenen Blazor-Kreislauf und holt den Stand vom Server. Bleibt als Regressionsschutz stehen.
[TestFixture]
public class ZweiterBrowserkontextProbeE2ETests : PageTest
{
    [Test]
    public async Task Wenn_ein_zweiter_Kontext_geoeffnet_wird_dann_sieht_er_den_Browserzustand_des_ersten_nicht()
    {
        await Page.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);
        await Page.EvaluateAsync("() => localStorage.setItem('probe', 'erste Sitzung')");
        await using var zweiterKontext = await Browser.NewContextAsync();
        var zweiteSeite = await zweiterKontext.NewPageAsync();

        await zweiteSeite.GotoAsync(Testumgebung.Aktuelle.BlazorAdresse);

        var imZweitenKontext = await zweiteSeite.EvaluateAsync<string?>("() => localStorage.getItem('probe')");
        var imErstenKontext = await Page.EvaluateAsync<string?>("() => localStorage.getItem('probe')");
        Assert.Multiple(() =>
        {
            Assert.That(imErstenKontext, Is.EqualTo("erste Sitzung"));
            Assert.That(imZweitenKontext, Is.Null, "Der zweite Kontext teilt den Browserzustand des ersten — er belegt dann nichts.");
        });
    }

    [Test]
    public async Task Wenn_ein_zweiter_Kontext_dasselbe_Board_oeffnet_dann_bekommt_er_den_Stand_des_Servers()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();
        await Expect(liste.Boardzeile(1)).ToBeVisibleAsync();

        await using var zweiterKontext = await Browser.NewContextAsync();
        var zweiteSeite = await zweiterKontext.NewPageAsync();
        var boardImZweitenKontext = new BoardSeite(zweiteSeite, Testumgebung.Aktuelle.BlazorAdresse);
        await boardImZweitenKontext.Oeffne(1);

        await Assertions.Expect(boardImZweitenKontext.Name).ToHaveTextAsync("Entwicklung");
        await Assertions.Expect(boardImZweitenKontext.Spaltenbahnen).ToHaveCountAsync(3);
    }

    // Fault Injection: eine Blazor-Server-Seite lebt an ihrem Kreislauf. Bekäme der zweite Kontext
    // keinen eigenen, wären beide Seiten dieselbe Sitzung und der Beweis von B0115 wertlos.
    [Test]
    public async Task Wenn_beide_Kontexte_dieselbe_Seite_offen_haben_dann_haben_sie_getrennte_Blazor_Kreislaeufe()
    {
        await Testumgebung.Aktuelle.StarteWebApiMitLeererDatenbank();
        var liste = new BoardsSeite(Page, Testumgebung.Aktuelle.BlazorAdresse);
        await liste.Oeffne();
        await Expect(liste.HinweisKeineBoards).ToBeVisibleAsync();
        await using var zweiterKontext = await Browser.NewContextAsync();
        var zweiteSeite = await zweiterKontext.NewPageAsync();
        var zweiteListe = new BoardsSeite(zweiteSeite, Testumgebung.Aktuelle.BlazorAdresse);
        await zweiteListe.Oeffne();

        await liste.FuelleFormular("Entwicklung", "Linie", null, null);
        await liste.SendeFormularAb();

        await Expect(liste.Boardzeilen).ToHaveCountAsync(1);
        // Ohne Live-Übertragung (I0028) sieht die zweite Sitzung das neue Board erst beim
        // nächsten Laden — genau das belegt, dass es zwei Kreisläufe sind.
        await Assertions.Expect(zweiteListe.Boardzeilen).ToHaveCountAsync(0);
        await zweiteListe.Oeffne();
        await Assertions.Expect(zweiteListe.Boardzeilen).ToHaveCountAsync(1);
    }
}
