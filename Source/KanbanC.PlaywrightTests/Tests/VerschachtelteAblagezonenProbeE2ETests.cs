using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace KanbanC.PlaywrightTests.Tests;

// Probe nach dem Skill dependency-probe: R00008 legt zwei Ablagezonen als Auflagen in ein
// article, das selbst Ziehquelle ist. Ob der Wechsel zwischen den Hälften ein zwischenzeitliches
// „kein Ziel“ erzeugt — und damit eine flackernde Einfügelinie —, entscheidet sich an der
// Reihenfolge von dragenter und dragleave und daran, ob stopPropagation die Bahn abschirmt.
// Ohne diese Belege hätte F0022 keinen tragfähigen Entwurf.
[TestFixture]
public class VerschachtelteAblagezonenProbeE2ETests : PageTest
{
    private const string BahnMitZweiKartenhaelften = """
        <style>
          body { margin: 0 }
          #kopf { height: 40px; background: #ddd }
          #bahn { position: relative; padding: 10px; background: #eee }
          .karte { position: relative; height: 80px; margin-bottom: 10px; background: #fff }
          .haelfte { position: absolute; left: 0; right: 0; height: 50% }
          .oben { top: 0 }
          .unten { bottom: 0 }
        </style>
        <div id="kopf">Kopf</div>
        <div id="bahn">
          <article class="karte" id="karte-a" draggable="true">
            <div class="haelfte oben" data-zone="a-oben"></div>
            <div class="haelfte unten" data-zone="a-unten"></div>
          </article>
          <article class="karte" id="karte-b" draggable="true">
            <div class="haelfte oben" data-zone="b-oben"></div>
            <div class="haelfte unten" data-zone="b-unten"></div>
          </article>
        </div>
        <script>
          window.ereignisse = [];
          for (const quelle of document.querySelectorAll('.karte')) {
            quelle.addEventListener('dragstart', e => {
              e.dataTransfer.setData('text/plain', quelle.id);
              window.ereignisse.push('dragstart:' + quelle.id);
            });
            quelle.addEventListener('dragend', () => window.ereignisse.push('dragend'));
          }
          for (const zone of document.querySelectorAll('.haelfte')) {
            const name = zone.dataset.zone;
            zone.addEventListener('dragenter', e => {
              e.stopPropagation();
              window.ereignisse.push('enter:' + name);
            });
            zone.addEventListener('dragleave', e => {
              e.stopPropagation();
              window.ereignisse.push('leave:' + name);
            });
            zone.addEventListener('dragover', e => e.preventDefault());
            zone.addEventListener('drop', e => {
              e.preventDefault();
              e.stopPropagation();
              window.ereignisse.push('drop:' + name);
            });
          }
          const bahn = document.getElementById('bahn');
          bahn.addEventListener('dragenter', () => window.ereignisse.push('enter:bahn'));
          bahn.addEventListener('dragleave', () => window.ereignisse.push('leave:bahn'));
          bahn.addEventListener('dragover', e => e.preventDefault());
          bahn.addEventListener('drop', e => { e.preventDefault(); window.ereignisse.push('drop:bahn'); });
          const kopf = document.getElementById('kopf');
          kopf.addEventListener('dragenter', () => window.ereignisse.push('enter:kopf'));
        </script>
        """;

    [Test]
    public async Task Wenn_der_Zeiger_von_der_oberen_in_die_untere_Haelfte_wechselt_dann_kommt_dragenter_vor_dragleave()
    {
        await Page.SetContentAsync(BahnMitZweiKartenhaelften);

        await NimmAuf(Page.Locator("#karte-b"));
        await FahreUeber(Page.Locator("[data-zone='a-oben']"));
        await FahreUeber(Page.Locator("[data-zone='a-unten']"));

        var ereignisse = await Ereignisse();
        var enterUnten = Array.IndexOf(ereignisse, "enter:a-unten");
        var leaveOben = Array.IndexOf(ereignisse, "leave:a-oben");
        Assert.Multiple(() =>
        {
            Assert.That(enterUnten, Is.GreaterThanOrEqualTo(0), "Die untere Hälfte wurde nie betreten.");
            Assert.That(leaveOben, Is.GreaterThanOrEqualTo(0), "Die obere Hälfte wurde nie verlassen.");
            Assert.That(enterUnten, Is.LessThan(leaveOben),
                "dragleave der alten Zone kam vor dragenter der neuen — ein ungeschütztes dragleave würde die Linie löschen.");
        });
    }

    // Fault Injection: die Bahn ist selbst Ablageziel. Ohne stopPropagation an der Hälfte
    // überschriebe ihr dragenter das Ziel der Hälfte, und die Linie zeigte ans Bahnende.
    [Test]
    public async Task Wenn_eine_Kartenhaelfte_betreten_wird_dann_erreicht_dragenter_die_Bahn_darunter_nicht()
    {
        await Page.SetContentAsync(BahnMitZweiKartenhaelften);

        await NimmAuf(Page.Locator("#karte-b"));
        await LeereEreignisse();
        await FahreUeber(Page.Locator("[data-zone='a-oben']"));

        var ereignisse = await Ereignisse();
        Assert.Multiple(() =>
        {
            Assert.That(ereignisse, Does.Contain("enter:a-oben"));
            Assert.That(ereignisse, Does.Not.Contain("enter:bahn"));
        });
    }

    // Die Bahn muss ihr dragenter dort sehen, wo keine Hälfte liegt — nur so räumt der
    // Zeigerwechsel auf eine bedienfreie Fläche das Ziel wieder weg.
    [Test]
    public async Task Wenn_der_Zeiger_die_Kartenhaelften_verlaesst_dann_meldet_die_umgebende_Flaeche_ihr_eigenes_dragenter()
    {
        await Page.SetContentAsync(BahnMitZweiKartenhaelften);

        await NimmAuf(Page.Locator("#karte-b"));
        await FahreUeber(Page.Locator("[data-zone='a-oben']"));
        await LeereEreignisse();
        await FahreZumKopf();

        var ereignisse = await Ereignisse();
        Assert.That(ereignisse, Does.Contain("enter:kopf"));
    }

    // Die gezogene Karte trägt ihre eigenen Hälften: „auf sich selbst ablegen“ ist ein
    // Akzeptanzkriterium und braucht ein zugestelltes drop, keinen verschluckten Zug.
    [Test]
    public async Task Wenn_die_gezogene_Karte_auf_ihrer_eigenen_Haelfte_losgelassen_wird_dann_kommt_das_drop_an()
    {
        await Page.SetContentAsync(BahnMitZweiKartenhaelften);

        await NimmAuf(Page.Locator("#karte-a"));
        await LasseLosUeber(Page.Locator("[data-zone='a-unten']"));

        var ereignisse = await Ereignisse();
        Assert.Multiple(() =>
        {
            Assert.That(ereignisse, Does.Contain("drop:a-unten"));
            Assert.That(ereignisse, Does.Not.Contain("drop:bahn"));
        });
    }

    // Zonen, die erst nach dem Beginn des Zugs entstehen, sind unter Blazor Server der Regelfall:
    // die Hälften erscheinen erst nach einem Rundlauf über SignalR.
    [Test]
    public async Task Wenn_die_Kartenhaelften_erst_nach_dem_Beginn_des_Zugs_entstehen_dann_lassen_sie_sich_treffen()
    {
        await Page.SetContentAsync(BahnMitNachgereichtenHaelften);
        var obereHaelfte = Page.Locator("[data-zone='a-oben']");
        await Assertions.Expect(obereHaelfte).ToHaveCountAsync(0);

        await NimmAuf(Page.Locator("#karte-b"));
        await Assertions.Expect(obereHaelfte).ToBeVisibleAsync();
        await LasseLosUeber(obereHaelfte);

        var ereignisse = await Ereignisse();
        Assert.That(ereignisse, Does.Contain("drop:a-oben"));
    }

    private const string BahnMitNachgereichtenHaelften = """
        <style>
          body { margin: 0 }
          #bahn { position: relative; padding: 10px; background: #eee }
          .karte { position: relative; height: 80px; margin-bottom: 10px; background: #fff }
          .haelfte { position: absolute; left: 0; right: 0; height: 50% }
          .oben { top: 0 }
        </style>
        <div id="bahn">
          <article class="karte" id="karte-a"></article>
          <article class="karte" id="karte-b" draggable="true"></article>
        </div>
        <script>
          window.ereignisse = [];
          const b = document.getElementById('karte-b');
          b.addEventListener('dragstart', e => {
            e.dataTransfer.setData('text/plain', 'karte-b');
            setTimeout(() => {
              const zone = document.createElement('div');
              zone.className = 'haelfte oben';
              zone.dataset.zone = 'a-oben';
              zone.addEventListener('dragover', e => e.preventDefault());
              zone.addEventListener('drop', e => { e.preventDefault(); window.ereignisse.push('drop:a-oben'); });
              document.getElementById('karte-a').appendChild(zone);
            }, 300);
          });
        </script>
        """;

    private async Task<string[]> Ereignisse()
    {
        return await Page.EvaluateAsync<string[]>("() => window.ereignisse");
    }

    private async Task LeereEreignisse()
    {
        await Page.EvaluateAsync("() => { window.ereignisse = []; }");
    }

    private async Task NimmAuf(ILocator karte)
    {
        var kasten = await karte.BoundingBoxAsync();
        await Page.Mouse.MoveAsync(kasten!.X + kasten.Width / 2, kasten.Y + kasten.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(kasten.X + kasten.Width / 2, kasten.Y + kasten.Height / 2 + 10, new MouseMoveOptions { Steps = 5 });
    }

    private async Task FahreUeber(ILocator zone)
    {
        var kasten = await zone.BoundingBoxAsync();
        await Page.Mouse.MoveAsync(kasten!.X + kasten.Width / 2, kasten.Y + kasten.Height / 2, new MouseMoveOptions { Steps = 5 });
    }

    private async Task FahreZumKopf()
    {
        var kasten = await Page.Locator("#kopf").BoundingBoxAsync();
        await Page.Mouse.MoveAsync(kasten!.X + kasten.Width / 2, kasten.Y + kasten.Height / 2, new MouseMoveOptions { Steps = 5 });
    }

    private async Task LasseLosUeber(ILocator zone)
    {
        await FahreUeber(zone);
        await Page.Mouse.UpAsync();
    }
}
